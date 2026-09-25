using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LibriKeep.Core.Application.Common.Interfaces;
using LibriKeep.Infrastructure.Persistence.Context;
using LibriKeep.Infrastructure.Persistence.Repositories;

namespace LibriKeep.Infrastructure.Persistence
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructurePersistence(this IServiceCollection services, IConfiguration configuration)
        {
            // ── Nodo PRIMARIO de PostgreSQL (escrituras + lectura del autor) ──────────────
            // Regla VIII-4: mutaciones y read-your-own-writes siempre al primario.
            var primaryConnectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<LibriKeepDbContext>(options =>
            {
                if (!string.IsNullOrEmpty(primaryConnectionString))
                {
                    options.UseNpgsql(primaryConnectionString,
                        b => b.MigrationsAssembly(typeof(LibriKeepDbContext).Assembly.FullName));
                }
                else
                {
                    // Fallback en memoria si no se provee cadena de conexión (desarrollo rápido / CI)
                    options.UseInMemoryDatabase("LibriKeepDb");
                }
            });

            // ── Réplica de LECTURA de PostgreSQL (catálogo público) ───────────────────────
            // Regla VIII-4: consultas con tolerancia a consistencia eventual se desvían
            // a réplicas de lectura para proteger el nodo primario.
            // Si 'ReadConnection' no está configurada, usa la misma cadena primaria
            // (degradación graciosa para entornos sin réplica configurada).
            var readConnectionString = configuration.GetConnectionString("ReadConnection")
                                       ?? primaryConnectionString;

            services.AddDbContext<LibriKeepReadDbContext>(options =>
            {
                if (!string.IsNullOrEmpty(readConnectionString))
                {
                    options.UseNpgsql(readConnectionString)
                           .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                }
                else
                {
                    options.UseInMemoryDatabase("LibriKeepDb")
                           .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                }
            });

            // ── Repositorios ──────────────────────────────────────────────────────────────
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<IEjemplarRepository, EjemplarRepository>();
            services.AddScoped<IPrestamoRepository, PrestamoRepository>();
            // ILibroRepository usa el repositorio concreto como implementación interna;
            // el decorador CachedLibroRepository se registra encima desde el proyecto de caché.
            services.AddScoped<LibroRepository>();
            services.AddScoped<ILibroRepository, LibroRepository>();
            services.AddScoped<ISancionRepository, SancionRepository>();
            services.AddScoped<IReservaRepository, ReservaRepository>();

            // ── Unidad de Trabajo ─────────────────────────────────────────────────────────
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
