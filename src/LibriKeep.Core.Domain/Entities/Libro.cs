using System;
using System.Collections.Generic;
using LibriKeep.Core.Domain.Enums;
using LibriKeep.Core.Domain.Exceptions;

namespace LibriKeep.Core.Domain.Entities
{
    public class Libro
    {
        public int Id { get; private set; }
        public string Titulo { get; private set; }
        public string Isbn { get; private set; }
        
        public int AutorId { get; private set; }
        public Autor Autor { get; private set; } = null!;

        public int CategoriaId { get; private set; }
        public Categoria Categoria { get; private set; } = null!;

        public int EditorialId { get; private set; }
        public Editorial Editorial { get; private set; } = null!;

        public DateTime FechaPublicacion { get; private set; }
        public string Idioma { get; private set; }
        public string Pais { get; private set; }
        public TipoMaterial TipoMaterial { get; private set; }

        public ICollection<Ejemplar> Ejemplares { get; private set; } = new List<Ejemplar>();
        public ICollection<Reserva> Reservas { get; private set; } = new List<Reserva>();

        // Constructor para EF Core
        #pragma warning disable CS8618
        private Libro() { }
        #pragma warning restore CS8618

        public Libro(string titulo, string isbn, int autorId, int categoriaId, int editorialId, DateTime fechaPublicacion, string idioma, string pais, TipoMaterial tipoMaterial)
        {
            ValidarIsbn(isbn);
            
            Titulo = titulo;
            Isbn = isbn;
            AutorId = autorId;
            CategoriaId = categoriaId;
            EditorialId = editorialId;
            FechaPublicacion = fechaPublicacion;
            Idioma = idioma;
            Pais = pais;
            TipoMaterial = tipoMaterial;
        }

        public void Update(string titulo, string isbn, int autorId, int categoriaId, int editorialId, DateTime fechaPublicacion, string idioma, string pais, TipoMaterial tipoMaterial)
        {
            ValidarIsbn(isbn);

            Titulo = titulo;
            Isbn = isbn;
            AutorId = autorId;
            CategoriaId = categoriaId;
            EditorialId = editorialId;
            FechaPublicacion = fechaPublicacion;
            Idioma = idioma;
            Pais = pais;
            TipoMaterial = tipoMaterial;
        }

        private static void ValidarIsbn(string isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn))
            {
                throw new DomainException("ERR_INVALID_ISBN", "El ISBN no puede estar vacío.");
            }

            // Normalización simple (remover guiones)
            var cleanIsbn = isbn.Replace("-", "").Replace(" ", "");
            if (cleanIsbn.Length != 10 && cleanIsbn.Length != 13)
            {
                throw new DomainException("ERR_INVALID_ISBN_FORMAT", "El ISBN debe tener 10 o 13 caracteres numéricos.");
            }
        }
    }
}
