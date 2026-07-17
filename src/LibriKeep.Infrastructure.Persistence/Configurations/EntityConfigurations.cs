using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LibriKeep.Core.Domain.Entities;
using LibriKeep.Core.Domain.Enums;

namespace LibriKeep.Infrastructure.Persistence.Configurations
{
    // ==========================================
    // 1. AUTOR CONFIGURATION
    // ==========================================
    public class AutorConfiguration : IEntityTypeConfiguration<Autor>
    {
        public void Configure(EntityTypeBuilder<Autor> builder)
        {
            builder.ToTable("Autores");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Nombre).HasMaxLength(150).IsRequired();
            builder.Property(a => a.Nacionalidad).HasMaxLength(100);

            builder.HasMany(a => a.Libros)
                   .WithOne(l => l.Autor)
                   .HasForeignKey(l => l.AutorId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    // ==========================================
    // 2. CATEGORIA CONFIGURATION
    // ==========================================
    public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
    {
        public void Configure(EntityTypeBuilder<Categoria> builder)
        {
            builder.ToTable("Categorias");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Nombre).HasMaxLength(100).IsRequired();
            builder.Property(c => c.Descripcion).HasMaxLength(500);

            builder.HasMany(c => c.Libros)
                   .WithOne(l => l.Categoria)
                   .HasForeignKey(l => l.CategoriaId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    // ==========================================
    // 3. EDITORIAL CONFIGURATION
    // ==========================================
    public class EditorialConfiguration : IEntityTypeConfiguration<Editorial>
    {
        public void Configure(EntityTypeBuilder<Editorial> builder)
        {
            builder.ToTable("Editoriales");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Nombre).HasMaxLength(150).IsRequired();

            builder.HasMany(e => e.Libros)
                   .WithOne(l => l.Editorial)
                   .HasForeignKey(l => l.EditorialId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    // ==========================================
    // 4. LIBRO CONFIGURATION
    // ==========================================
    public class LibroConfiguration : IEntityTypeConfiguration<Libro>
    {
        public void Configure(EntityTypeBuilder<Libro> builder)
        {
            builder.ToTable("Libros");
            builder.HasKey(l => l.Id);
            builder.Property(l => l.Titulo).HasMaxLength(250).IsRequired();
            
            // ISBN Único indexado
            builder.Property(l => l.Isbn).HasMaxLength(20).IsRequired();
            builder.HasIndex(l => l.Isbn).IsUnique();

            builder.Property(l => l.Idioma).HasMaxLength(50);
            builder.Property(l => l.Pais).HasMaxLength(100);
            
            builder.Property(l => l.TipoMaterial)
                   .HasConversion<string>()
                   .HasMaxLength(50)
                   .IsRequired();

            builder.HasMany(l => l.Ejemplares)
                   .WithOne(e => e.Libro)
                   .HasForeignKey(e => e.LibroId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(l => l.Reservas)
                   .WithOne(r => r.Libro)
                   .HasForeignKey(r => r.LibroId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

    // ==========================================
    // 5. EJEMPLAR CONFIGURATION
    // ==========================================
    public class EjemplarConfiguration : IEntityTypeConfiguration<Ejemplar>
    {
        public void Configure(EntityTypeBuilder<Ejemplar> builder)
        {
            builder.ToTable("Ejemplares");
            builder.HasKey(e => e.Id);
            
            // Código de barras Único e indexado
            builder.Property(e => e.CodigoBarras).HasMaxLength(50).IsRequired();
            builder.HasIndex(e => e.CodigoBarras).IsUnique();

            builder.Property(e => e.UbicacionFisica).HasMaxLength(200);
            builder.Property(e => e.Observaciones).HasMaxLength(1000);

            // Estado convertido a string
            builder.Property(e => e.Estado)
                   .HasConversion<string>()
                   .HasMaxLength(50)
                   .IsRequired();

            // CONCURRENCIA OPTIMISTA: Marcar el estado del ejemplar como token de concurrencia
            builder.Property(e => e.Estado).IsConcurrencyToken();
        }
    }

    // ==========================================
    // 6. USUARIO CONFIGURATION
    // ==========================================
    public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("Usuarios");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.NombreCompleto).HasMaxLength(200).IsRequired();

            // DNI Único indexado
            builder.Property(u => u.Dni).HasMaxLength(20).IsRequired();
            builder.HasIndex(u => u.Dni).IsUnique();

            // Correo Único indexado
            builder.Property(u => u.Email).HasMaxLength(150).IsRequired();
            builder.HasIndex(u => u.Email).IsUnique();

            builder.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
            builder.Property(u => u.Telefono).HasMaxLength(20);

            builder.Property(u => u.TipoMiembro)
                   .HasConversion<string>()
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(u => u.Rol)
                   .HasConversion<string>()
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(u => u.Estado)
                   .HasConversion<string>()
                   .HasMaxLength(50)
                   .IsRequired();

            builder.HasMany(u => u.Prestamos)
                   .WithOne(p => p.Usuario)
                   .HasForeignKey(p => p.UsuarioId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.Sanciones)
                   .WithOne(s => s.Usuario)
                   .HasForeignKey(s => s.UsuarioId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.Reservas)
                   .WithOne(r => r.Usuario)
                   .HasForeignKey(r => r.UsuarioId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    // ==========================================
    // 7. PRESTAMO CONFIGURATION
    // ==========================================
    public class PrestamoConfiguration : IEntityTypeConfiguration<Prestamo>
    {
        public void Configure(EntityTypeBuilder<Prestamo> builder)
        {
            builder.ToTable("Prestamos");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.FechaSalida).IsRequired();
            builder.Property(p => p.FechaMaxDevolucion).IsRequired();
            builder.Property(p => p.FechaDevolucionEfectiva);

            builder.Property(p => p.Estado)
                   .HasConversion<string>()
                   .HasMaxLength(50)
                   .IsRequired();

            builder.HasOne(p => p.Ejemplar)
                   .WithMany()
                   .HasForeignKey(p => p.EjemplarId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    // ==========================================
    // 8. SANCION CONFIGURATION
    // ==========================================
    public class SancionConfiguration : IEntityTypeConfiguration<Sancion>
    {
        public void Configure(EntityTypeBuilder<Sancion> builder)
        {
            builder.ToTable("Sanciones");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.FechaInicio).IsRequired();
            builder.Property(s => s.FechaFin).IsRequired();
            builder.Property(s => s.DiasSancion).IsRequired();

            builder.Property(s => s.Estado)
                   .HasConversion<string>()
                   .HasMaxLength(50)
                   .IsRequired();

            builder.HasOne(s => s.Prestamo)
                   .WithMany()
                   .HasForeignKey(s => s.PrestamoId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }

    // ==========================================
    // 9. RESERVA CONFIGURATION
    // ==========================================
    public class ReservaConfiguration : IEntityTypeConfiguration<Reserva>
    {
        public void Configure(EntityTypeBuilder<Reserva> builder)
        {
            builder.ToTable("Reservas");
            builder.HasKey(r => r.Id);

            builder.Property(r => r.FechaReserva).IsRequired();
            builder.Property(r => r.PosicionCola).IsRequired();

            builder.Property(r => r.Estado)
                   .HasConversion<string>()
                   .HasMaxLength(50)
                   .IsRequired();
        }
    }
}
