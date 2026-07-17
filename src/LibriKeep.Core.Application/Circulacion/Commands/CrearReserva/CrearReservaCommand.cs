using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using LibriKeep.Core.Application.Common.Interfaces;
using LibriKeep.Core.Domain.Entities;
using LibriKeep.Core.Domain.Enums;
using LibriKeep.Core.Domain.Exceptions;

namespace LibriKeep.Core.Application.Circulacion.Commands.CrearReserva
{
    public record CrearReservaCommand(int UsuarioId, int LibroId) : IRequest<Reserva>;

    public class CrearReservaCommandHandler : IRequestHandler<CrearReservaCommand, Reserva>
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly ILibroRepository _libroRepository;
        private readonly IReservaRepository _reservaRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CrearReservaCommandHandler(
            IUsuarioRepository usuarioRepository,
            ILibroRepository libroRepository,
            IReservaRepository reservaRepository,
            IUnitOfWork unitOfWork)
        {
            _usuarioRepository = usuarioRepository;
            _libroRepository = libroRepository;
            _reservaRepository = reservaRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Reserva> Handle(CrearReservaCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var usuario = await _usuarioRepository.GetByIdAsync(request.UsuarioId, cancellationToken);
                if (usuario == null)
                {
                    throw new DomainException("ERR_USER_NOT_FOUND", $"El usuario con ID {request.UsuarioId} no existe.");
                }

                var libro = await _libroRepository.GetByIdAsync(request.LibroId, cancellationToken);
                if (libro == null)
                {
                    throw new DomainException("ERR_BOOK_NOT_FOUND", $"El libro con ID {request.LibroId} no existe.");
                }

                // RN-02: Control de Bloqueo por Morosidad o Sanciones Activas
                usuario.ValidarCapacidadCirculacion(DateTime.UtcNow);

                // Validar si realmente no hay ejemplares disponibles
                // Si el libro tiene ejemplares disponibles para préstamo inmediato, denegar la reserva.
                bool hasAvailableCopies = libro.Ejemplares.Any(e => e.Estado == EstadoEjemplar.Disponible);
                if (hasAvailableCopies)
                {
                    throw new DomainException("ERR_BOOK_AVAILABLE", 
                        $"El libro '{libro.Titulo}' cuenta con ejemplares físicos disponibles en estanterías. No es posible reservarlo.");
                }

                // Determinar posición en la cola de reservas
                // Contar cuántas reservas activas existen para este libro
                // Para esto se puede usar la propiedad de navegación o consultar a base de datos.
                int activeReservationsCount = libro.Reservas.Count(r => r.Estado == EstadoReserva.Activa);
                int posicion = activeReservationsCount + 1;

                var reserva = new Reserva(usuario.Id, libro.Id, DateTime.UtcNow, posicion);
                await _reservaRepository.AddAsync(reserva, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return reserva;
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}
