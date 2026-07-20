using System;
using LibriKeep.Core.Domain.Enums;

namespace LibriKeep.Core.Domain.Entities
{
    public class Reserva
    {
        public int Id { get; private set; }
        public int UsuarioId { get; private set; }
        public Usuario Usuario { get; private set; } = null!;

        public int LibroId { get; private set; }
        public Libro Libro { get; private set; } = null!;

        public DateTime FechaReserva { get; private set; }
        public int PosicionCola { get; private set; }
        public EstadoReserva Estado { get; private set; }

        // Constructor para EF Core
        #pragma warning disable CS8618
        private Reserva() { }
        #pragma warning restore CS8618

        public Reserva(int usuarioId, int libroId, DateTime fechaReserva, int posicionCola)
        {
            UsuarioId = usuarioId;
            LibroId = libroId;
            FechaReserva = fechaReserva.Kind == DateTimeKind.Utc ? fechaReserva : DateTime.SpecifyKind(fechaReserva, DateTimeKind.Utc);
            PosicionCola = posicionCola;
            Estado = EstadoReserva.Activa;
        }

        public void Procesar()
        {
            Estado = EstadoReserva.Procesada;
        }

        public void Cancelar()
        {
            Estado = EstadoReserva.Cancelada;
        }

        public void Expirar()
        {
            Estado = EstadoReserva.Vencida;
        }
    }
}
