import {
  LoginResponse, PaginatedBooks, BookDetail, BookDto, EjemplarDto,
  UsuarioDto, PrestamoDto, DevolucionResponseDto, ReservaDto,
  ConfiguracionDto, EstadisticasDto, ReporteMasSolicitadoDto,
  ReporteEjemplarProblemaDto, ReporteUsuarioMorosoDto, ErrorResponse, SancionDto,
  PaginatedPrestamos, RolUsuario, TipoMiembro
} from '../types/api';

const BASE_URL = import.meta.env.VITE_API_URL || '/api';

export class ApiError extends Error {
  constructor(public status: number, public errorResponse: ErrorResponse) {
    super(errorResponse.detail || errorResponse.title);
  }
}

// Bandera para usar mocks si el servidor real no responde
let useMockData = false;

export const setUseMockData = (value: boolean) => {
  useMockData = value;
};

export const getUseMockData = () => useMockData;

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  if (useMockData) {
    return getMockResponse<T>(path, options);
  }

  const token = localStorage.getItem('librikeep_token');
  const headers = new Headers(options.headers || {});

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  if (options.body && !(options.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json');
  }

  try {
    const response = await fetch(`${BASE_URL}${path}`, {
      ...options,
      headers
    });

    if (!response.ok) {
      let errResp: ErrorResponse = {
        code: 'ERR_UNKNOWN',
        title: 'Error Inesperado',
        detail: `Ocurrió un error en el servidor (HTTP ${response.status}).`
      };

      try {
        const json = await response.json();
        if (json.code) errResp = json;
      } catch {
        // Ignorar fallo de parseo
      }

      throw new ApiError(response.status, errResp);
    }

    if (response.status === 204) {
      return {} as T;
    }

    return await response.json();
  } catch (error) {
    if (error instanceof ApiError) {
      throw error;
    }

    console.warn("Servidor inalcanzable. Activando modo simulación (Mock Data)...");
    useMockData = true;
    return getMockResponse<T>(path, options);
  }
}

export const apiClient = {
  auth: {
    login: (email: string, password: string) =>
      request<LoginResponse>('/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password })
      })
  },
  opac: {
    search: (query?: string, autorId?: number, categoriaId?: number, page = 1, pageSize = 10) => {
      const params = new URLSearchParams();
      if (query) params.append('query', query);
      if (autorId) params.append('autorId', autorId.toString());
      if (categoriaId) params.append('categoriaId', categoriaId.toString());
      params.append('page', page.toString());
      params.append('pageSize', pageSize.toString());
      return request<PaginatedBooks>(`/opac/libros?${params.toString()}`);
    },
    getById: (id: number) => request<BookDetail>(`/opac/libros/${id}`)
  },
  cataloging: {
    createBook: (book: any) => request<BookDto>('/libros', { method: 'POST', body: JSON.stringify(book) }),
    updateBook: (id: number, book: any) => request<BookDto>(`/libros/${id}`, { method: 'PUT', body: JSON.stringify(book) }),
    deleteBook: (id: number) => request<void>(`/libros/${id}`, { method: 'DELETE' }),
    createEjemplar: (libroId: number, barcode: string, location: string, obs: string) =>
      request<EjemplarDto>(`/libros/${libroId}/ejemplares`, {
        method: 'POST',
        body: JSON.stringify({ codigoBarras: barcode, ubicacionFisica: location, observaciones: obs })
      }),
    updateEjemplar: (id: number, ejemplar: any) => request<EjemplarDto>(`/ejemplares/${id}`, { method: 'PUT', body: JSON.stringify(ejemplar) }),
    deleteEjemplar: (id: number) => request<void>(`/ejemplares/${id}`, { method: 'DELETE' }),
    getAutores: (query?: string) => request<any[]>(`/autores${query ? `?query=${query}` : ''}`),
    getCategorias: () => request<any[]>('/categorias'),
    getEditoriales: () => request<any[]>('/editoriales')
  },
  users: {
    list: (search?: string) => request<UsuarioDto[]>(`/usuarios${search ? `?search=${search}` : ''}`),
    create: (user: any) => request<UsuarioDto>('/usuarios', { method: 'POST', body: JSON.stringify(user) }),
    getProfile: (id: number) => request<{ usuario: UsuarioDto; prestamosActivosCount: number; prestamosPermitidosCount: number; tieneMultaImpagada: boolean; tieneSancionActiva: boolean; tienePrestamoVencido: boolean }>(`/usuarios/${id}/perfil`),
    getSanciones: (id: number) => request<SancionDto[]>(`/usuarios/${id}/sanciones`)
  },
  circulation: {
    getLoans: (usuarioId?: number, estado?: string) => {
      const params = new URLSearchParams();
      if (usuarioId) params.append('usuarioId', usuarioId.toString());
      if (estado) params.append('estado', estado);
      return request<PaginatedPrestamos>(`/circulacion/prestamos?${params.toString()}`);
    },
    loan: (usuarioId: number, ejemplarId: number, fechaDevolucion?: string) =>
      request<PrestamoDto>('/circulacion/prestamos', {
        method: 'POST',
        body: JSON.stringify({ usuarioId, ejemplarId, fechaDevolucion })
      }),
    return: (barcode: string, status: string, obs: string) =>
      request<DevolucionResponseDto>('/circulacion/devoluciones', {
        method: 'POST',
        body: JSON.stringify({ codigoBarras: barcode, estadoEntrega: status, observaciones: obs })
      }),
    reserve: (usuarioId: number, libroId: number) =>
      request<ReservaDto>('/circulacion/reservas', {
        method: 'POST',
        body: JSON.stringify({ usuarioId, libroId })
      })
  },
  config: {
    get: () => request<ConfiguracionDto>('/configuracion'),
    update: (config: ConfiguracionDto) => request<ConfiguracionDto>('/configuracion', { method: 'PUT', body: JSON.stringify(config) })
  },
  reports: {
    getKpis: () => request<EstadisticasDto>('/reportes/estadisticas'),
    getPopular: () => request<ReporteMasSolicitadoDto[]>('/reportes/mas-solicitados'),
    getProblematic: () => request<ReporteEjemplarProblemaDto[]>('/reportes/mantenimiento-perdidos'),
    getOverdueUsers: () => request<ReporteUsuarioMorosoDto[]>('/reportes/morosos')
  }
};

