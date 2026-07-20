using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using LibriKeep.Core.Application.Common.Interfaces;
using LibriKeep.Core.Domain.Entities;
using LibriKeep.Core.Domain.Enums;
using LibriKeep.Core.Domain.Exceptions;

namespace LibriKeep.Core.Application.Catalogacion.Commands.CrearLibro
{
    public record CrearLibroCommand(
        string Titulo,
        string Isbn,
        int AutorId,
        int CategoriaId,
        int EditorialId,
        DateTime FechaPublicacion,
        string Idioma,
        string Pais,
        TipoMaterial TipoMaterial,
        string? AutorNombre = null,
        string? CategoriaNombre = null,
        string? EditorialNombre = null
    ) : IRequest<Libro>;

    public class CrearLibroCommandHandler : IRequestHandler<CrearLibroCommand, Libro>
    {
        private readonly ILibroRepository _libroRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CrearLibroCommandHandler(
            ILibroRepository libroRepository,
            IUnitOfWork unitOfWork)
        {
            _libroRepository = libroRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Libro> Handle(CrearLibroCommand request, CancellationToken cancellationToken)
        {
            // RN-1.3: Validación de ISBN
            if (string.IsNullOrWhiteSpace(request.Isbn) || request.Isbn.Replace("-", "").Length < 10 || request.Isbn.Any(c => !char.IsDigit(c) && c != '-'))
            {
                throw new DomainException("ERR_INVALID_ISBN", "El formato del ISBN ingresado no es válido o contiene caracteres alfabéticos (RN-1.3).");
            }

            var existing = await _libroRepository.GetByIsbnAsync(request.Isbn, cancellationToken);
            if (existing != null)
            {
                throw new DomainException("ERR_DUPLICATE_ISBN", "El libro con este ISBN ya existe.");
            }

            var utcDate = request.FechaPublicacion.Kind == DateTimeKind.Utc
                ? request.FechaPublicacion
                : DateTime.SpecifyKind(request.FechaPublicacion, DateTimeKind.Utc);

            var libro = new Libro(
                request.Titulo,
                request.Isbn,
                request.AutorId > 0 ? request.AutorId : 1,
                request.CategoriaId > 0 ? request.CategoriaId : 1,
                request.EditorialId > 0 ? request.EditorialId : 1,
                utcDate,
                string.IsNullOrWhiteSpace(request.Idioma) ? "Español" : request.Idioma,
                string.IsNullOrWhiteSpace(request.Pais) ? "Perú" : request.Pais,
                request.TipoMaterial
            );

            await _libroRepository.AddAsync(libro, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return libro;
        }
    }
}
