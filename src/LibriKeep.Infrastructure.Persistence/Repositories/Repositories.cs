using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LibriKeep.Core.Application.Common.Interfaces;
using LibriKeep.Core.Domain.Entities;
using LibriKeep.Core.Domain.Enums;
using LibriKeep.Infrastructure.Persistence.Context;

namespace LibriKeep.Infrastructure.Persistence.Repositories
{
    // ==========================================
    // 1. USUARIO REPOSITORY
    // ==========================================
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly LibriKeepDbContext _context;

        public UsuarioRepository(LibriKeepDbContext context)
        {
            _context = context;
        }

        public async Task<Usuario?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Usuarios
                .Include(u => u.Prestamos)
                .Include(u => u.Sanciones)
                .Include(u => u.Reservas)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task<Usuario?> GetByDniAsync(string dni, CancellationToken cancellationToken = default)
        {
            return await _context.Usuarios
                .Include(u => u.Prestamos)
                .Include(u => u.Sanciones)
                .Include(u => u.Reservas)
                .FirstOrDefaultAsync(u => u.Dni == dni, cancellationToken);
        }

        public async Task AddAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            await _context.Usuarios.AddAsync(usuario, cancellationToken);
        }
    }

    // ==========================================
    // 2. EJEMPLAR REPOSITORY
    // ==========================================
    public class EjemplarRepository : IEjemplarRepository
    {
        private readonly LibriKeepDbContext _context;

        public EjemplarRepository(LibriKeepDbContext context)
        {
            _context = context;
        }

        public async Task<Ejemplar?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Ejemplares
                .Include(e => e.Libro)
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }

        public async Task<Ejemplar?> GetByCodigoBarrasAsync(string codigoBarras, CancellationToken cancellationToken = default)
        {
            return await _context.Ejemplares
                .Include(e => e.Libro)
                .FirstOrDefaultAsync(e => e.CodigoBarras == codigoBarras, cancellationToken);
        }
    }

    // ==========================================
    // 3. PRESTAMO REPOSITORY
    // ==========================================
    public class PrestamoRepository : IPrestamoRepository
    {
        private readonly LibriKeepDbContext _context;

        public PrestamoRepository(LibriKeepDbContext context)
        {
            _context = context;
        }

        public async Task<Prestamo?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Prestamos
                .Include(p => p.Ejemplar)
                .ThenInclude(e => e.Libro)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task AddAsync(Prestamo prestamo, CancellationToken cancellationToken = default)
        {
            await _context.Prestamos.AddAsync(prestamo, cancellationToken);
        }

        public async Task<Prestamo?> GetActiveLoanByCodigoBarrasAsync(string codigoBarras, CancellationToken cancellationToken = default)
        {
            return await _context.Prestamos
                .Include(p => p.Ejemplar)
                .ThenInclude(e => e.Libro)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Ejemplar.CodigoBarras == codigoBarras &&
                                         (p.Estado == EstadoPrestamo.Activo || p.Estado == EstadoPrestamo.Demorado),
                                         cancellationToken);
        }

        public async Task<int> GetActiveLoansCountForUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default)
        {
            return await _context.Prestamos
                .CountAsync(p => p.UsuarioId == usuarioId && 
                                 (p.Estado == EstadoPrestamo.Activo || p.Estado == EstadoPrestamo.Demorado), 
                                 cancellationToken);
        }
    }

    // ==========================================
    // 4. LIBRO REPOSITORY
    // ==========================================
    public class LibroRepository : ILibroRepository
    {
        private readonly LibriKeepDbContext _context;

        public LibroRepository(LibriKeepDbContext context)
        {
            _context = context;
        }

        public async Task<Libro?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Libros
                .Include(l => l.Ejemplares)
                .Include(l => l.Reservas)
                .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        }

        public async Task<Libro?> GetByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
        {
            return await _context.Libros
                .FirstOrDefaultAsync(l => l.Isbn == isbn, cancellationToken);
        }

        public async Task<bool> AnyAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Libros.AnyAsync(l => l.Id == id, cancellationToken);
        }

        public async Task AddAsync(Libro libro, CancellationToken cancellationToken = default)
        {
            await _context.Libros.AddAsync(libro, cancellationToken);
        }
    }

    // ==========================================
    // 5. SANCION REPOSITORY
    // ==========================================
    public class SancionRepository : ISancionRepository
    {
        private readonly LibriKeepDbContext _context;

        public SancionRepository(LibriKeepDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Sancion sancion, CancellationToken cancellationToken = default)
        {
            await _context.Sanciones.AddAsync(sancion, cancellationToken);
        }
    }

    // ==========================================
    // 6. RESERVA REPOSITORY
    // ==========================================
    public class ReservaRepository : IReservaRepository
    {
        private readonly LibriKeepDbContext _context;

        public ReservaRepository(LibriKeepDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Reserva reserva, CancellationToken cancellationToken = default)
        {
            await _context.Reservas.AddAsync(reserva, cancellationToken);
        }

        public async Task<Reserva?> GetFirstActiveReservationForLibroAsync(int libroId, CancellationToken cancellationToken = default)
        {
            return await _context.Reservas
                .Where(r => r.LibroId == libroId && r.Estado == EstadoReserva.Activa)
                .OrderBy(r => r.PosicionCola)
                .ThenBy(r => r.FechaReserva)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> HasActiveReservationsForLibroAsync(int libroId, CancellationToken cancellationToken = default)
        {
            return await _context.Reservas
                .AnyAsync(r => r.LibroId == libroId && r.Estado == EstadoReserva.Activa, cancellationToken);
        }
    }
}
