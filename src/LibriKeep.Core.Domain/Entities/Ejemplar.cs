using LibriKeep.Core.Domain.Enums;
using LibriKeep.Core.Domain.Exceptions;

namespace LibriKeep.Core.Domain.Entities
{
    public class Ejemplar
    {
        public int Id { get; private set; }
        public int LibroId { get; private set; }
        public Libro Libro { get; private set; } = null!;
        public string CodigoBarras { get; private set; }
        public EstadoEjemplar Estado { get; private set; }
        public string UbicacionFisica { get; private set; }
        public string Observaciones { get; private set; }

        // Constructor para EF Core
        #pragma warning disable CS8618
        private Ejemplar() { }
        #pragma warning restore CS8618

        public Ejemplar(int libroId, string codigoBarras, string ubicacionFisica, string observaciones)
        {
            LibroId = libroId;
            CodigoBarras = codigoBarras;
            UbicacionFisica = ubicacionFisica;
            Observaciones = observaciones;
            Estado = EstadoEjemplar.Disponible;
        }

        public void Update(EstadoEjemplar estado, string ubicacionFisica, string observaciones)
        {
            Estado = estado;
            UbicacionFisica = ubicacionFisica;
            Observaciones = observaciones;
        }

        public void Prestar()
        {
            // RN-01: Control de Estados Invariables del Activo (Ejemplar)
            if (Estado != EstadoEjemplar.Disponible)
            {
                throw new DomainException("ERR_COPY_NOT_AVAILABLE", 
                    $"El ejemplar con código de barras '{CodigoBarras}' no se encuentra disponible para préstamo. Estado actual: {Estado}.");
            }

            Estado = EstadoEjemplar.Prestado;
        }

        public void Devolver(bool tieneReservasActivas, EstadoEjemplar entregaFisica)
        {
            // Si el estado de entrega física es de mantenimiento o pérdida, ese estado prevalece.
            if (entregaFisica == EstadoEjemplar.Mantenimiento || entregaFisica == EstadoEjemplar.Pérdida)
            {
                Estado = entregaFisica;
                return;
            }

            // RN-05: Regla de Prioridad y Consistencia en Devolución de Reservas
            if (tieneReservasActivas)
            {
                Estado = EstadoEjemplar.Reservado;
            }
            else
            {
                Estado = EstadoEjemplar.Disponible;
            }
        }
    }
}
