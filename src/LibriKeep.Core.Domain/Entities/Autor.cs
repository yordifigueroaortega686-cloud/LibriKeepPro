using System.Collections.Generic;

namespace LibriKeep.Core.Domain.Entities
{
    public class Autor
    {
        public int Id { get; private set; }
        public string Nombre { get; private set; }
        public string Nacionalidad { get; private set; }
        public ICollection<Libro> Libros { get; private set; } = new List<Libro>();

        // Constructor para EF Core
        #pragma warning disable CS8618
        private Autor() { }
        #pragma warning restore CS8618

        public Autor(string nombre, string nacionalidad)
        {
            Nombre = nombre;
            Nacionalidad = nacionalidad;
        }

        public void Update(string nombre, string nacionalidad)
        {
            Nombre = nombre;
            Nacionalidad = nacionalidad;
        }
    }
}
