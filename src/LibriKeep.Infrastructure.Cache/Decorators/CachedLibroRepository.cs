using System;
using System.Threading;
using System.Threading.Tasks;
using LibriKeep.Core.Application.Common.Interfaces;
using LibriKeep.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LibriKeep.Infrastructure.Cache.Decorators
{
    /// <summary>
    /// Decorador de <see cref="ILibroRepository"/> que aplica el patrón Cache-Aside (Regla VIII-3).
    ///
    /// Lecturas del catálogo público se resuelven desde Redis (TTL = 60 s) antes de tocar PostgreSQL.
    /// Las operaciones de escritura (AddAsync) invalidan las entradas de caché afectadas.
    ///
    /// El autor del cambio siempre lee del repositorio primario (sin caché) para cumplir con la
    /// regla de consistencia inmediata read-your-own-writes (Regla VIII-4).
    /// </summary>
    public sealed class CachedLibroRepository : ILibroRepository
    {
        // TTL del catálogo público: 60 segundos (Regla VIII-3)
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

        // Prefijos de clave para evitar colisiones en el keyspace de Redis
        private const string PrefixById   = "libro:id:";
        private const string PrefixByIsbn = "libro:isbn:";

        private readonly ILibroRepository _inner;
        private readonly ICacheService _cache;
        private readonly ILogger<CachedLibroRepository> _logger;

        public CachedLibroRepository(
            ILibroRepository inner,
            ICacheService cache,
            ILogger<CachedLibroRepository> logger)
        {
            _inner  = inner;
            _cache  = cache;
            _logger = logger;
        }

        /// <summary>
        /// Consulta de catálogo público: resuelve desde caché con Single-Flight.
        /// Usa Cache-Aside: Redis → miss → PostgreSQL (réplica) → almacenar → retornar.
        /// </summary>
        public Task<Libro?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var key = $"{PrefixById}{id}";
            return _cache.GetOrCreateAsync<Libro>(
                key,
                ct => _inner.GetByIdAsync(id, ct)!,
                CacheTtl,
                cancellationToken)!;
        }

        /// <summary>
        /// Consulta por ISBN: resuelve desde caché con Single-Flight.
        /// </summary>
        public Task<Libro?> GetByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
        {
            var key = $"{PrefixByIsbn}{isbn}";
            return _cache.GetOrCreateAsync<Libro>(
                key,
                ct => _inner.GetByIsbnAsync(isbn, ct)!,
                CacheTtl,
                cancellationToken)!;
        }

        /// <summary>
        /// Verificación de existencia: siempre va al repositorio primario (dato crítico
        /// para validar unicidad antes de una escritura). Sin caché por diseño.
        /// </summary>
        public Task<bool> AnyAsync(int id, CancellationToken cancellationToken = default)
            => _inner.AnyAsync(id, cancellationToken);

        /// <summary>
        /// Alta de nuevo libro: persiste en el nodo primario de PostgreSQL e invalida
        /// cualquier entrada de caché previa relacionada para evitar lecturas obsoletas.
        /// </summary>
        public async Task AddAsync(Libro libro, CancellationToken cancellationToken = default)
        {
            await _inner.AddAsync(libro, cancellationToken);

            // Invalidar entradas de caché que pudieran estar obsoletas
            await _cache.RemoveAsync($"{PrefixByIsbn}{libro.Isbn}", cancellationToken);
            _logger.LogDebug("[Caché] Entrada invalidada tras escritura: libro ISBN '{Isbn}'.", libro.Isbn);
        }
    }
}
