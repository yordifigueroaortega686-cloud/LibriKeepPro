using LibriKeep.Core.Application.Common.Interfaces;
using LibriKeep.Infrastructure.Cache.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace LibriKeep.Infrastructure.Cache
{
    /// <summary>
    /// Extensión de IServiceCollection que registra toda la infraestructura de caché:
    /// - Conexión Redis (StackExchange.Redis / Upstash compatible)
    /// - ICacheService implementado por RedisCacheService
    ///
    /// Si la cadena de conexión de Redis no está configurada, se registra una implementación
    /// nula (NullCacheService) que pasa siempre el factory directamente, garantizando
    /// que el sistema funcione aunque Redis no esté disponible (degradación graciosamente).
    ///
    /// Regla VIII-3 y VIII-5 — Constitución del Proyecto LibriKeep Pro.
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureCache(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var redisConnectionString = configuration.GetConnectionString("RedisConnection");

            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                // Registrar la conexión Redis como Singleton (recomendación de StackExchange.Redis)
                services.AddSingleton<IConnectionMultiplexer>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<RedisCacheService>>();
                    try
                    {
                        var options = ConfigurationOptions.Parse(redisConnectionString);
                        options.AbortOnConnectFail = false; // No abortar si Redis no responde al inicio
                        options.ConnectTimeout = 3000;
                        options.SyncTimeout = 2000;
                        return ConnectionMultiplexer.Connect(options);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex,
                            "[Caché] No se pudo conectar a Redis. El sistema operará sin caché (degradación graciosa).");
                        throw;
                    }
                });

                // Registrar ICacheService con implementación Redis
                services.AddSingleton<ICacheService, RedisCacheService>();
            }
            else
            {
                // Sin configuración de Redis: registrar implementación nula (pass-through)
                services.AddSingleton<ICacheService, NullCacheService>();
            }

            return services;
        }
    }
}
