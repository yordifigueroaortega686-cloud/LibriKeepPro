using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using LibriKeep.Core.Application.Common.Interfaces;
using LibriKeep.Core.Domain.Entities;
using LibriKeep.Core.Domain.Enums;
using LibriKeep.Core.Domain.Exceptions;

namespace LibriKeep.Core.Application.Circulacion.Commands.ProcesarDevolucion
{
    public record DevolucionResult(
        int PrestamoId,
        DateTime FechaDevolucionEfectiva,
        int DiasRetraso,
        bool PenalizacionAplicada,
        Sancion? Sancion,
        EstadoEjemplar NuevoEstadoEjemplar
    );

    public record ProcesarDevolucionCommand(
        string CodigoBarras,
        EstadoEjemplar EstadoEntrega,
        string Observaciones
    ) : IRequest<DevolucionResult>;

    public class ProcesarDevolucionCommandHandler : IRequestHandler<ProcesarDevolucionCommand, DevolucionResult>
    {
        private readonly IPrestamoRepository _prestamoRepository;
        private readonly IReservaRepository _reservaRepository;
        private readonly ISancionRepository _sancionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ProcesarDevolucionCommandHandler(
            IPrestamoRepository prestamoRepository,
            IReservaRepository reservaRepository,
            ISancionRepository sancionRepository,
            IUnitOfWork unitOfWork)
        {
            _prestamoRepository = prestamoRepository;
            _reservaRepository = reservaRepository;
            _sancionRepository = sancionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<DevolucionResult> Handle(ProcesarDevolucionCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Obtener el préstamo activo del ejemplar por código de barras
                var prestamo = await _prestamoRepository.GetActiveLoanByCodigoBarrasAsync(request.CodigoBarras, cancellationToken);
                if (prestamo == null)
                {
                    throw new DomainException("ERR_NO_ACTIVE_LOAN", 
                        $"El ejemplar con código de barras '{request.CodigoBarras}' no tiene préstamos activos registrados.");
                }

                // Cargar ejemplar y libro relacionados con guardas de nulidad
                var ejemplar = prestamo.Ejemplar;
                if (ejemplar == null)
                {
                    throw new DomainException("ERR_CORRUPT_DATA", "El préstamo encontrado no tiene asignado un ejemplar físico válido.");
                }

                if (prestamo.Usuario == null)
                {
                    throw new DomainException("ERR_CORRUPT_DATA", "El préstamo encontrado no tiene asignado un lector o usuario válido.");
                }

                int libroId = ejemplar.LibroId;

                // RN-05: Evaluar si existen reservas activas en cola para el libro
                bool tieneReservas = await _reservaRepository.HasActiveReservationsForLibroAsync(libroId, cancellationToken);

                // Procesar devolución en la entidad
                var timestamp = DateTime.UtcNow;
                var sancion = prestamo.ProcesarDevolucion(timestamp, tieneReservas, request.EstadoEntrega);

                if (sancion != null)
                {
                    await _sancionRepository.AddAsync(sancion, cancellationToken);
                }

                // RN-05: Si hay reservas y el ejemplar quedó Reservado, despachar al primer lector
                if (tieneReservas && ejemplar.Estado == EstadoEjemplar.Reservado)
                {
                    var primeraReserva = await _reservaRepository.GetFirstActiveReservationForLibroAsync(libroId, cancellationToken);
                    if (primeraReserva != null)
                    {
                        primeraReserva.Procesar(); // Marca la reserva como procesada
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                int diasRetraso = 0;
                if (timestamp.Date > prestamo.FechaMaxDevolucion.Date)
                {
                    diasRetraso = (timestamp.Date - prestamo.FechaMaxDevolucion.Date).Days;
                }

                return new DevolucionResult(
                    prestamo.Id,
                    timestamp,
                    diasRetraso,
                    sancion != null,
                    sancion,
                    ejemplar.Estado
                );
            }
            catch (DomainException)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                var typeName = ex.GetType().Name;
                if (typeName.Contains("DbUpdateConcurrencyException"))
                {
                    throw new DomainException("ERR_CONCURRENCY_CONFLICT", 
                        "El estado del ejemplar cambió simultáneamente. Por favor, reintente la devolución.");
                }
                if (typeName.Contains("DbUpdateException"))
                {
                    throw new DomainException("ERR_PERSISTENCE_FAILED", 
                        "Fallo al guardar la devolución física en la base de datos.");
                }

                throw new DomainException("ERR_UNEXPECTED_ERROR", 
                    $"Error inesperado al devolver: {ex.Message}");
            }
        }
    }
}
