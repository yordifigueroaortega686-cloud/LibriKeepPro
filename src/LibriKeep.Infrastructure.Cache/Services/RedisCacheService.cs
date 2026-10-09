using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using LibriKeep.Core.Application.Common.Interfaces;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace LibriKeep.Infrastructure.Cache.Services
{
    /// <summary>
    /// Implementación de <see cref="ICacheService"/> usando Redis vía StackExchange.Redis.
    /// Compatible con Upstash Redis (capa gratuita) en producción.
    ///
    /// Garantías:
    /// - Cache-Aside: consulta caché primero; en miss, va a la fuente y almacena el resultado.
    /// - Single-Flight / Mutex: un SemaphoreSlim por clave evita que múltiples solicitudes
    ///   concurrentes bombardeen la base de datos ante un cache miss simultáneo.
    /// - Resiliente: si Redis no está disponible, el sistema degrada graciosamente ejecutando
    ///   siempre el factory sin elevar excepciones al cliente.
    ///
    /// Regla VIII-3 — Constitución del Proyecto LibriKeep Pro.
    /// </summary>
    public sealed class RedisCacheService : ICacheService, IDisposable
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        // Single-Flight: mapa de SemaphoreSlim por clave para evitar cache stampede.
        private readonly Dictionary<string, SemaphoreSlim> _locks = new();
        private readonly object _lockMapLock = new();

        public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
        {
            _redis = redis;
            _db = redis.GetDatabase();
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            };
        }

        /// <inheritdoc />
        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                _logger.LogInformation("[Caché] Intentando leer clave '{Key}' desde Redis.", key);
                var raw = await _db.StringGetAsync(key);
                if (!raw.HasValue)
                {
                    _logger.LogInformation("[Caché] Cache MISS para clave '{Key}'.", key);
                    return null;
                }

                _logger.LogInformation("[Caché] Cache HIT para clave '{Key}'.", key);
                return JsonSerializer.Deserialize<T>((string)raw!, _jsonOptions);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "[Caché] Redis no disponible al leer clave '{Key}'. Degradando a miss.", key);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                _logger.LogInformation("[Caché] Guardando clave '{Key}' en Redis con TTL={Ttl}s.", key, ttl.TotalSeconds);
                var payload = JsonSerializer.Serialize(value, _jsonOptions);
                await _db.StringSetAsync(key, payload, ttl);
                _logger.LogInformation("[Caché] Clave '{Key}' guardada exitosamente en Redis.", key);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "[Caché] Redis no disponible al escribir clave '{Key}'. Continuando sin caché.", key);
            }
        }

        /// <inheritdoc />
        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                await _db.KeyDeleteAsync(key);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "[Caché] Redis no disponible al eliminar clave '{Key}'.", key);
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// Implementa Single-Flight: un <see cref="SemaphoreSlim"/> por clave garantiza que,
        /// ante múltiples solicitudes concurrentes con cache miss, solo la primera consulta la BD.
        /// Las restantes esperan y leen el resultado almacenado por la primera.
        /// </remarks>
        public async Task<T> GetOrCreateAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan ttl,
            CancellationToken cancellationToken = default) where T : class
        {
            // Intento rápido sin bloqueo (caso optimista: cache hit)
            var cached = await GetAsync<T>(key, cancellationToken);
            if (cached is not null) return cached;

            // Obtener o crear el semáforo para esta clave específica (Single-Flight)
            SemaphoreSlim semaphore;
            lock (_lockMapLock)
            {
                if (!_locks.TryGetValue(key, out semaphore!))
                {
                    semaphore = new SemaphoreSlim(1, 1);
                    _locks[key] = semaphore;
                }
            }

            await semaphore.WaitAsync(cancellationToken);
            try
            {
                // Segunda verificación tras adquirir el mutex:
                // la primera solicitud ya pudo haber llenado la caché.
                cached = await GetAsync<T>(key, cancellationToken);
                if (cached is not null) return cached;

                // Única solicitud que llega a la fuente de verdad (BD)
                _logger.LogDebug("[Caché] Cache miss en '{Key}'. Consultando fuente de verdad.", key);
                var result = await factory(cancellationToken);

                await SetAsync(key, result, ttl, cancellationToken);
                return result;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public void Dispose()
        {
            lock (_lockMapLock)
            {
                foreach (var sem in _locks.Values)
                    sem.Dispose();
                _locks.Clear();
            }
        }
    }
}
