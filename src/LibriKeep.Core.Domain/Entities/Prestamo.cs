using System;
using LibriKeep.Core.Domain.Enums;
using LibriKeep.Core.Domain.Exceptions;

namespace LibriKeep.Core.Domain.Entities
{
    public class Prestamo
    {
        public int Id { get; private set; }
        public int UsuarioId { get; private set; }
        public Usuario Usuario { get; private set; } = null!;

        public int EjemplarId { get; private set; }
        public Ejemplar Ejemplar { get; private set; } = null!;

        public DateTime FechaSalida { get; private set; }
        public DateTime FechaMaxDevolucion { get; private set; }
        public DateTime? FechaDevolucionEfectiva { get; private set; }
        public EstadoPrestamo Estado { get; private set; }

        // Constructor para EF Core
        #pragma warning disable CS8618
        private Prestamo() { }
        #pragma warning restore CS8618

        public Prestamo(int usuarioId, int ejemplarId, DateTime fechaSalida, int diasPrestamoDefault)
        {
            UsuarioId = usuarioId;
            EjemplarId = ejemplarId;
            FechaSalida = fechaSalida;
            FechaMaxDevolucion = fechaSalida.AddDays(diasPrestamoDefault);
            Estado = EstadoPrestamo.Activo;
        }

        public Prestamo(int usuarioId, int ejemplarId, DateTime fechaSalida, DateTime fechaMaxDevolucion)
        {
            UsuarioId = usuarioId;
            EjemplarId = ejemplarId;
            FechaSalida = fechaSalida;
            FechaMaxDevolucion = fechaMaxDevolucion;
            Estado = EstadoPrestamo.Activo;
        }

        public Sancion? ProcesarDevolucion(DateTime fechaDevolucionEfectiva, bool tieneReservasActivas, EstadoEjemplar entregaFisica)
        {
            if (Estado != EstadoPrestamo.Activo && Estado != EstadoPrestamo.Demorado)
            {
                throw new DomainException("ERR_LOAN_ALREADY_RETURNED", "El préstamo no está activo y ya ha sido devuelto.");
            }

            FechaDevolucionEfectiva = fechaDevolucionEfectiva;
            Estado = EstadoPrestamo.Devuelto;

            // Actualizar el estado físico del ejemplar (RN-05)
            Ejemplar.Devolver(tieneReservasActivas, entregaFisica);

            // RN-04: Algoritmo Inmutable de Cálculo de Penalizaciones
            // Comparar solo fechas sin hora para cálculo calendario exacto
            if (fechaDevolucionEfectiva.Date > FechaMaxDevolucion.Date)
            {
                int diasRetraso = (fechaDevolucionEfectiva.Date - FechaMaxDevolucion.Date).Days;
                if (diasRetraso > 0)
                {
                    int diasSancion = diasRetraso * 2;
                    
                    // Crear la sanción
                    var sancion = new Sancion(UsuarioId, Id, fechaDevolucionEfectiva, diasSancion);
                    
                    // Modificar estado del usuario a BloqueoTemporal
                    Usuario.BloquearTemporalmente();
                    
                    return sancion;
                }
            }

            return null;
        }

        public void MarcarDemorado()
        {
            if (Estado == EstadoPrestamo.Activo)
            {
                Estado = EstadoPrestamo.Demorado;
            }
        }
    }
}
