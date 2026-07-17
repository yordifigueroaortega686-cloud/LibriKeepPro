namespace LibriKeep.Core.Domain.Enums
{
    public enum TipoMiembro
    {
        Alumno,
        Docente,
        PersonalAdministrativo,
        Bibliotecario,
        Externo
    }

    public enum Rol
    {
        Lector,
        Bibliotecario,
        Administrador
    }

    public enum EstadoUsuario
    {
        Activo,
        BloqueoTemporal,
        Suspendido,
        Inactivo
    }

    public enum EstadoEjemplar
    {
        Disponible,
        Prestado,
        EnSala,
        Mantenimiento,
        Pérdida,
        Reservado
    }

    public enum TipoMaterial
    {
        LibroFisico,
        LibroDigital,
        Revista,
        Tesis,
        Otro
    }

    public enum EstadoPrestamo
    {
        Activo,
        Devuelto,
        Demorado
    }

    public enum EstadoSancion
    {
        Activa,
        Expirada,
        Levantada
    }

    public enum EstadoReserva
    {
        Activa,
        Procesada,
        Cancelada,
        Vencida
    }
}