// ==========================================
// MOCK DATA GENERATOR FOR OFFLINE PITCHES
// ==========================================

const mockBooks: BookDto[] = [
  {
    id: 1,
    titulo: "Clean Architecture",
    isbn: "9780134494166",
    autor: { id: 1, nombre: "Robert C. Martin", nacionalidad: "Estadounidense" },
    categoria: { id: 1, nombre: "Ingeniería de Software", descripcion: "Desarrollo de software" },
    editorial: { id: 1, nombre: "Prentice Hall" },
    fechaPublicacion: "2017-09-10",
    idioma: "Español",
    pais: "Estados Unidos",
    tipoMaterial: "LibroFisico",
    totalCopias: 3,
    copiasDisponibles: 2
  },
  {
    id: 2,
    titulo: "Clean Code",
    isbn: "9780132350884",
    autor: { id: 1, nombre: "Robert C. Martin", nacionalidad: "Estadounidense" },
    categoria: { id: 1, nombre: "Ingeniería de Software", descripcion: "Desarrollo de software" },
    editorial: { id: 1, nombre: "Prentice Hall" },
    fechaPublicacion: "2008-08-01",
    idioma: "Español",
    pais: "Estados Unidos",
    tipoMaterial: "LibroFisico",
    totalCopias: 5,
    copiasDisponibles: 0
  },
  {
    id: 3,
    titulo: "Design Patterns",
    isbn: "9780201633610",
    autor: { id: 2, nombre: "Erich Gamma", nacionalidad: "Suizo" },
    categoria: { id: 1, nombre: "Ingeniería de Software", descripcion: "Desarrollo de software" },
    editorial: { id: 2, nombre: "Addison-Wesley" },
    fechaPublicacion: "1994-10-21",
    idioma: "Inglés",
    pais: "Estados Unidos",
    tipoMaterial: "LibroFisico",
    totalCopias: 2,
    copiasDisponibles: 1
  }
];

