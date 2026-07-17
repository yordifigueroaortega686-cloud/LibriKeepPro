using System.Collections.Generic;

namespace LibriKeep.Core.Domain.Entities
{
    public class Categoria
    {
        public int Id { get; private set; }
        public string Nombre { get; private set; }
        public string Descripcion { get; private set; }
        public ICollection<Libro> Libros { get; private set; } = new List<Libro>();

        // Constructor para EF Core
        #pragma warning disable CS8618
        private Categoria() { }
        #pragma warning restore CS8618

        public Categoria(string nombre, string descripcion)
        {
            Nombre = nombre;
            Descripcion = descripcion;
        }

        public void Update(string nombre, string descripcion)
        {
            Nombre = nombre;
            Descripcion = descripcion;
        }
    }
}
