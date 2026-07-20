using System;
using LibriKeep.Core.Domain.Enums;

namespace LibriKeep.Core.Domain.Entities
{
    public class Sancion
    {
        public int Id { get; private set; }
        public int UsuarioId { get; private set; }
        public Usuario Usuario { get; private set; } = null!;

        public int? PrestamoId { get; private set; }
        public Prestamo? Prestamo { get; private set; }

        public DateTime FechaInicio { get; private set; }
        public DateTime FechaFin { get; private set; }
        public int DiasSancion { get; private set; }
        public EstadoSancion Estado { get; private set; }

        // Constructor para EF Core
        #pragma warning disable CS8618
        private Sancion() { }
        #pragma warning restore CS8618

        public Sancion(int usuarioId, int? prestamoId, DateTime fechaInicio, int diasSancion)
        {
            UsuarioId = usuarioId;
            PrestamoId = prestamoId;
            FechaInicio = fechaInicio.Kind == DateTimeKind.Utc ? fechaInicio : DateTime.SpecifyKind(fechaInicio, DateTimeKind.Utc);
            DiasSancion = diasSancion;
            FechaFin = FechaInicio.AddDays(diasSancion);
            Estado = EstadoSancion.Activa;
        }

        public void Expirar()
        {
            Estado = EstadoSancion.Expirada;
        }

        public void Levantar()
        {
            Estado = EstadoSancion.Levantada;
        }
    }
}