const mockEjemplares: Record<number, EjemplarDto[]> = {
  1: [
    { id: 1, libroId: 1, codigoBarras: "9780134494166-C1", estado: "Disponible", ubicacionFisica: "Estante A-4" },
    { id: 2, libroId: 1, codigoBarras: "9780134494166-C2", estado: "Prestado", ubicacionFisica: "Estante A-4" },
    { id: 3, libroId: 1, codigoBarras: "9780134494166-C3", estado: "Disponible", ubicacionFisica: "Estante A-4" }
  ],
  2: [
    { id: 4, libroId: 2, codigoBarras: "9780132350884-C1", estado: "Prestado", ubicacionFisica: "Estante B-2" },
    { id: 5, libroId: 2, codigoBarras: "9780132350884-C2", estado: "Prestado", ubicacionFisica: "Estante B-2" },
    { id: 6, libroId: 2, codigoBarras: "9780132350884-C3", estado: "Mantenimiento", ubicacionFisica: "Taller Reparación", observaciones: "Portada rota" }
  ],
  3: [
    { id: 7, libroId: 3, codigoBarras: "9780201633610-C1", estado: "Disponible", ubicacionFisica: "Estante C-1" },
    { id: 8, libroId: 3, codigoBarras: "9780201633610-C2", estado: "Prestado", ubicacionFisica: "Estante C-1" }
  ]
};

const mockLoans: PrestamoDto[] = [
  {
    id: 101,
    usuarioId: 10,
    usuarioNombre: "Juan Pérez",
    ejemplarId: 2,
    ejemplarCodigoBarras: "9780134494166-C2",
    libroTitulo: "Clean Architecture",
    fechaSalida: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000).toISOString(),
    fechaMaxDevolucion: new Date(Date.now() + 2 * 24 * 60 * 60 * 1000).toISOString(),
    estado: "Activo"
  },
  {
    id: 102,
    usuarioId: 10,
    usuarioNombre: "Juan Pérez",
    ejemplarId: 4,
    ejemplarCodigoBarras: "9780132350884-C1",
    libroTitulo: "Clean Code",
    fechaSalida: new Date(Date.now() - 12 * 24 * 60 * 60 * 1000).toISOString(),
    fechaMaxDevolucion: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000).toISOString(), // VENCIDO
    estado: "Demorado"
  }
];

const mockUsersList: UsuarioDto[] = [
  {
    id: 1,
    dni: "88888888",
    nombreCompleto: "Admin Principal",
    email: "admin@uni.edu.pe",
    telefono: "+51999111222",
    tipoMiembro: "PersonalAdministrativo",
    rol: "Administrador",
    estado: "Activo",
    password: "adminPassword"
  },
  {
    id: 5,
    dni: "77777777",
    nombreCompleto: "María Gómez (Bibliotecaria)",
    email: "maria.gomez@biblioteca.edu.pe",
    telefono: "+51999777666",
    tipoMiembro: "Bibliotecario",
    rol: "Bibliotecario",
    estado: "Activo",
    password: "password"
  },
  {
    id: 10,
    dni: "71234567",
    nombreCompleto: "Juan Pérez",
    email: "alumno@uni.edu.pe",
    telefono: "+51999888777",
    tipoMiembro: "Alumno",
    rol: "Lector",
    estado: "BloqueoTemporal",
    password: "password"
  },
  {
    id: 99,
    dni: "99999999",
    nombreCompleto: "Lector Limite Excedido",
    email: "limite@uni.edu.pe",
    telefono: "+51999555444",
    tipoMiembro: "Alumno",
    rol: "Lector",
    estado: "Activo",
    password: "password"
  }
];

