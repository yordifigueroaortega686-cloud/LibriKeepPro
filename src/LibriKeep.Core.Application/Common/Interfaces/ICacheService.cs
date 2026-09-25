using System;
using System.Threading;
using System.Threading.Tasks;

namespace LibriKeep.Core.Application.Common.Interfaces
{
    /// <summary>
    /// Contrato de servicio de caché distribuido para LibriKeep Pro.
    /// Implementa el patrón Cache-Aside con soporte de TTL y Single-Flight / Mutex
    /// para prevenir el efecto avalancha (cache stampede) ante caídas del servicio de caché.
    /// Regla VIII-3 — Constitución del Proyecto.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Obtiene el valor almacenado bajo <paramref name="key"/>. Retorna null si no existe.
        /// </summary>
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Almacena <paramref name="value"/> bajo <paramref name="key"/> con el TTL indicado.
        /// </summary>
        Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Elimina la entrada de caché asociada a <paramref name="key"/>.
        /// Se invoca tras operaciones de escritura para invalidar entradas obsoletas.
        /// </summary>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Patrón Cache-Aside + Single-Flight:
        /// Intenta obtener el valor de caché. Si no existe (cache miss), ejecuta <paramref name="factory"/>
        /// para obtenerlo de la fuente de verdad, lo almacena y lo retorna.
        /// Ante concurrencia simultánea sobre la misma clave, solo una solicitud consulta la fuente;
        /// las demás esperan y reutilizan el resultado (Single-Flight / Mutex).
        /// </summary>
        Task<T> GetOrCreateAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan ttl,
            CancellationToken cancellationToken = default) where T : class;
    }
}
