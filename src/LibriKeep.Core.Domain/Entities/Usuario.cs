using System;
using System.Collections.Generic;
using System.Linq;
using LibriKeep.Core.Domain.Enums;
using LibriKeep.Core.Domain.Exceptions;

namespace LibriKeep.Core.Domain.Entities
{
    public class Usuario
    {
        public int Id { get; private set; }
        public string Dni { get; private set; }
        public string NombreCompleto { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public string Telefono { get; private set; }
        public TipoMiembro TipoMiembro { get; private set; }
        public Rol Rol { get; private set; }
        public EstadoUsuario Estado { get; private set; }

        public ICollection<Prestamo> Prestamos { get; private set; } = new List<Prestamo>();
        public ICollection<Sancion> Sanciones { get; private set; } = new List<Sancion>();
        public ICollection<Reserva> Reservas { get; private set; } = new List<Reserva>();

        // Constructor para EF Core
        #pragma warning disable CS8618
        private Usuario() { }
        #pragma warning restore CS8618

        public Usuario(string dni, string nombreCompleto, string email, string passwordHash, string telefono, TipoMiembro tipoMiembro, Rol rol)
        {
            Dni = dni;
            NombreCompleto = nombreCompleto;
            Email = email;
            PasswordHash = passwordHash;
            Telefono = telefono;
            TipoMiembro = tipoMiembro;
            Rol = rol;
            Estado = EstadoUsuario.Activo;
        }

        public void Update(string nombreCompleto, string email, string telefono, TipoMiembro tipoMiembro, Rol rol, EstadoUsuario estado)
        {
            NombreCompleto = nombreCompleto;
            Email = email;
            Telefono = telefono;
            TipoMiembro = tipoMiembro;
            Rol = rol;
            Estado = estado;
        }

        public void Suspender()
        {
            Estado = EstadoUsuario.Suspendido;
        }

        public void Activar()
        {
            Estado = EstadoUsuario.Activo;
        }

        public void BloquearTemporalmente()
        {
            Estado = EstadoUsuario.BloqueoTemporal;
        }

        public int GetMaxPrestamosPermitidos()
        {
            return TipoMiembro switch
            {
                TipoMiembro.Alumno => 3,
                TipoMiembro.Docente => 5,
                TipoMiembro.PersonalAdministrativo => 3,
                _ => 3
            };
        }

        public bool TieneSancionActiva(DateTime currentUtc)
        {
            return Sanciones.Any(s => s.Estado == EstadoSancion.Activa && currentUtc >= s.FechaInicio && currentUtc <= s.FechaFin);
        }

        public bool TienePrestamoVencido(DateTime currentUtc)
        {
            return Prestamos.Any(p => p.Estado == EstadoPrestamo.Activo && currentUtc > p.FechaMaxDevolucion);
        }

        public bool TieneMultaImpagada()
        {
            // RN-02 indica "Ningún usuario con una multa impagada...".
            // Para simplificar, consideramos las sanciones no expiradas o si el estado del usuario es bloqueado/sancionado.
            // Si hay alguna sanción activa en el dominio, se considera moroso/sancionado.
            return Estado == EstadoUsuario.Suspendido || Estado == EstadoUsuario.BloqueoTemporal;
        }

        public void ValidarCapacidadCirculacion(DateTime currentUtc)
        {
            // RN-02: Control de Bloqueo por Morosidad o Sanciones Activas
            if (Estado == EstadoUsuario.Inactivo)
            {
                throw new DomainException("ERR_USER_INACTIVE", "El usuario está inactivo.");
            }

            if (TieneSancionActiva(currentUtc))
            {
                var activeSanction = Sanciones.First(s => s.Estado == EstadoSancion.Activa && currentUtc >= s.FechaInicio && currentUtc <= s.FechaFin);
                throw new DomainException("ERR_USER_SANCTIONED", 
                    $"El usuario {NombreCompleto} cuenta con una sanción activa vigente hasta {activeSanction.FechaFin:dd/MM/yyyy}.");
            }

            if (TienePrestamoVencido(currentUtc))
            {
                throw new DomainException("ERR_USER_OVERDUE_LOANS", 
                    $"El usuario {NombreCompleto} tiene préstamos vencidos no devueltos.");
            }

            if (TieneMultaImpagada())
            {
                throw new DomainException("ERR_USER_UNPAID_FINES", 
                    $"El usuario {NombreCompleto} tiene deudas o suspensiones pendientes por regularizar.");
            }
        }

        public void ValidarLmitePrestamos()
        {
            // RN-03: Límites Máximos y Cuotas de Préstamos Activos
            int prestamosActivosCount = Prestamos.Count(p => p.Estado == EstadoPrestamo.Activo || p.Estado == EstadoPrestamo.Demorado);
            int maxPermitidos = GetMaxPrestamosPermitidos();

            if (prestamosActivosCount >= maxPermitidos)
            {
                throw new DomainException("ERR_USER_MAX_LOANS_EXCEEDED", 
                    $"El usuario {NombreCompleto} ha alcanzado el límite máximo de préstamos activos ({prestamosActivosCount} de {maxPermitidos} permitidos).");
            }
        }
    }
}
