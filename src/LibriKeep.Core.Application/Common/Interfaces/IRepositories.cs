using System;
using System.Threading;
using System.Threading.Tasks;
using LibriKeep.Core.Domain.Entities;
using LibriKeep.Core.Domain.Enums;

namespace LibriKeep.Core.Application.Common.Interfaces
{
    public interface IUsuarioRepository
    {
        Task<Usuario?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Usuario?> GetByDniAsync(string dni, CancellationToken cancellationToken = default);
        Task AddAsync(Usuario usuario, CancellationToken cancellationToken = default);
    }

    public interface IEjemplarRepository
    {
        Task<Ejemplar?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Ejemplar?> GetByCodigoBarrasAsync(string codigoBarras, CancellationToken cancellationToken = default);
    }

    public interface IPrestamoRepository
    {
        Task<Prestamo?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task AddAsync(Prestamo prestamo, CancellationToken cancellationToken = default);
        Task<Prestamo?> GetActiveLoanByCodigoBarrasAsync(string codigoBarras, CancellationToken cancellationToken = default);
        Task<int> GetActiveLoansCountForUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default);
    }

    public interface ILibroRepository
    {
        Task<Libro?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Libro?> GetByIsbnAsync(string isbn, CancellationToken cancellationToken = default);
        Task<bool> AnyAsync(int id, CancellationToken cancellationToken = default);
        Task AddAsync(Libro libro, CancellationToken cancellationToken = default);
    }

    public interface ISancionRepository
    {
        Task AddAsync(Sancion sancion, CancellationToken cancellationToken = default);
    }

    public interface IReservaRepository
    {
        Task AddAsync(Reserva reserva, CancellationToken cancellationToken = default);
        Task<Reserva?> GetFirstActiveReservationForLibroAsync(int libroId, CancellationToken cancellationToken = default);
        Task<bool> HasActiveReservationsForLibroAsync(int libroId, CancellationToken cancellationToken = default);
    }

    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}