function getMockResponse<T>(path: string, options: RequestInit): T {
  const url = path.split('?')[0];

  if (url === '/usuarios') {
    const storedUser = localStorage.getItem('librikeep_user');
    const currentUser = storedUser ? JSON.parse(storedUser) : null;
    const isOperatorAdmin = currentUser?.rol === 'Administrador';

    if (options.method === 'POST') {
      const body = JSON.parse(options.body as string);
      const newUser: UsuarioDto = {
        id: Math.floor(Math.random() * 1000) + 20,
        dni: body.dni,
        nombreCompleto: body.nombreCompleto,
        email: body.email,
        telefono: body.telefono || "",
        tipoMiembro: body.tipoMiembro,
        rol: body.rol,
        estado: "Activo",
        password: body.password
      };
      mockUsersList.push(newUser);

      return {
        ...newUser,
        password: isOperatorAdmin ? newUser.password : undefined
      } as unknown as T;
    }

    return mockUsersList.map(u => ({
      ...u,
      password: isOperatorAdmin ? u.password : undefined
    })) as unknown as T;
  }

  if (url === '/auth/login') {
    const { email, password } = JSON.parse(options.body as string);
    
    // Simular error de credenciales incorrectas
    if (email === 'error@librikeep.com' || password === 'wrong-password') {
      throw new ApiError(401, {
        code: "ERR_AUTH_FAILED",
        title: "Fallo de Autenticación",
        detail: "Usuario o contraseña incorrectos."
      });
    }

    const isLibrarian = email.includes('bibliotecario');
    const isAdmin = email.includes('admin');

    const rol: RolUsuario = isAdmin ? 'Administrador' : (isLibrarian ? 'Bibliotecario' : 'Lector');
    const tipo: TipoMiembro = isLibrarian ? 'Bibliotecario' : 'Alumno';

    return {
      token: "mock-jwt-token-token",
      usuario: {
        id: isAdmin ? 1 : (isLibrarian ? 5 : 10),
        dni: "71234567",
        nombreCompleto: isAdmin ? "Admin Principal" : (isLibrarian ? "María Gómez (Bibliotecaria)" : "Juan Pérez"),
        email: email,
        telefono: isLibrarian ? "+51999777666" : (isAdmin ? "+51999111222" : "+51999888777"),
        tipoMiembro: tipo,
        rol: rol,
        estado: "Activo"
      }
    } as unknown as T;
  }

  if (url === '/opac/libros') {
    return {
      totalItems: mockBooks.length,
      page: 1,
      pageSize: 10,
      totalPages: 1,
      items: mockBooks
    } as unknown as T;
  }

  if (url.startsWith('/opac/libros/')) {
    const id = parseInt(url.split('/').pop() || '1');
    const book = mockBooks.find(b => b.id === id) || mockBooks[0];
    const copies = mockEjemplares[id] || [];
    return {
      libro: book,
      ejemplares: copies
    } as unknown as T;
  }

  if (url === '/usuarios/10/perfil') {
    return {
      usuario: {
        id: 10,
        dni: "71234567",
        nombreCompleto: "Juan Pérez",
        email: "alumno@uni.edu.pe",
        telefono: "+51999888777",
        tipoMiembro: "Alumno",
        rol: "Lector",
        estado: "BloqueoTemporal" // Para demostrar pantalla C de mora
      },
      prestamosActivosCount: 2,
      prestamosPermitidosCount: 3,
      tieneMultaImpagada: false,
      tieneSancionActiva: false,
      tienePrestamoVencido: true
    } as unknown as T;
  }

  if (url === '/categorias') {
    return [
      { id: 1, nombre: "Ingeniería de Software", descripcion: "Desarrollo de software" },
      { id: 2, nombre: "Seguridad de la Información", descripcion: "Ciberseguridad" },
      { id: 3, nombre: "Algoritmos y Estructuras", descripcion: "Estructuras de datos" }
    ] as unknown as T;
  }

  if (url === '/usuarios/10/sanciones') {
    return [
      {
        id: 202,
        usuarioId: 10,
        prestamoId: 102,
        fechaInicio: new Date().toISOString(),
        fechaFin: new Date(Date.now() + 10 * 24 * 60 * 60 * 1000).toISOString(),
        diasSancion: 10,
        estado: "Activa"
      }
    ] as unknown as T;
  }

  if (url === '/circulacion/prestamos') {
    if (options.method === 'POST') {
      const { usuarioId, ejemplarId } = JSON.parse(options.body as string);

      // Simular validación RN-02 y lanzar error para el modal H si el usuario es deudor (ID 10)
      if (usuarioId === 10) {
        throw new ApiError(400, {
          code: "ERR_USER_SANCTIONED",
          title: "Infracción de Regla de Negocio",
          detail: "El alumno Juan Pérez cuenta con un préstamo vencido (Clean Code) y su cuenta está suspendida."
        });
      }

      // Simular validación RN-03: Límite máximo de préstamos activos
      if (usuarioId === 99) {
        throw new ApiError(400, {
          code: "ERR_LOAN_LIMIT_EXCEEDED",
          title: "Infracción de Regla de Negocio",
          detail: "El usuario ha alcanzado el límite máximo de préstamos activos permitidos (RN-03)."
        });
      }

      // Encontrar el ejemplar, validar RN-01 (disponibilidad) y cambiar su estado a "Prestado"
      let foundBarcode = "BARCODE-OK";
      let foundTitle = "Libro Seleccionado";
      
      for (const bookId of Object.keys(mockEjemplares)) {
        const list = mockEjemplares[parseInt(bookId)];
        const e = list.find(x => x.id === ejemplarId);
        if (e) {
          if (e.estado !== 'Disponible') {
            throw new ApiError(400, {
              code: "ERR_COPY_NOT_AVAILABLE",
              title: "Infracción de Regla de Negocio",
              detail: `El ejemplar con código de barras ${e.codigoBarras} no se encuentra disponible (Estado actual: ${e.estado}).`
            });
          }
          e.estado = "Prestado";
          foundBarcode = e.codigoBarras;
          const book = mockBooks.find(b => b.id === e.libroId);
          if (book) {
            foundTitle = book.titulo;
            if (book.copiasDisponibles > 0) book.copiasDisponibles--;
          }
          break;
        }
      }

      const newLoan: PrestamoDto = {
        id: Math.floor(Math.random() * 1000),
        usuarioId,
        usuarioNombre: usuarioId === 5 ? "María Gómez (Bibliotecaria)" : "Usuario Registrado",
        ejemplarId,
        ejemplarCodigoBarras: foundBarcode,
        libroTitulo: foundTitle,
        fechaSalida: new Date().toISOString(),
        fechaMaxDevolucion: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString(),
        estado: "Activo"
      };

      mockLoans.push(newLoan);
      return newLoan as unknown as T;
    }
    return {
      totalItems: mockLoans.length,
      page: 1,
      pageSize: 10,
      totalPages: 1,
      items: mockLoans
    } as unknown as T;
  }

  if (url === '/circulacion/devoluciones') {
    const { codigoBarras, estadoEntrega } = JSON.parse(options.body as string);
    const isLate = codigoBarras === '9780132350884-C1'; // Clean Code es el atrasado en mock

    // Encontrar el ejemplar y cambiar su estado en mock data
    let targetBookId = 0;
    for (const bookId of Object.keys(mockEjemplares)) {
      const list = mockEjemplares[parseInt(bookId)];
      const e = list.find(x => x.codigoBarras === codigoBarras);
      if (e) {
        e.estado = estadoEntrega === 'Bueno' ? 'Disponible' : (estadoEntrega === 'Dañado' ? 'Mantenimiento' : 'Pérdida');
        targetBookId = e.libroId;
        break;
      }
    }
    
    if (targetBookId > 0 && estadoEntrega === 'Bueno') {
      const book = mockBooks.find(b => b.id === targetBookId);
      if (book) {
        book.copiasDisponibles = (book.copiasDisponibles || 0) + 1;
      }
    }

    // Encontrar y cerrar el préstamo correspondiente en mockLoans
    const loan = mockLoans.find(l => l.ejemplarCodigoBarras === codigoBarras && l.estado === 'Activo');
    if (loan) {
      loan.estado = 'Devuelto';
    }

    return {
      prestamoId: isLate ? 102 : (loan ? loan.id : 101),
      fechaDevolucionEfectiva: new Date().toISOString(),
      diasRetraso: isLate ? 5 : 0,
      penalizacionAplicada: isLate,
      sancion: isLate ? {
        id: 301,
        usuarioId: 10,
        prestamoId: 102,
        fechaInicio: new Date().toISOString(),
        fechaFin: new Date(Date.now() + 10 * 24 * 60 * 60 * 1000).toISOString(),
        diasSancion: 10,
        estado: "Activa"
      } : null,
      nuevoEstadoEjemplar: estadoEntrega === 'Bueno' ? 'Disponible' : (estadoEntrega === 'Dañado' ? 'Mantenimiento' : 'Pérdida')
    } as unknown as T;
  }

  if (url === '/libros') {
    const body = JSON.parse(options.body as string);

    // Validar formato del ISBN (RN-1.3)
    if (body.isbn.length < 10 || isNaN(Number(body.isbn))) {
      throw new ApiError(400, {
        code: "ERR_INVALID_ISBN",
        title: "Error de Ingesta",
        detail: "El formato del ISBN ingresado no es válido o contiene caracteres alfabéticos (RN-1.3)."
      });
    }

    const newBook: BookDto = {
      id: Math.floor(Math.random() * 1000) + 100,
      titulo: body.titulo,
      isbn: body.isbn,
      autor: { id: body.autorId || 1, nombre: body.autorNombre || "Robert C. Martin", nacionalidad: "Estadounidense" },
      categoria: { id: body.categoriaId || 1, nombre: body.categoriaNombre || "Ingeniería de Software", descripcion: "Desarrollo de software" },
      editorial: { id: body.editorialId || 1, nombre: body.editorialNombre || "Editorial" },
      fechaPublicacion: body.fechaPublicacion || new Date().toISOString().split('T')[0],
      idioma: "Español",
      pais: "Perú",
      tipoMaterial: body.tipoMaterial || "LibroFisico",
      totalCopias: 0,
      copiasDisponibles: 0
    };
    mockBooks.push(newBook);
    return newBook as unknown as T;
  }

  if (url.startsWith('/libros/') && url.endsWith('/ejemplares')) {
    const segments = url.split('/');
    const libroId = parseInt(segments[2]);
    const { codigoBarras, ubicacionFisica, observaciones } = JSON.parse(options.body as string);

    const newEjemplar: EjemplarDto = {
      id: Math.floor(Math.random() * 10000),
      libroId,
      codigoBarras,
      estado: "Disponible",
      ubicacionFisica,
      observaciones
    };

    if (!mockEjemplares[libroId]) {
      mockEjemplares[libroId] = [];
    }
    mockEjemplares[libroId].push(newEjemplar);

    // Actualizar conteos de copias en el libro
    const book = mockBooks.find(b => b.id === libroId);
    if (book) {
      book.totalCopias = (book.totalCopias || 0) + 1;
      book.copiasDisponibles = (book.copiasDisponibles || 0) + 1;
    }

    return newEjemplar as unknown as T;
  }

  if (url === '/reportes/estadisticas') {
    return {
      totalEjemplares: 1240,
      prestamosActivos: 142,
      tasaMorosidad: 4.2,
      usuariosBloqueados: 8
    } as unknown as T;
  }

  if (url === '/reportes/mas-solicitados') {
    return [
      { rank: 1, libroId: 1, titulo: "Clean Architecture", autorNombre: "Robert C. Martin", totalPrestamos: 48 },
      { rank: 2, libroId: 2, titulo: "Clean Code", autorNombre: "Robert C. Martin", totalPrestamos: 36 },
      { rank: 3, libroId: 3, titulo: "Design Patterns", autorNombre: "Erich Gamma", totalPrestamos: 24 }
    ] as unknown as T;
  }

  if (url === '/reportes/mantenimiento-perdidos') {
    return [
      { ejemplarId: 6, codigoBarras: "9780132350884-C3", libroTitulo: "Clean Code", estado: "Mantenimiento", observaciones: "Portada rota", ultimaFechaActualizacion: new Date().toISOString() }
    ] as unknown as T;
  }

  if (url === '/reportes/morosos') {
    return [
      { usuarioId: 10, dni: "71234567", nombreCompleto: "Juan Pérez", email: "alumno@uni.edu.pe", prestamosVencidosCount: 1, sancionesActivasCount: 1, estadoUsuario: "BloqueoTemporal" }
    ] as unknown as T;
  }

  if (url === '/configuracion') {
    return {
      maxPrestamosAlumno: 3,
      maxPrestamosDocente: 5,
      maxPrestamosAdministrativo: 3,
      diasPrestamoDefecto: 7,
      diasSuspensionPorDiaRetraso: 2,
      horasGraciaReserva: 48
    } as unknown as T;
  }

  return {} as T;
}
