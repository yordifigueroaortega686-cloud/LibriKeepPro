using Microsoft.EntityFrameworkCore;
using LibriKeep.Core.Domain.Entities;

namespace LibriKeep.Infrastructure.Persistence.Context
{
    /// <summary>
    /// DbContext de solo lectura que apunta a la réplica de lectura de PostgreSQL.
    /// Se usa para consultas del catálogo público con tolerancia a consistencia eventual.
    ///
    /// NUNCA debe usarse para operaciones de escritura (INSERT, UPDATE, DELETE).
    /// Las mutaciones siempre se realizan a través de <see cref="LibriKeepDbContext"/> (nodo primario).
    ///
    /// El autor del cambio (operador catalogador) siempre lee del nodo primario
    /// (LibriKeepDbContext) para garantizar consistencia inmediata (read-your-own-writes).
    ///
    /// Regla VIII-4 — Constitución del Proyecto LibriKeep Pro.
    /// </summary>
    public sealed class LibriKeepReadDbContext : DbContext
    {
        // Conjuntos de solo lectura para el catálogo público
        public DbSet<Libro>     Libros     => Set<Libro>();
        public DbSet<Autor>     Autores    => Set<Autor>();
        public DbSet<Categoria> Categorias => Set<Categoria>();
        public DbSet<Editorial> Editoriales => Set<Editorial>();
        public DbSet<Ejemplar>  Ejemplares => Set<Ejemplar>();

        public LibriKeepReadDbContext(DbContextOptions<LibriKeepReadDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Reusar las mismas configuraciones de Fluent API del contexto principal
            // para mantener consistencia en los mappings de columnas e índices.
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibriKeepDbContext).Assembly);
        }

        /// <summary>
        /// Sobrescribir SaveChangesAsync para prevenir escrituras accidentales vía este contexto.
        /// </summary>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "LibriKeepReadDbContext es de solo lectura (réplica de lectura). " +
                "Use LibriKeepDbContext para operaciones de escritura (Regla VIII-4).");
        }

        public override int SaveChanges()
        {
            throw new InvalidOperationException(
                "LibriKeepReadDbContext es de solo lectura (réplica de lectura). " +
                "Use LibriKeepDbContext para operaciones de escritura (Regla VIII-4).");
        }
    }
}
