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
            // Registrar DbContext apuntando a PostgreSQL
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            services.AddDbContext<LibriKeepDbContext>(options =>
            {
                if (!string.IsNullOrEmpty(connectionString))
                {
                    options.UseNpgsql(connectionString, b => b.MigrationsAssembly(typeof(LibriKeepDbContext).Assembly.FullName));
                }
                else
                {
                    // Fallback en memoria si no se provee cadena de conexión (útil para desarrollo rápido/pruebas)
                    options.UseInMemoryDatabase("LibriKeepDb");
                }
            });

            // Registrar Repositorios
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<IEjemplarRepository, EjemplarRepository>();
            services.AddScoped<IPrestamoRepository, PrestamoRepository>();
            services.AddScoped<ILibroRepository, LibroRepository>();
            services.AddScoped<ISancionRepository, SancionRepository>();
            services.AddScoped<IReservaRepository, ReservaRepository>();

            // Registrar UnitOfWork
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
