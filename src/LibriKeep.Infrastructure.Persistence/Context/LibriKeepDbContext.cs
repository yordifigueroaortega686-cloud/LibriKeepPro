using Microsoft.EntityFrameworkCore;
using LibriKeep.Core.Domain.Entities;

namespace LibriKeep.Infrastructure.Persistence.Context
{
    public class LibriKeepDbContext : DbContext
    {
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Libro> Libros => Set<Libro>();
        public DbSet<Ejemplar> Ejemplares => Set<Ejemplar>();
        public DbSet<Prestamo> Prestamos => Set<Prestamo>();
        public DbSet<Sancion> Sanciones => Set<Sancion>();
        public DbSet<Reserva> Reservas => Set<Reserva>();
        public DbSet<Autor> Autores => Set<Autor>();
        public DbSet<Categoria> Categorias => Set<Categoria>();
        public DbSet<Editorial> Editoriales => Set<Editorial>();

        public LibriKeepDbContext(DbContextOptions<LibriKeepDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Aplicar configuraciones de Fluent API desde el ensamblado
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibriKeepDbContext).Assembly);
        }
    }
}
