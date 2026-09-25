using System;
using System.Threading;
using System.Threading.Tasks;
using LibriKeep.Core.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LibriKeep.Infrastructure.Cache.Services
{
    /// <summary>
    /// Implementación nula (Null Object Pattern) de <see cref="ICacheService"/>.
    /// Se activa cuando Redis no está configurado (ej. entorno de desarrollo local sin Docker).
    /// Todas las operaciones pasan directamente al factory sin almacenar ni leer ningún valor.
    /// El sistema funciona correctamente pero sin beneficios de caché.
    /// </summary>
    public sealed class NullCacheService : ICacheService
    {
        private readonly ILogger<NullCacheService> _logger;

        public NullCacheService(ILogger<NullCacheService> logger)
        {
            _logger = logger;
            _logger.LogInformation(
                "[Caché] NullCacheService activo. El sistema opera sin caché distribuida. " +
                "Configure 'ConnectionStrings:RedisConnection' para activar Redis.");
        }

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
            => Task.FromResult<T?>(null);

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default) where T : class
            => Task.CompletedTask;

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public async Task<T> GetOrCreateAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan ttl,
            CancellationToken cancellationToken = default) where T : class
            => await factory(cancellationToken);
    }
}
