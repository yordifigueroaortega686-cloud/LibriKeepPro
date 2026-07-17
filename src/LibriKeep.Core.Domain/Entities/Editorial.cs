using System.Collections.Generic;

namespace LibriKeep.Core.Domain.Entities
{
    public class Editorial
    {
        public int Id { get; private set; }
        public string Nombre { get; private set; }
        public ICollection<Libro> Libros { get; private set; } = new List<Libro>();

        // Constructor para EF Core
        #pragma warning disable CS8618
        private Editorial() { }
        #pragma warning restore CS8618

        public Editorial(string nombre)
        {
            Nombre = nombre;
        }

        public void Update(string nombre)
        {
            Nombre = nombre;
        }
    }
}
