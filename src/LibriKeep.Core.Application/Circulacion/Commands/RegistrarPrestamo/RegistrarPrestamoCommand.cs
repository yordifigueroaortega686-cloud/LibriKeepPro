using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using LibriKeep.Core.Application.Common.Interfaces;
using LibriKeep.Core.Domain.Entities;
using LibriKeep.Core.Domain.Exceptions;

namespace LibriKeep.Core.Application.Circulacion.Commands.RegistrarPrestamo
{
    public record RegistrarPrestamoCommand(int UsuarioId, int EjemplarId, DateTime? FechaLimiteDevolucion = null) : IRequest<Prestamo>;

    public class RegistrarPrestamoCommandHandler : IRequestHandler<RegistrarPrestamoCommand, Prestamo>
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IEjemplarRepository _ejemplarRepository;
        private readonly IPrestamoRepository _prestamoRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RegistrarPrestamoCommandHandler(
            IUsuarioRepository usuarioRepository,
            IEjemplarRepository ejemplarRepository,
            IPrestamoRepository prestamoRepository,
            IUnitOfWork unitOfWork)
        {
            _usuarioRepository = usuarioRepository;
            _ejemplarRepository = ejemplarRepository;
            _prestamoRepository = prestamoRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Prestamo> Handle(RegistrarPrestamoCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var usuario = await _usuarioRepository.GetByIdAsync(request.UsuarioId, cancellationToken);
                if (usuario == null)
                {
                    usuario = await _usuarioRepository.GetByDniAsync(request.UsuarioId.ToString(), cancellationToken);
                }

                if (usuario == null)
                {
                    throw new DomainException("ERR_USER_NOT_FOUND", $"El usuario con ID o DNI {request.UsuarioId} no existe.");
                }

                var ejemplar = await _ejemplarRepository.GetByIdAsync(request.EjemplarId, cancellationToken);
                if (ejemplar == null)
                {
                    throw new DomainException("ERR_COPY_NOT_FOUND", $"El ejemplar con ID {request.EjemplarId} no existe.");
                }

                // RN-02: Control de Bloqueo por Morosidad o Sanciones Activas
                // Cargamos conteo de préstamos activos para comprobar límites
                int activeLoansCount = await _prestamoRepository.GetActiveLoansCountForUsuarioAsync(request.UsuarioId, cancellationToken);
                
                // Realizamos validaciones sobre la entidad Usuario
                usuario.ValidarCapacidadCirculacion(DateTime.UtcNow);
                usuario.ValidarLmitePrestamos(activeLoansCount); // Verifica cantidad acumulada en base de datos

                // RN-01: Control de Estados Invariables del Activo (Ejemplar)
                ejemplar.Prestar(); // Lanza DomainException si no está Disponible

                // Registrar el préstamo (usando la fecha límite provista o por defecto 7 días de préstamo)
                var prestamo = request.FechaLimiteDevolucion.HasValue
                    ? new Prestamo(usuario.Id, ejemplar.Id, DateTime.UtcNow, request.FechaLimiteDevolucion.Value)
                    : new Prestamo(usuario.Id, ejemplar.Id, DateTime.UtcNow, 7);
                await _prestamoRepository.AddAsync(prestamo, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return prestamo;
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}
