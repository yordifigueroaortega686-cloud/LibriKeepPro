export interface AutorDto {
  id: number;
  nombre: string;
  nacionalidad?: string;
}

export interface CategoriaDto {
  id: number;
  nombre: string;
  descripcion?: string;
}

export interface EditorialDto {
  id: number;
  nombre: string;
}

export type TipoMaterial = 'LibroFisico' | 'LibroDigital' | 'Revista' | 'Tesis' | 'Otro';

export interface BookDto {
  id: number;
  titulo: string;
  isbn: string;
  autor: AutorDto;
  categoria: CategoriaDto;
  editorial: EditorialDto;
  fechaPublicacion: string;
  idioma?: string;
  pais?: string;
  tipoMaterial: TipoMaterial;
  totalCopias: number;
  copiasDisponibles: number;
}

export type EstadoEjemplar = 'Disponible' | 'Prestado' | 'EnSala' | 'Mantenimiento' | 'Pérdida' | 'Reservado';

export interface EjemplarDto {
  id: number;
  libroId: number;
  codigoBarras: string;
  estado: EstadoEjemplar;
  ubicacionFisica?: string;
  observaciones?: string;
}

export interface BookDetail {
  libro: BookDto;
  ejemplares: EjemplarDto[];
}

export interface PaginatedBooks {
  totalItems: number;
  page: number;
  pageSize: number;
  totalPages: number;
  items: BookDto[];
}

export type TipoMiembro = 'Alumno' | 'Docente' | 'PersonalAdministrativo' | 'Bibliotecario' | 'Externo';
export type RolUsuario = 'Lector' | 'Bibliotecario' | 'Administrador';
export type EstadoUsuario = 'Activo' | 'BloqueoTemporal' | 'Suspendido' | 'Inactivo';

export interface UsuarioDto {
  id: number;
  dni: string;
  nombreCompleto: string;
  email: string;
  telefono?: string;
  tipoMiembro: TipoMiembro;
  rol: RolUsuario;
  estado: EstadoUsuario;
  password?: string;
}

export interface LoginResponse {
  token: string;
  usuario: UsuarioDto;
}

export type EstadoPrestamo = 'Activo' | 'Devuelto' | 'Demorado';

export interface PrestamoDto {
  id: number;
  usuarioId: number;
  usuarioNombre: string;
  ejemplarId: number;
  ejemplarCodigoBarras: string;
  libroTitulo: string;
  fechaSalida: string;
  fechaMaxDevolucion: string;
  fechaDevolucionEfectiva?: string | null;
  estado: EstadoPrestamo;
}

export interface PaginatedPrestamos {
  totalItems: number;
  page: number;
  pageSize: number;
  totalPages: number;
  items: PrestamoDto[];
}

export type EstadoSancion = 'Activa' | 'Expirada' | 'Levantada';

export interface SancionDto {
  id: number;
  usuarioId: number;
  prestamoId: number;
  fechaInicio: string;
  fechaFin: string;
  diasSancion: number;
  estado: EstadoSancion;
}

export type EstadoReserva = 'Activa' | 'Procesada' | 'Cancelada' | 'Vencida';

export interface ReservaDto {
  id: number;
  usuarioId: number;
  usuarioNombre: string;
  libroId: number;
  libroTitulo: string;
  fechaReserva: string;
  posicionCola: number;
  estado: EstadoReserva;
}

export interface DevolucionResponseDto {
  prestamoId: number;
  fechaDevolucionEfectiva: string;
  diasRetraso: number;
  penalizacionAplicada: boolean;
  sancion?: SancionDto | null;
  nuevoEstadoEjemplar: EstadoEjemplar;
}

export interface ErrorResponse {
  code: string;
  title: string;
  detail: string;
}

export interface ConfiguracionDto {
  maxPrestamosAlumno: number;
  maxPrestamosDocente: number;
  maxPrestamosAdministrativo: number;
  diasPrestamoDefecto: number;
  diasSuspensionPorDiaRetraso: number;
  horasGraciaReserva: number;
}

export interface EstadisticasDto {
  totalEjemplares: number;
  prestamosActivos: number;
  tasaMorosidad: number;
  usuariosBloqueados: number;
}

export interface ReporteMasSolicitadoDto {
  rank: number;
  libroId: number;
  titulo: string;
  autorNombre: string;
  totalPrestamos: number;
}

export interface ReporteEjemplarProblemaDto {
  ejemplarId: number;
  codigoBarras: string;
  libroTitulo: string;
  estado: 'Mantenimiento' | 'Pérdida';
  observaciones?: string;
  ultimaFechaActualizacion: string;
}

export interface ReporteUsuarioMorosoDto {
  usuarioId: number;
  dni: string;
  nombreCompleto: string;
  email: string;
  prestamosVencidosCount: number;
  sancionesActivasCount: number;
  estadoUsuario: string;
}
