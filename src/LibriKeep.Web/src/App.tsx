import React, { useState, useEffect } from 'react';
import { 
  BookOpen, Lock, ShieldAlert, RefreshCw, PlusCircle, 
  BarChart3, AlertOctagon, User, ArrowRightLeft, 
  LogOut, CheckCircle2, Search, Info, ArrowLeft
} from 'lucide-react';
import { apiClient, getUseMockData } from './services/apiClient';
import { BookDto, UsuarioDto, PrestamoDto, SancionDto, DevolucionResponseDto, EjemplarDto } from './types/api';
import { jsPDF } from 'jspdf';
import autoTable from 'jspdf-autotable';
// @ts-ignore
import logoImage from './image_31.png';

export default function App() {
  // --- Estados Globales ---
  const [currentUser, setCurrentUser] = useState<UsuarioDto | null>(() => {
    const saved = localStorage.getItem('librikeep_user');
    return saved ? JSON.parse(saved) : null;
  });
  const [activeView, setActiveView] = useState<string>('opac');
  const [globalError, setGlobalError] = useState<{ code: string; title: string; detail: string } | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [isOfflineMode, setIsOfflineMode] = useState<boolean>(getUseMockData());

  // --- Efecto de sincronización de estado fuera de línea ---
  useEffect(() => {
    const interval = setInterval(() => {
      setIsOfflineMode(getUseMockData());
    }, 1000);
    return () => clearInterval(interval);
  }, []);

  const handleLogout = () => {
    localStorage.removeItem('librikeep_token');
    localStorage.removeItem('librikeep_user');
    setCurrentUser(null);
    setActiveView('opac');
    triggerSuccess('Sesión cerrada correctamente');
  };

  // --- Utilidad para mostrar alertas que se disuelven ---
  const triggerSuccess = (msg: string) => {
    setSuccessMessage(msg);
    setTimeout(() => setSuccessMessage(null), 3000);
  };

  useEffect(() => {
    if (globalError) {
      const timer = setTimeout(() => {
        setGlobalError(null);
      }, 3000);
      return () => clearTimeout(timer);
    }
  }, [globalError]);

  return (
    <div className="min-h-screen bg-background text-text flex flex-col font-sans relative selection:bg-primary selection:text-white">
      
      {/* --- BANNER DE SUCESO GLOBAL --- */}
      {successMessage && (
        <div className="fixed bottom-6 right-6 z-50 bg-primary border border-primary/20 text-text px-5 py-3.5 rounded-xl shadow-2xl backdrop-blur flex items-center gap-3 animate-in slide-in-from-bottom duration-300 min-w-[280px]">
          <CheckCircle2 className="w-5 h-5 text-emerald-400 shrink-0" />
          <span className="text-sm font-semibold text-text">{successMessage}</span>
        </div>
      )}

      {/* --- PANTALLA H: MODAL GLOBAL DE EXCEPCIONES Y ERRORES --- */}
      {globalError && (
        <div className="fixed inset-0 bg-background/80 backdrop-blur-md flex items-center justify-center z-50 p-4">
          <div className="bg-card border border-secondary/80 p-6 rounded-2xl shadow-2xl max-w-md w-full animate-in fade-in zoom-in-95 duration-200">
            <div className="flex items-center gap-3 text-rose-400 border-b border-secondary pb-4 mb-4">
              <AlertOctagon className="w-6 h-6 shrink-0" />
              <h3 className="text-lg font-bold uppercase tracking-wide">Infracción de Regla de Negocio</h3>
            </div>
            
            <div className="space-y-4">
              <div className="p-3.5 bg-background/60 rounded-xl border border-secondary">
                <span className="text-xs text-text block uppercase font-bold tracking-wider">Código de Infracción</span>
                <code className="text-sm font-mono text-rose-400 font-semibold">{globalError.code}</code>
              </div>

              <div>
                <span className="text-xs text-text block uppercase font-bold tracking-wider mb-1">Descripción Técnica</span>
                <p className="text-sm text-text leading-relaxed">{globalError.detail}</p>
              </div>
            </div>

            <button 
              onClick={() => setGlobalError(null)}
              className="mt-6 w-full py-2.5 bg-card hover:bg-border/20 active:bg-border/30 text-text hover:text-primary rounded-xl text-sm font-medium transition duration-150 border border-border"
            >
              Entendido, Cerrar
            </button>
          </div>
        </div>
      )}

      {/* --- CABECERA PRINCIPAL (BARRA DE NAVEGACIÓN) --- */}
      <header className="sticky top-0 z-40 bg-background/60 backdrop-blur-md border-b border-secondary/80 px-6 py-4 flex flex-col md:flex-row justify-between items-center gap-4">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 bg-gradient-to-tr from-primary to-secondary rounded-xl shadow-lg shadow-primary/10 overflow-hidden flex items-center justify-center p-0.5">
            <img 
              src={logoImage} 
              alt="LibriKeep Logo" 
              className="w-full h-full object-cover rounded-lg"
            />
          </div>
          <div>
            <h1 className="text-xl font-extrabold tracking-tight bg-gradient-to-r from-white via-slate-200 to-primary bg-clip-text text-transparent">LibriKeep <span className="text-xs px-2 py-0.5 rounded bg-primary/10 text-primary border border-primary/20 font-bold ml-1">PRO</span></h1>
            <p className="text-xs text-textOnBg/70">Spec-Driven Library Architecture</p>
          </div>
        </div>

        {/* --- PANEL DE ACCIONES E IDENTIDAD --- */}
        <div className="flex flex-wrap items-center justify-center gap-3">
          {/* Indicador de Simulación Mock */}
          {isOfflineMode && (
            <span className="px-2.5 py-1 text-[11px] font-bold rounded-lg bg-amber-500/10 text-amber-400 border border-amber-500/20 flex items-center gap-1.5 animate-pulse">
              <Info className="w-3.5 h-3.5" /> Modo Simulación Activo
            </span>
          )}

          {/* Menú de Navegación Condicional */}
          <nav className="flex items-center gap-1 bg-background p-1 rounded-xl border border-secondary">
            <button 
              onClick={() => setActiveView('opac')}
              className={`px-3 py-1.5 text-xs font-semibold rounded-lg transition flex items-center gap-1.5 ${activeView === 'opac' ? 'bg-primary text-card shadow' : 'text-textOnBg hover:text-textOnBg/80'}`}
            >
              <BookOpen className="w-3.5 h-3.5" /> Catálogo OPAC
            </button>

            {!currentUser ? (
              <button 
                onClick={() => setActiveView('login')}
                className={`px-3 py-1.5 text-xs font-semibold rounded-lg transition flex items-center gap-1.5 ${activeView === 'login' ? 'bg-primary text-card shadow' : 'text-textOnBg hover:text-textOnBg/80'}`}
              >
                <Lock className="w-3.5 h-3.5" /> Iniciar Sesión
              </button>
            ) : (
              <>
                {currentUser.rol === 'Lector' && (
                  <button 
                    onClick={() => setActiveView('reader')}
                    className={`px-3 py-1.5 text-xs font-semibold rounded-lg transition flex items-center gap-1.5 ${activeView === 'reader' ? 'bg-primary text-card shadow' : 'text-textOnBg hover:text-textOnBg/80'}`}
                  >
                    <User className="w-3.5 h-3.5" /> Mi Perfil
                  </button>
                )}

                {(currentUser.rol === 'Bibliotecario' || currentUser.rol === 'Administrador') && (
                  <>
                    {currentUser.rol === 'Administrador' && (
                      <button 
                        onClick={() => setActiveView('dashboard')}
                        className={`px-3 py-1.5 text-xs font-semibold rounded-lg transition flex items-center gap-1.5 ${activeView === 'dashboard' ? 'bg-primary text-card shadow' : 'text-textOnBg hover:text-textOnBg/80'}`}
                      >
                        <BarChart3 className="w-3.5 h-3.5" /> Reportes
                      </button>
                    )}
                    <button 
                      onClick={() => setActiveView('circulation')}
                      className={`px-3 py-1.5 text-xs font-semibold rounded-lg transition flex items-center gap-1.5 ${activeView === 'circulation' ? 'bg-primary text-card shadow' : 'text-textOnBg hover:text-textOnBg/80'}`}
                    >
                      <ArrowRightLeft className="w-3.5 h-3.5" /> Préstamos
                    </button>
                    <button 
                      onClick={() => setActiveView('returns')}
                      className={`px-3 py-1.5 text-xs font-semibold rounded-lg transition flex items-center gap-1.5 ${activeView === 'returns' ? 'bg-primary text-card shadow' : 'text-textOnBg hover:text-textOnBg/80'}`}
                    >
                      <RefreshCw className="w-3.5 h-3.5" /> Devoluciones
                    </button>
                    <button 
                      onClick={() => setActiveView('cataloging')}
                      className={`px-3 py-1.5 text-xs font-semibold rounded-lg transition flex items-center gap-1.5 ${activeView === 'cataloging' ? 'bg-primary text-card shadow' : 'text-textOnBg hover:text-textOnBg/80'}`}
                    >
                      <PlusCircle className="w-3.5 h-3.5" /> Catalogación
                    </button>
                    <button 
                      onClick={() => setActiveView('readers')}
                      className={`px-3 py-1.5 text-xs font-semibold rounded-lg transition flex items-center gap-1.5 ${activeView === 'readers' ? 'bg-primary text-card shadow' : 'text-textOnBg hover:text-textOnBg/80'}`}
                    >
                      <User className="w-3.5 h-3.5" /> Lectores
                    </button>
                  </>
                )}
              </>
            )}
          </nav>

          {/* Información del Usuario Activo */}
          {currentUser && (
            <div className="flex items-center gap-3 pl-2 border-l border-secondary">
              <div className="text-right hidden sm:block">
                <span className="text-xs block font-bold text-textOnBg">{currentUser.nombreCompleto}</span>
                <span className="text-[10px] block font-semibold text-secondary uppercase tracking-wider">{currentUser.rol}</span>
              </div>
              <button 
                onClick={handleLogout}
                title="Cerrar Sesión"
                className="p-2 bg-background hover:bg-card rounded-lg text-textOnBg hover:text-rose-400 border border-secondary transition"
              >
                <LogOut className="w-4 h-4" />
              </button>
            </div>
          )}
        </div>
      </header>

      {/* --- CONTENIDO PRINCIPAL DINÁMICO --- */}
      <main className="flex-1 p-6 max-w-7xl w-full mx-auto">
        
        {/* --- PANTALLA A: CATÁLOGO PÚBLICO (OPAC) --- */}
        {activeView === 'opac' && <OpacScreen setGlobalError={setGlobalError} />}

        {/* --- PANTALLA B: LOGIN --- */}
        {activeView === 'login' && (
          <LoginScreen 
            setCurrentUser={setCurrentUser} 
            setActiveView={setActiveView} 
            triggerSuccess={triggerSuccess}
            setGlobalError={setGlobalError}
          />
        )}

        {/* --- PANTALLA C: PANEL DE LECTOR (CON ALERTAS) --- */}
        {activeView === 'reader' && currentUser && currentUser.rol === 'Lector' && (
          <ReaderScreen 
            currentUser={currentUser} 
            setGlobalError={setGlobalError}
          />
        )}

        {/* --- PANTALLA D: PANEL DE CIRCULACIÓN (PRESTAMOS) --- */}
        {activeView === 'circulation' && currentUser && (currentUser.rol === 'Bibliotecario' || currentUser.rol === 'Administrador') && (
          <CirculationScreen 
            setGlobalError={setGlobalError}
            triggerSuccess={triggerSuccess}
          />
        )}

        {/* --- PANTALLA E: PROCESAMIENTO DE DEVOLUCIONES Y MULTAS --- */}
        {activeView === 'returns' && currentUser && (currentUser.rol === 'Bibliotecario' || currentUser.rol === 'Administrador') && (
          <ReturnsScreen 
            setGlobalError={setGlobalError}
            triggerSuccess={triggerSuccess}
          />
        )}

        {/* --- PANTALLA F: FORMULARIO DE CATALOGACIÓN --- */}
        {activeView === 'cataloging' && currentUser && (currentUser.rol === 'Bibliotecario' || currentUser.rol === 'Administrador') && (
          <CatalogingScreen 
            setGlobalError={setGlobalError}
            triggerSuccess={triggerSuccess}
          />
        )}

        {/* --- PANTALLA G: DASHBOARD Y KPI --- */}
        {activeView === 'dashboard' && currentUser && (currentUser.rol === 'Administrador' || currentUser.rol === 'Bibliotecario') && (
          <DashboardScreen 
            setGlobalError={setGlobalError}
            triggerSuccess={triggerSuccess}
          />
        )}

        {/* --- PANTALLA H: GESTIÓN DE LECTORES --- */}
        {activeView === 'readers' && currentUser && (currentUser.rol === 'Bibliotecario' || currentUser.rol === 'Administrador') && (
          <ReadersScreen 
            currentUser={currentUser}
            setGlobalError={setGlobalError}
            triggerSuccess={triggerSuccess}
          />
        )}

      </main>

      {/* --- PIE DE PÁGINA --- */}
      <footer className="bg-card/40 border-t border-secondary/80 px-6 py-4 mt-12 text-center text-xs text-textOnBg/80 flex flex-col sm:flex-row justify-between items-center gap-3">
        <span>© {new Date().getFullYear()} LibriKeep Pro. Diseñado bajo estándares de SDD estricto.</span>
        <div className="flex gap-4">
          <a href="/api/openapi.yaml" className="hover:text-primary transition" target="_blank" rel="noreferrer">OpenAPI Spec</a>
          <span className="text-textOnBg/60">|</span>
          <span className="text-textOnBg/60">PostgreSQL + .NET Core 10 + React 19</span>
        </div>
      </footer>
    </div>
  );
}

// ==========================================
// PANTALLA A: Catálogo Público Abierto (OPAC)
// ==========================================
function OpacScreen({ setGlobalError }: { setGlobalError: any }) {
  const [books, setBooks] = useState<BookDto[]>([]);
  const [allBooks, setAllBooks] = useState<BookDto[]>([]);
  const [query, setQuery] = useState('');
  const [selectedBook, setSelectedBook] = useState<BookDto | null>(null);
  const [ejemplares, setEjemplares] = useState<EjemplarDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [loadingDetail, setLoadingDetail] = useState(false);
  const [selectedCategoryModule, setSelectedCategoryModule] = useState<string | null>(null);

  const fetchBooks = async (searchTerm = '') => {
    setLoading(true);
    try {
      const resp = await apiClient.opac.search(searchTerm);
      setBooks(resp.items);
      if (searchTerm === '') {
        setAllBooks(resp.items);
      }
    } catch (e: any) {
      setGlobalError({ code: e.errorResponse?.code || 'ERR_OPAC', title: 'Error de Catálogo', detail: e.message });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchBooks();
  }, []);

  useEffect(() => {
    if (query.trim() === '') {
      fetchBooks('');
    }
  }, [query]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    fetchBooks(query);
  };

  const getRecommendations = (): BookDto[] => {
    if (allBooks.length === 0) return [];
    let matched: BookDto[] = [];

    // 1. Si hay un libro seleccionado, buscar por su autor o categoría
    if (selectedBook) {
      matched = allBooks.filter(b => 
        b.id !== selectedBook.id && 
        (b.autor.id === selectedBook.autor.id || b.categoria?.id === selectedBook.categoria?.id)
      );
    }

    // 2. Si no hay coincidencias o no hay libro seleccionado, buscar por los términos de búsqueda (query)
    if (matched.length === 0 && query.trim().length > 0) {
      const terms = query.toLowerCase().split(/\s+/);
      matched = allBooks.filter(b => 
        terms.some(t => 
          b.titulo.toLowerCase().includes(t) || 
          b.autor.nombre.toLowerCase().includes(t) ||
          (b.categoria?.nombre && b.categoria.nombre.toLowerCase().includes(t))
        )
      );
    }

    // 3. Si sigue sin haber coincidencias, elegir 2 libros aleatorios de allBooks
    if (matched.length === 0) {
      const shuffled = [...allBooks].sort(() => 0.5 - Math.random());
      matched = shuffled.slice(0, 2);
    }

    return matched.slice(0, 3);
  };

  const handleViewBook = async (book: BookDto) => {
    setSelectedBook(book);
    setLoadingDetail(true);
    try {
      const detail = await apiClient.opac.getById(book.id);
      setEjemplares(detail.ejemplares);
    } catch (e: any) {
      setGlobalError({ code: e.errorResponse?.code || 'ERR_DETAIL', title: 'Error de Carga', detail: e.message });
    } finally {
      setLoadingDetail(false);
    }
  };

  const categoryExemplarCounts = allBooks.reduce((acc: { [key: string]: number }, book) => {
    const catName = book.categoria?.nombre || 'General';
    acc[catName] = (acc[catName] || 0) + (book.copiasDisponibles || 0);
    return acc;
  }, {});

  const modules = Object.keys(categoryExemplarCounts).map(name => ({
    name,
    count: categoryExemplarCounts[name]
  }));

  return (
    <div className="space-y-6">
      <div className="text-center max-w-2xl mx-auto space-y-3">
        <h2 className="text-2xl font-black md:text-3xl tracking-tight text-textOnBg">Catálogo de Biblioteca OPAC</h2>
        <p className="text-textOnBg/80 text-sm">Explora las obras de ingeniería y literatura general de LibriKeep en tiempo real con disponibilidad de existencias físicas.</p>
      </div>

      {/* Barra de Búsqueda */}
      <form onSubmit={handleSearch} className="max-w-2xl mx-auto flex gap-2">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-3.5 w-4 h-4 text-placeholder" />
          <input 
            type="text" 
            placeholder="Buscar por título, autor o ISBN..." 
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            className="w-full pl-10 pr-4 py-3 bg-card border border-border rounded-xl text-text placeholder-placeholder focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary transition text-sm font-medium"
          />
        </div>
        <button 
          type="submit"
          className="px-5 py-3 bg-primary hover:bg-primary/90 active:bg-primary/80 text-card font-semibold rounded-xl text-sm transition"
        >
          Buscar
        </button>
      </form>

      {/* Grid de Contenidos */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        
        {/* Lista de Libros / Módulos */}
        <div className="lg:col-span-2 space-y-6">
          
          {loading ? (
            <div className="text-center py-12 text-text text-sm">Cargando catálogo...</div>
          ) : query.trim() !== '' ? (
            // ==========================================
            // VISTA 1: BÚSQUEDA GLOBAL (LISTA PLANA)
            // ==========================================
            <div className="space-y-4">
              <h3 className="text-sm font-bold uppercase tracking-wider text-text">Resultados de Búsqueda Global ({books.length})</h3>
              {books.length === 0 ? (
                <div className="space-y-6">
                  <div className="text-center py-8 bg-card border border-border rounded-2xl text-text text-sm">No se encontraron resultados de búsqueda.</div>
                  
                  <div className="space-y-4 text-left">
                    <h3 className="text-sm font-bold uppercase tracking-wider text-primary">Libros que te podrían interesar</h3>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      {getRecommendations().map(b => (
                        <div 
                          key={b.id} 
                          onClick={() => handleViewBook(b)}
                          className="p-5 rounded-2xl border bg-card border-border hover:border-primary/50 hover:bg-card/90 transition cursor-pointer text-left"
                        >
                          <span className="text-[10px] uppercase font-bold tracking-wider text-primary bg-primary/10 px-2 py-0.5 rounded border border-primary/20">{b.tipoMaterial}</span>
                          <h4 className="font-bold text-text text-base mt-2">{b.titulo}</h4>
                          <p className="text-text text-xs mt-1">{b.autor.nombre}</p>
                          
                          <div className="flex justify-between items-center mt-5 text-[11px] text-text">
                            <span>ISBN: {b.isbn}</span>
                            <span className={`px-2 py-0.5 rounded font-bold ${b.copiasDisponibles > 0 ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20' : 'bg-amber-500/10 text-amber-400 border border-amber-500/20'}`}>
                              {b.copiasDisponibles > 0 ? 'Disponible' : 'Prestado'}
                            </span>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {books.map(b => (
                    <div 
                      key={b.id} 
                      onClick={() => handleViewBook(b)}
                      className={`p-5 rounded-2xl border transition cursor-pointer text-left ${selectedBook?.id === b.id ? 'bg-card border-primary shadow-lg ring-1 ring-primary' : 'bg-card/90 border-border hover:border-primary/50 hover:bg-card/70'}`}
                    >
                      <span className="text-[10px] uppercase font-bold tracking-wider text-primary bg-primary/10 px-2 py-0.5 rounded border border-primary/20">{b.tipoMaterial}</span>
                      <h4 className="font-bold text-text text-base mt-2">{b.titulo}</h4>
                      <p className="text-text text-xs mt-1">{b.autor.nombre}</p>
                      
                      <div className="flex justify-between items-center mt-5 text-[11px] text-text">
                        <span>ISBN: {b.isbn}</span>
                        <span className={`px-2 py-0.5 rounded font-bold ${b.copiasDisponibles > 0 ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20' : 'bg-amber-500/10 text-amber-400 border border-amber-500/20'}`}>
                          {b.copiasDisponibles > 0 ? 'Disponible' : 'Prestado'}
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          ) : selectedCategoryModule === null ? (
            // ==========================================
            // VISTA 2: CUADRÍCULA DE MÓDULOS (INICIAL)
            // ==========================================
            <div className="space-y-6 text-left">
              <div>
                <h3 className="text-sm font-bold uppercase tracking-wider text-textOnBg">Módulos Temáticos</h3>
                <p className="text-xs text-textOnBg/80 mt-1">Selecciona una categoría temática para desplegar los recursos físicos catalogados en la biblioteca.</p>
              </div>
              
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {modules.map(m => {
                  let IconComponent = BookOpen;
                  if (m.name.toLowerCase().includes('software') || m.name.toLowerCase().includes('program')) {
                    IconComponent = BookOpen;
                  } else if (m.name.toLowerCase().includes('seguridad') || m.name.toLowerCase().includes('ciber')) {
                    IconComponent = Lock;
                  } else if (m.name.toLowerCase().includes('algoritmo') || m.name.toLowerCase().includes('datos')) {
                    IconComponent = ShieldAlert;
                  }

                  return (
                    <div
                      key={m.name}
                      onClick={() => setSelectedCategoryModule(m.name)}
                      className="p-6 bg-card border border-border hover:border-primary/50 hover:bg-card/80 rounded-2xl shadow-xl transition duration-200 cursor-pointer flex items-start gap-4 group"
                    >
                      <div className="p-3 bg-primary/10 rounded-xl text-primary group-hover:bg-primary group-hover:text-card transition shrink-0">
                        <IconComponent className="w-6 h-6" />
                      </div>
                      <div className="space-y-1">
                        <h4 className="font-extrabold text-text text-base group-hover:text-primary transition">{m.name}</h4>
                        <p className="text-text text-[10px] font-semibold uppercase tracking-wide">Módulo Temático</p>
                        <span className="text-[11px] text-primary font-bold block mt-2">{categoryExemplarCounts[m.name] || 0} ejemplares registrados</span>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          ) : (
            // ==========================================
            // VISTA 3: LIBROS DEL MÓDULO SELECCIONADO
            // ==========================================
            <div className="space-y-6 text-left">
              <div className="flex items-center gap-4 py-1">
                <button
                  onClick={() => setSelectedCategoryModule(null)}
                  title="Volver a Módulos"
                  className="p-2.5 bg-card hover:bg-card/90 border border-border text-text hover:text-primary rounded-xl transition flex items-center justify-center shrink-0 shadow-lg"
                >
                  <ArrowLeft className="w-4 h-4" />
                </button>
                <h3 className="text-lg font-black tracking-tight text-textOnBg uppercase border-l border-secondary pl-4">
                  {selectedCategoryModule}
                </h3>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 animate-in fade-in duration-200">
                {allBooks.filter(b => (b.categoria?.nombre || 'General') === selectedCategoryModule).map(b => (
                  <div 
                    key={b.id} 
                    onClick={() => handleViewBook(b)}
                    className={`p-5 rounded-2xl border transition cursor-pointer text-left ${selectedBook?.id === b.id ? 'bg-card border-primary shadow-lg ring-1 ring-primary' : 'bg-card/90 border-border hover:border-primary/50 hover:bg-card/70'}`}
                  >
                    <span className="text-[10px] uppercase font-bold tracking-wider text-primary bg-primary/10 px-2 py-0.5 rounded border border-primary/20">{b.tipoMaterial}</span>
                    <h4 className="font-bold text-text text-base mt-2">{b.titulo}</h4>
                    <p className="text-text text-xs mt-1">{b.autor.nombre}</p>
                    
                    <div className="flex justify-between items-center mt-5 text-[11px] text-text">
                      <span>ISBN: {b.isbn}</span>
                      <span className={`px-2 py-0.5 rounded font-bold ${b.copiasDisponibles > 0 ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20' : 'bg-amber-500/10 text-amber-400 border border-amber-500/20'}`}>
                        {b.copiasDisponibles > 0 ? 'Disponible' : 'Prestado'}
                      </span>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Detalle y Ejemplares del Libro Seleccionado */}
        <div className="bg-card border border-border p-6 rounded-2xl space-y-6 self-start">
          <h3 className="text-sm font-bold uppercase tracking-wider text-text">Detalle de Existencias Físicas</h3>
          
          {selectedBook ? (
            <div className="space-y-6 text-left">
              <div>
                <h4 className="text-lg font-black text-text">{selectedBook.titulo}</h4>
                <p className="text-sm text-text">{selectedBook.autor.nombre} ({selectedBook.autor.nacionalidad})</p>
              </div>

              <div className="grid grid-cols-2 gap-3 text-xs p-3.5 bg-border/30 rounded-xl border border-border">
                <div>
                  <span className="text-text block uppercase font-bold text-[10px] tracking-wider">Editorial</span>
                  <span className="font-medium text-text">{selectedBook.editorial.nombre}</span>
                </div>
                <div>
                  <span className="text-text block uppercase font-bold text-[10px] tracking-wider">Fecha Pub.</span>
                  <span className="font-medium text-text">{new Date(selectedBook.fechaPublicacion).toLocaleDateString()}</span>
                </div>
                <div>
                  <span className="text-text block uppercase font-bold text-[10px] tracking-wider">Idioma</span>
                  <span className="font-medium text-text">{selectedBook.idioma || 'Español'}</span>
                </div>
                <div>
                  <span className="text-text block uppercase font-bold text-[10px] tracking-wider">País</span>
                  <span className="font-medium text-text">{selectedBook.pais || 'No especificado'}</span>
                </div>
              </div>

              <div className="space-y-3">
                <span className="text-xs font-bold text-text uppercase tracking-wider block">Copias físicas en Biblioteca</span>
                
                {loadingDetail ? (
                  <div className="text-center py-4 text-text text-xs">Cargando copias...</div>
                ) : ejemplares.length === 0 ? (
                  <div className="text-center py-4 text-text text-xs">No hay copias físicas ingresadas para este libro.</div>
                ) : (
                  <div className="space-y-2">
                    {ejemplares.map((e, i) => (
                      <div key={e.id} className={`p-3 ${i % 2 === 0 ? 'bg-tableEven' : 'bg-tableOdd'} border-border rounded-xl flex items-center justify-between text-xs`}>
                        <div>
                          <span className="font-mono text-text block">{e.codigoBarras}</span>
                          <span className="text-[10px] text-text block">Estantería: {e.ubicacionFisica || 'Sin asignar'}</span>
                        </div>
                        <span className={`px-2 py-0.5 rounded font-bold ${
                          e.estado === 'Disponible' ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20' : 
                          e.estado === 'Prestado' ? 'bg-amber-500/10 text-amber-400 border border-amber-500/20' : 
                          'bg-rose-500/10 text-rose-400 border border-rose-500/20'
                        }`}>
                          {e.estado}
                        </span>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          ) : (
            <div className="text-center py-12 text-text text-xs leading-relaxed">Selecciona un libro del catálogo para verificar la ubicación de los ejemplares físicos e incidencias de mantenimiento.</div>
          )}
        </div>

      </div>
    </div>
  );
}

// ==========================================
// PANTALLA B: Autenticación y Acceso Seguro (Login)
// ==========================================
function LoginScreen({ setCurrentUser, setActiveView, triggerSuccess, setGlobalError }: { setCurrentUser: any; setActiveView: any; triggerSuccess: any; setGlobalError: any }) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const resp = await apiClient.auth.login(email, password);
      localStorage.setItem('librikeep_token', resp.token);
      localStorage.setItem('librikeep_user', JSON.stringify(resp.usuario));
      setCurrentUser(resp.usuario);
      triggerSuccess(`¡Bienvenido al sistema, ${resp.usuario.nombreCompleto}!`);
      
      if (resp.usuario.rol === 'Lector') setActiveView('reader');
      else if (resp.usuario.rol === 'Bibliotecario') setActiveView('circulation');
      else setActiveView('dashboard');
    } catch (err: any) {
      setGlobalError({ code: err.errorResponse?.code || 'ERR_AUTH_FAILED', title: 'Fallo de Autenticación', detail: err.message });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-md w-full mx-auto mt-12 bg-card border border-border rounded-2xl shadow-xl overflow-hidden text-left animate-in fade-in duration-300">
      <div className="p-8 space-y-6">
        <div className="text-center space-y-2">
          <div className="p-3 bg-primary/10 text-primary rounded-xl inline-block border border-primary/20">
            <Lock className="w-6 h-6" />
          </div>
          <h2 className="text-2xl font-black text-text">Acceso Seguro</h2>
          <p className="text-text text-xs">Introduce tus credenciales para autorizar el consumo de la API.</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="text-xs font-bold text-text block uppercase tracking-wider mb-1">Correo Electrónico</label>
            <input 
              type="email" 
              required
              placeholder="alumno@uni.edu.pe"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full px-4 py-2.5 bg-card border border-border rounded-xl text-text placeholder-placeholder focus:outline-none focus:border-primary transition text-sm"
            />
          </div>

          <div>
            <label className="text-xs font-bold text-text block uppercase tracking-wider mb-1">Contraseña</label>
            <input 
              type="password" 
              required
              placeholder="*************"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full px-4 py-2.5 bg-card border border-border rounded-xl text-text placeholder-placeholder focus:outline-none focus:border-primary transition text-sm"
            />
          </div>

          <button 
            type="submit"
            disabled={loading}
            className="w-full py-3 bg-primary hover:bg-primary/90 active:bg-primary/80 text-card font-semibold rounded-xl text-sm transition shadow-primary/20 disabled:opacity-50"
          >
            {loading ? 'Validando token JWT...' : 'Iniciar Sesión'}
          </button>
        </form>
      </div>
    </div>
  );
}

// ==========================================
// PANTALLA C: Panel del Lector y Alertas de Suspensión
// ==========================================
function ReaderScreen({ currentUser, setGlobalError }: { currentUser: UsuarioDto; setGlobalError: any }) {
  const [profile, setProfile] = useState<any>(null);
  const [loans, setLoans] = useState<PrestamoDto[]>([]);
  const [sanciones, setSanciones] = useState<SancionDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const loadProfileData = async () => {
      setLoading(true);
      try {
        const prof = await apiClient.users.getProfile(currentUser.id);
        setProfile(prof);

        const activeLoans = await apiClient.circulation.getLoans(currentUser.id);
        setLoans(activeLoans.items);

        const activeSanciones = await apiClient.users.getSanciones(currentUser.id);
        setSanciones(activeSanciones);
      } catch (e: any) {
        setGlobalError({ code: e.errorResponse?.code || 'ERR_READER_PROFILE', title: 'Error de Perfil', detail: e.message });
      } finally {
        setLoading(false);
      }
    };

    loadProfileData();
  }, [currentUser]);

  if (loading) return <div className="text-center py-12 text-text text-sm">Cargando perfil de usuario...</div>;

  const tieneMora = profile?.tienePrestamoVencido || profile?.tieneSancionActiva || profile?.tieneMultaImpagada;

  return (
    <div className="space-y-6 text-left animate-in fade-in duration-300">
      
      {/* Banner Reactivo de Alerta de Suspensión (RN-02) */}
      {tieneMora && (
        <div className="p-5 bg-rose-950/40 border border-rose-500/30 rounded-2xl text-rose-300 flex items-start gap-4 shadow-xl">
          <ShieldAlert className="w-6 h-6 text-rose-400 shrink-0 mt-0.5 animate-bounce" />
          <div className="space-y-1">
            <h4 className="font-bold text-rose-200">CUENTA SUSPENDIDA POR INCUMPLIMIENTO (RN-02)</h4>
            <p className="text-xs text-rose-300/90 leading-relaxed">
              Tu cuenta de circulación ha sido bloqueada. No podrás realizar nuevos préstamos físicos ni encolar reservas en el OPAC hasta regularizar tus préstamos retrasados o expirar las multas.
            </p>
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        
        {/* Datos de Membresía */}
        <div className="bg-card border border-border p-6 rounded-2xl space-y-6 self-start">
          <h3 className="text-sm font-bold uppercase tracking-wider text-text">Credencial del Lector</h3>
          <div className="flex items-center gap-4">
            <div className="w-12 h-12 bg-primary/10 border border-primary/20 text-primary flex items-center justify-center rounded-2xl text-lg font-bold">
              {currentUser.nombreCompleto.charAt(0)}
            </div>
            <div>
              <h4 className="font-extrabold text-text text-base leading-tight">{currentUser.nombreCompleto}</h4>
              <span className="text-[10px] font-bold text-text uppercase tracking-wider">{currentUser.tipoMiembro}</span>
            </div>
          </div>

          <div className="space-y-3.5 text-xs border-t border-border pt-5">
            <div className="flex justify-between">
              <span className="text-text">Documento DNI</span>
              <span className="font-semibold text-text">{currentUser.dni}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-text">Email Académico</span>
              <span className="font-semibold text-text">{currentUser.email}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-text">Estado de Ficha</span>
              <span className={`px-2 py-0.5 rounded font-bold text-[10px] ${currentUser.estado === 'Activo' ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20' : 'bg-rose-500/10 text-rose-400 border border-rose-500/20'}`}>
                {currentUser.estado}
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-text">Préstamos Activos</span>
              <span className="font-semibold text-text">{profile?.prestamosActivosCount} de {profile?.prestamosPermitidosCount} permitidos</span>
            </div>
          </div>
        </div>

        {/* Historial de Préstamos Activos y Multas */}
        <div className="lg:col-span-2 space-y-6">
          <div className="bg-card border border-border p-6 rounded-2xl">
            <h3 className="text-sm font-bold uppercase tracking-wider text-text mb-4">Tus Préstamos Activos</h3>
            
            {loans.length === 0 ? (
              <div className="text-center py-8 text-text text-sm">No tienes préstamos registrados actualmente.</div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-xs text-left border-collapse">
                  <thead>
                    <tr className="border-b border-border text-text uppercase font-bold text-[10px] tracking-wider">
                      <th className="pb-3">Libro / Ejemplar</th>
                      <th className="pb-3">Fecha Salida</th>
                      <th className="pb-3">Vence El</th>
                      <th className="pb-3 text-right">Estado</th>
                    </tr>
                  </thead>
                  <tbody>
                    {loans.map(l => {
                      const isVencido = new Date(l.fechaMaxDevolucion) < new Date();
                      return (
                        <tr key={l.id} className={`border-b border-border align-middle ${isVencido ? 'bg-rose-500/10 text-rose-800 font-semibold' : 'text-text'}`}>
                          <td className="py-4 font-semibold">
                            <span className="block text-text text-sm">{l.libroTitulo}</span>
                            <span className="block font-mono text-[10px] text-text mt-0.5">{l.ejemplarCodigoBarras}</span>
                          </td>
                          <td className="py-4">{new Date(l.fechaSalida).toLocaleDateString()}</td>
                          <td className="py-4">{new Date(l.fechaMaxDevolucion).toLocaleDateString()}</td>
                          <td className="py-4 text-right">
                            <span className={`px-2 py-0.5 rounded font-bold text-[10px] ${
                              isVencido ? 'bg-rose-500/10 text-rose-700 border border-rose-500/20 animate-pulse' : 'bg-primary/10 text-primary border border-primary/20'
                            }`}>
                              {isVencido ? 'Vencido' : l.estado}
                            </span>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          {/* Listado de Sanciones Históricas */}
          {sanciones.length > 0 && (
            <div className="bg-card border border-border p-6 rounded-2xl">
              <h3 className="text-sm font-bold uppercase tracking-wider text-text mb-4">Sanciones de Circulación</h3>
              <div className="space-y-3">
                {sanciones.map(s => (
                  <div key={s.id} className="p-4 bg-border/30 border border-border rounded-xl flex items-center justify-between text-xs">
                    <div>
                      <span className="font-bold text-text block">Sanción de Suspensión por {s.diasSancion} días</span>
                      <span className="text-text text-[10px] block mt-0.5">Vigencia: {new Date(s.fechaInicio).toLocaleDateString()} al {new Date(s.fechaFin).toLocaleDateString()}</span>
                    </div>
                    <span className={`px-2.5 py-1 rounded-lg font-bold text-[10px] uppercase tracking-wider ${s.estado === 'Activa' ? 'bg-rose-500/10 text-rose-700 border border-rose-500/20' : 'bg-border/40 text-text border border-border'}`}>
                      {s.estado}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

      </div>
    </div>
  );
}

// ==========================================
// PANTALLA D: Panel de Circulación (Préstamos)
// ==========================================
function CirculationScreen({ setGlobalError, triggerSuccess }: { setGlobalError: any; triggerSuccess: any }) {
  const getDefaultDueDate = () => {
    const d = new Date();
    d.setDate(d.getDate() + 7);
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  };

  const [dni, setDni] = useState('');
  const [barcode, setBarcode] = useState('');
  const [dueDate, setDueDate] = useState(getDefaultDueDate());
  const [loans, setLoans] = useState<PrestamoDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [loadingList, setLoadingList] = useState(false);

  // Estados para autocompletado
  const [usersList, setUsersList] = useState<UsuarioDto[]>([]);
  const [availableCopies, setAvailableCopies] = useState<{ id: number; codigoBarras: string; tituloLibro: string }[]>([]);
  const [filteredUsers, setFilteredUsers] = useState<UsuarioDto[]>([]);
  const [showUsersDropdown, setShowUsersDropdown] = useState(false);
  const [filteredCopies, setFilteredCopies] = useState<{ id: number; codigoBarras: string; tituloLibro: string }[]>([]);
  const [showCopiesDropdown, setShowCopiesDropdown] = useState(false);

  const fetchRecentLoans = async () => {
    setLoadingList(true);
    try {
      const resp = await apiClient.circulation.getLoans();
      setLoans(resp.items);
    } catch (e: any) {
      // Ignorar fallos de carga
    } finally {
      setLoadingList(false);
    }
  };

  const loadSuggestions = async () => {
    try {
      const uList = await apiClient.users.list();
      setUsersList(uList);

      const bookList = await apiClient.opac.search();
      const copiesArray: { id: number; codigoBarras: string; tituloLibro: string }[] = [];
      
      for (const b of bookList.items) {
        const detail = await apiClient.opac.getById(b.id);
        if (detail.ejemplares) {
          const disp = detail.ejemplares.filter(e => e.estado === 'Disponible');
          disp.forEach(e => {
            copiesArray.push({
              id: e.id,
              codigoBarras: e.codigoBarras,
              tituloLibro: b.titulo
            });
          });
        }
      }
      setAvailableCopies(copiesArray);
    } catch (err) {
      console.error("No se pudieron cargar sugerencias para autocompletado", err);
    }
  };

  useEffect(() => {
    fetchRecentLoans();
    loadSuggestions();
  }, []);

  const handleDniChange = (val: string) => {
    setDni(val);
    if (val.trim() === '') {
      setFilteredUsers([]);
      setShowUsersDropdown(false);
    } else {
      const match = usersList.filter(u => 
        u.dni.includes(val) || 
        u.nombreCompleto.toLowerCase().includes(val.toLowerCase())
      );
      setFilteredUsers(match);
      setShowUsersDropdown(true);
    }
  };

  const handleBarcodeChange = (val: string) => {
    setBarcode(val);
    if (val.trim() === '') {
      setFilteredCopies([]);
      setShowCopiesDropdown(false);
    } else {
      const match = availableCopies.filter(c => 
        c.codigoBarras.toLowerCase().includes(val.toLowerCase()) || 
        c.tituloLibro.toLowerCase().includes(val.toLowerCase())
      );
      setFilteredCopies(match);
      setShowCopiesDropdown(true);
    }
  };

  const handleLoan = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      // 1. Buscar usuario por DNI
      const userList = await apiClient.users.list(dni);
      const user = userList.find(u => u.dni === dni);
      const targetUserId = user ? user.id : (isNaN(parseInt(dni)) ? 99 : parseInt(dni));
      
      // 2. Buscar ejemplar por código de barras de manera dinámica
      let targetEjemplarId = 0;
      
      // Intentar buscar usando el código de barras directamente
      const bookList = await apiClient.opac.search(barcode);
      for (const book of bookList.items) {
        const detail = await apiClient.opac.getById(book.id);
        const match = detail.ejemplares.find(e => e.codigoBarras === barcode);
        if (match) {
          targetEjemplarId = match.id;
          break;
        }
      }

      // Si no se encuentra, intentar buscar dividiendo por el guion
      if (targetEjemplarId === 0) {
        const isbn = barcode.split('-')[0];
        const fallbackBookList = await apiClient.opac.search(isbn);
        for (const book of fallbackBookList.items) {
          const detail = await apiClient.opac.getById(book.id);
          const match = detail.ejemplares.find(e => e.codigoBarras === barcode);
          if (match) {
            targetEjemplarId = match.id;
            break;
          }
        }
      }

      if (targetEjemplarId === 0) {
        throw new Error(`No se encontró ningún ejemplar registrado con el código de barras "${barcode}".`);
      }

      const isoDueDate = dueDate ? new Date(dueDate + 'T12:00:00Z').toISOString() : undefined;
      const loan = await apiClient.circulation.loan(targetUserId, targetEjemplarId, isoDueDate);
      
      triggerSuccess(`Préstamo registrado exitosamente. Vence el ${new Date(loan.fechaMaxDevolucion).toLocaleDateString()}`);
      setDni('');
      setBarcode('');
      setDueDate(getDefaultDueDate());
      fetchRecentLoans();
      loadSuggestions(); // Actualizar las sugerencias con los libros disponibles restantes
    } catch (err: any) {
      setGlobalError({ 
        code: err.errorResponse?.code || 'ERR_LOAN_FAILED', 
        title: 'Rechazo de Préstamo (Regla de Negocio)', 
        detail: err.message 
      });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 text-left animate-in fade-in duration-300">
      
      {/* Formulario de registro */}
      <div className="lg:col-span-2 bg-card border border-border p-6 rounded-2xl self-start space-y-6">
        <div>
          <h2 className="text-xl font-black text-text">Registrar Nuevo Préstamo</h2>
          <p className="text-text text-xs mt-1">Ingresa el DNI del usuario y el código de barras del ejemplar físico. Valida límites de cuotas y bloqueos.</p>
        </div>

        <form onSubmit={handleLoan} className="space-y-4">
          <div className="relative">
            <label className="text-xs font-bold text-text block uppercase tracking-wider mb-1">DNI del Lector</label>
            <input 
              type="text" 
              required
              placeholder="Escribe DNI o nombre del lector..."
              value={dni}
              onChange={(e) => handleDniChange(e.target.value)}
              onFocus={() => { if (dni.trim() !== '') setShowUsersDropdown(true); }}
              onBlur={() => setTimeout(() => setShowUsersDropdown(false), 250)}
              className="w-full px-4 py-2.5 bg-card border border-border rounded-xl text-text placeholder-placeholder focus:outline-none focus:border-primary transition text-sm font-semibold"
            />
            {showUsersDropdown && filteredUsers.length > 0 && (
              <div className="absolute z-50 w-full mt-1 bg-card border border-border rounded-xl max-h-48 overflow-y-auto shadow-2xl">
                {filteredUsers.map(u => (
                  <button
                    key={u.id}
                    type="button"
                    onClick={() => {
                      setDni(u.dni);
                      setShowUsersDropdown(false);
                    }}
                    className="w-full text-left px-4 py-2 hover:bg-primary hover:text-card text-text font-semibold border-b border-border/40 block transition"
                  >
                    <span className="block text-text text-xs">{u.nombreCompleto}</span>
                    <span className="block text-[10px] text-text font-mono mt-0.5">DNI: {u.dni} | {u.tipoMiembro}</span>
                  </button>
                ))}
              </div>
            )}
          </div>

          <div className="relative">
            <label className="text-xs font-bold text-text block uppercase tracking-wider mb-1">Código de Barras del Ejemplar</label>
            <input 
              type="text" 
              required
              placeholder="Escribe código de barras o título de la obra..."
              value={barcode}
              onChange={(e) => handleBarcodeChange(e.target.value)}
              onFocus={() => { if (barcode.trim() !== '') setShowCopiesDropdown(true); }}
              onBlur={() => setTimeout(() => setShowCopiesDropdown(false), 250)}
              className="w-full px-4 py-2.5 bg-card border border-border rounded-xl text-text placeholder-placeholder focus:outline-none focus:border-primary transition text-sm font-semibold"
            />
            {showCopiesDropdown && filteredCopies.length > 0 && (
              <div className="absolute z-50 w-full mt-1 bg-card border border-border rounded-xl max-h-48 overflow-y-auto shadow-2xl">
                {filteredCopies.map(c => (
                  <button
                    key={c.id}
                    type="button"
                    onClick={() => {
                      setBarcode(c.codigoBarras);
                      setShowCopiesDropdown(false);
                    }}
                    className="w-full text-left px-4 py-2 hover:bg-primary hover:text-card text-text font-semibold border-b border-border/40 block transition"
                  >
                    <span className="block text-text text-xs">{c.tituloLibro}</span>
                    <span className="block text-[10px] text-text font-mono mt-0.5">Código: {c.codigoBarras}</span>
                  </button>
                ))}
              </div>
            )}
          </div>

          <div>
            <label className="text-xs font-bold text-text block uppercase tracking-wider mb-1">Fecha Límite de Devolución</label>
            <input 
              type="date" 
              required
              value={dueDate}
              onChange={(e) => setDueDate(e.target.value)}
              className="w-full px-4 py-2.5 bg-card border border-border rounded-xl text-text focus:outline-none focus:border-primary transition text-sm font-semibold"
            />
          </div>

          <div className="flex gap-3 pt-4">
            <button 
              type="submit"
              disabled={loading}
              className="px-5 py-3 bg-[#6A5AE0] hover:bg-[#594ad1] active:bg-[#483bc0] text-white font-semibold rounded-xl text-sm transition shadow-lg shadow-[#6A5AE0]/20 disabled:opacity-50"
            >
              {loading ? 'Procesando en DB...' : 'Confirmar Préstamo'}
            </button>
            <button 
              type="button" 
              onClick={() => { setDni(''); setBarcode(''); }}
              className="px-5 py-3 bg-card hover:bg-border/20 text-text hover:text-primary rounded-xl text-sm font-semibold transition border border-border"
            >
              Cancelar
            </button>
          </div>
        </form>
      </div>

      {/* Monitoreo en Tiempo Real */}
      <div className="space-y-6">
        <div className="bg-card border border-border p-6 rounded-2xl">
          <h3 className="text-sm font-bold uppercase tracking-wider text-text mb-4">Métricas de Circulación</h3>
          <div className="grid grid-cols-2 gap-4">
            <div className="p-4 bg-border/30 rounded-xl border border-border text-center">
              <span className="text-[10px] text-text block uppercase font-bold tracking-wider">Prestados Hoy</span>
              <span className="text-2xl font-black text-primary mt-1 block">24</span>
            </div>
            <div className="p-4 bg-border/30 rounded-xl border border-border text-center">
              <span className="text-[10px] text-text block uppercase font-bold tracking-wider">Morosos</span>
              <span className="text-2xl font-black text-rose-700 mt-1 block">5</span>
            </div>
          </div>
        </div>

        <div className="bg-card border border-border p-6 rounded-2xl space-y-4">
          <h3 className="text-sm font-bold uppercase tracking-wider text-text">Salidas Recientes</h3>
          {loadingList ? (
            <div className="text-text text-xs">Cargando salidas...</div>
          ) : loans.length === 0 ? (
            <div className="text-text text-xs">No hay préstamos activos.</div>
          ) : (
            <div className="space-y-3">
              {loans.map(l => (
                <div key={l.id} className="p-3 bg-border/30 border border-border rounded-xl text-xs flex justify-between items-center">
                  <div>
                    <span className="font-bold text-text block">{l.libroTitulo}</span>
                    <span className="text-text text-[10px] block mt-0.5">Lector: {l.usuarioNombre}</span>
                  </div>
                  <span className={`px-2 py-0.5 rounded font-bold text-[9px] ${
                    l.estado === 'Demorado' ? 'bg-rose-500/10 text-rose-700 border border-rose-500/20 animate-pulse' : 'bg-emerald-500/10 text-emerald-700 border border-emerald-500/20'
                  }`}>
                    {l.estado}
                  </span>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

    </div>
  );
}

// ==========================================
// PANTALLA E: Procesamiento de Devoluciones y Multas
// ==========================================
function ReturnsScreen({ setGlobalError, triggerSuccess }: { setGlobalError: any; triggerSuccess: any }) {
  const [barcode, setBarcode] = useState('');
  const [status, setStatus] = useState('Bueno');
  const [obs, setObs] = useState('');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<DevolucionResponseDto | null>(null);

  // Estados para préstamos activos y autocompletado del lector
  const [activeLoans, setActiveLoans] = useState<PrestamoDto[]>([]);
  const [selectedLoan, setSelectedLoan] = useState<PrestamoDto | null>(null);
  const [loadingLoans, setLoadingLoans] = useState(false);
  const [readerInfo, setReaderInfo] = useState<{ nombre: string; dni: string } | null>(null);

  const fetchActiveLoans = async () => {
    setLoadingLoans(true);
    try {
      const resp = await apiClient.circulation.getLoans(undefined, 'Activo');
      const respDemorados = await apiClient.circulation.getLoans(undefined, 'Demorado');
      const allActive = [...resp.items, ...respDemorados.items];
      // Eliminar duplicados por ID de préstamo
      const uniqueActive = allActive.filter((v, i, a) => a.findIndex(t => t.id === v.id) === i);
      setActiveLoans(uniqueActive);
    } catch (e) {
      console.error("No se pudieron cargar préstamos activos", e);
    } finally {
      setLoadingLoans(false);
    }
  };

  useEffect(() => {
    fetchActiveLoans();
  }, []);

  const handleSelectLoan = async (loanIdStr: string) => {
    if (!loanIdStr) {
      setSelectedLoan(null);
      setBarcode('');
      setReaderInfo(null);
      return;
    }
    const loanId = parseInt(loanIdStr);
    const loan = activeLoans.find(l => l.id === loanId);
    if (loan) {
      setSelectedLoan(loan);
      setBarcode(loan.ejemplarCodigoBarras);
      try {
        const prof = await apiClient.users.getProfile(loan.usuarioId);
        if (prof && prof.usuario) {
          setReaderInfo({
            nombre: prof.usuario.nombreCompleto,
            dni: prof.usuario.dni
          });
        } else {
          setReaderInfo({
            nombre: loan.usuarioNombre,
            dni: 'Obteniendo...'
          });
        }
      } catch {
        setReaderInfo({
          nombre: loan.usuarioNombre,
          dni: 'No disponible'
        });
      }
    }
  };

  const handleReturn = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!barcode) {
      setGlobalError({ code: 'ERR_NO_BARCODE', title: 'Error de Selección', detail: 'Por favor, selecciona un préstamo activo antes de continuar.' });
      return;
    }
    setLoading(true);
    try {
      const res = await apiClient.circulation.return(barcode, status, obs);
      setResult(res);
      triggerSuccess('Devolución procesada y guardada en base de datos.');
      fetchActiveLoans(); // Refrescar la lista de préstamos activos
    } catch (err: any) {
      setGlobalError({ code: err.errorResponse?.code || 'ERR_RETURN', title: 'Error de Retorno', detail: err.message });
    } finally {
      setLoading(false);
    }
  };

  const resetForm = () => {
    setBarcode('');
    setObs('');
    setStatus('Bueno');
    setResult(null);
    setSelectedLoan(null);
    setReaderInfo(null);
    fetchActiveLoans();
  };

  return (
    <div className="max-w-2xl w-full mx-auto bg-card border border-border p-6 rounded-2xl text-left space-y-6 animate-in fade-in duration-300">
      <div>
        <h2 className="text-xl font-black text-text">Procesar Devolución Efectiva</h2>
        <p className="text-text text-xs mt-1">Registra el retorno físico de una obra. El sistema detectará automáticamente demoras calendario y generará sanciones.</p>
      </div>

      {!result ? (
        <form onSubmit={handleReturn} className="space-y-4">
          <div>
            <label className="text-xs font-bold text-text block uppercase tracking-wider mb-1">Seleccionar Préstamo Activo</label>
            {loadingLoans ? (
              <div className="text-xs text-text">Cargando préstamos activos de la biblioteca...</div>
            ) : (
              <select
                required
                value={selectedLoan?.id || ''}
                onChange={(e) => handleSelectLoan(e.target.value)}
                className="w-full px-4 py-2.5 bg-card border border-border rounded-xl text-text focus:outline-none focus:border-primary transition text-sm font-semibold"
              >
                <option value="">-- Elige un préstamo activo --</option>
                {activeLoans.map(l => (
                  <option key={l.id} value={l.id}>
                    {l.libroTitulo} ({l.ejemplarCodigoBarras}) - {l.usuarioNombre}
                  </option>
                ))}
              </select>
            )}
          </div>

          {/* Mostrar detalles del lector verificado */}
          {selectedLoan && (
            <div className="p-4 bg-border/30 border border-border rounded-xl space-y-2 animate-in fade-in slide-in-from-top-1 duration-200">
              <span className="text-[10px] text-primary font-extrabold uppercase tracking-wider block">Identificación del Lector Verificada</span>
              <div className="grid grid-cols-2 gap-4 text-xs">
                <div>
                  <span className="text-text block font-bold">Nombre Completo</span>
                  <span className="text-text font-semibold">{readerInfo?.nombre || selectedLoan.usuarioNombre}</span>
                </div>
                <div>
                  <span className="text-text block font-bold">Número de DNI</span>
                  <span className="text-text font-mono font-semibold">{readerInfo?.dni || 'Obteniendo...'}</span>
                </div>
              </div>
              <div className="pt-2 border-t border-border grid grid-cols-2 gap-4 text-xs">
                <div>
                  <span className="text-text block font-bold">Código de Barras del Libro</span>
                  <span className="text-primary font-mono font-semibold">{barcode}</span>
                </div>
                <div>
                  <span className="text-text block font-bold">Fecha de Vencimiento</span>
                  <span className="text-text font-semibold">{new Date(selectedLoan.fechaMaxDevolucion).toLocaleDateString()}</span>
                </div>
              </div>
            </div>
          )}

          <div>
            <label className="text-xs font-bold text-text block uppercase tracking-wider mb-2">Estado Físico de la Obra</label>
            <div className="flex gap-4">
              <label className="flex items-center gap-2 text-sm text-text cursor-pointer">
                <input 
                  type="radio" 
                  name="state" 
                  value="Bueno"
                  checked={status === 'Bueno'}
                  onChange={() => setStatus('Bueno')}
                  className="accent-primary"
                /> Bueno (Disponible)
              </label>
              <label className="flex items-center gap-2 text-sm text-text cursor-pointer">
                <input 
                  type="radio" 
                  name="state" 
                  value="Dañado"
                  checked={status === 'Dañado'}
                  onChange={() => setStatus('Dañado')}
                  className="accent-primary"
                /> Dañado (Mantenimiento)
              </label>
              <label className="flex items-center gap-2 text-sm text-text cursor-pointer">
                <input 
                  type="radio" 
                  name="state" 
                  value="Pérdida"
                  checked={status === 'Pérdida'}
                  onChange={() => setStatus('Pérdida')}
                  className="accent-primary"
                /> Pérdida / Extraviado
              </label>
            </div>
          </div>

          <div>
            <label className="text-xs font-bold text-text block uppercase tracking-wider mb-1">Observaciones</label>
            <textarea 
              rows={3}
              placeholder="Escribe comentarios sobre rayas, roturas de cubiertas, etc."
              value={obs}
              onChange={(e) => setObs(e.target.value)}
              className="w-full px-4 py-2.5 bg-card border border-border rounded-xl text-text placeholder-placeholder focus:outline-none focus:border-primary transition text-sm"
            />
          </div>

          <div className="flex gap-3 pt-4">
            <button 
              type="submit"
              disabled={loading}
              className="px-5 py-3 bg-rose-600 hover:bg-rose-500 active:bg-rose-700 text-white font-semibold rounded-xl text-sm transition shadow-lg shadow-rose-600/10 disabled:opacity-50"
            >
              {loading ? 'Actualizando base de datos...' : 'Registrar Retorno de Obra'}
            </button>
            <button 
              type="button" 
              onClick={resetForm}
              className="px-5 py-3 bg-card hover:bg-border/20 text-text hover:text-primary transition rounded-xl text-sm font-semibold border border-border"
            >
              Cancelar
            </button>
          </div>
        </form>
      ) : (
        // Pantalla E: Resultados de la Devolución y Alerta de Sanción (RN-04)
        <div className="space-y-6">
          <div className="p-4 bg-border/30 border border-border rounded-xl text-sm space-y-2">
            <div className="flex justify-between">
              <span className="text-text">Nro Préstamo</span>
              <span className="font-mono font-bold text-text">#{result.prestamoId}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-text">Fecha de Retorno</span>
              <span className="font-bold text-text">{new Date(result.fechaDevolucionEfectiva).toLocaleString()}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-text">Nuevo Estado de Inventario</span>
              <span className="font-bold text-primary">{result.nuevoEstadoEjemplar}</span>
            </div>
          </div>

          {result.penalizacionAplicada && result.sancion ? (
            <div className="p-5 bg-rose-500/10 border border-rose-500/20 rounded-2xl text-rose-800 flex flex-col gap-2 shadow-xl animate-pulse">
              <span className="text-xs uppercase font-extrabold text-rose-600 block tracking-wider">Alerta de Sanción Registrada (RN-04)</span>
              <h4 className="text-base font-bold">¡DETECCION DE RETRASO CALENDARIO!</h4>
              <p className="text-xs text-rose-700/95 leading-relaxed">
                El ejemplar fue devuelto con un retraso de <span className="font-extrabold text-rose-950 font-black">{result.diasRetraso} días</span>.
                Se ha generado una sanción inmutable de <span className="font-extrabold text-rose-950 font-black">{result.sancion.diasSancion} días de suspensión</span>. La cuenta del alumno ha sido bloqueada temporalmente hasta el {new Date(result.sancion.fechaFin).toLocaleDateString()}.
              </p>
            </div>
          ) : (
            <div className="p-4 bg-emerald-500/10 border border-emerald-500/20 rounded-xl text-emerald-800 flex items-center gap-3 text-sm font-semibold">
              <CheckCircle2 className="w-5 h-5 text-emerald-600" />
              Retorno a tiempo. Sin penalizaciones pendientes.
            </div>
          )}

          <button 
            onClick={resetForm}
            className="w-full py-3 bg-[#6A5AE0] hover:bg-[#594ad1] text-white font-semibold rounded-xl text-sm transition"
          >
            Procesar Otro Retorno
          </button>
        </div>
      )}
    </div>
  );
}

// ==========================================
// PANTALLA F: Formulario de Catalogación e Ingesta Avanzada
// ==========================================
function CatalogingScreen({ setGlobalError, triggerSuccess }: { setGlobalError: any; triggerSuccess: any }) {
  const [titulo, setTitulo] = useState('');
  const [isbn, setIsbn] = useState('');
  const [autorNombre, setAutorNombre] = useState('');
  const [editorialNombre, setEditorialNombre] = useState('');
  const [categories, setCategories] = useState<any[]>([]);
  const [categorySearch, setCategorySearch] = useState('');
  const [showCategoryDropdown, setShowCategoryDropdown] = useState(false);
  const [copias, setCopias] = useState(1);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const loadCategories = async () => {
      try {
        const list = await apiClient.cataloging.getCategorias();
        setCategories(list);
      } catch (e) {
        // Fallback en caso de error
      }
    };
    loadCategories();
  }, []);

  const filteredCategories = categories.filter(c => 
    c.nombre.toLowerCase().includes(categorySearch.toLowerCase())
  );

  const handleIngest = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const matchedCategory = categories.find(c => c.nombre.toLowerCase() === categorySearch.trim().toLowerCase());
      const book = await apiClient.cataloging.createBook({
        titulo,
        isbn,
        autorId: 1, // Fallback en BD, se resolverá por autorNombre
        categoriaId: matchedCategory ? matchedCategory.id : 1,
        editorialId: 1, // Fallback en BD, se resolverá por editorialNombre
        autorNombre: autorNombre.trim(),
        editorialNombre: editorialNombre.trim(),
        categoriaNombre: matchedCategory ? undefined : categorySearch.trim(),
        fechaPublicacion: new Date().toISOString().split('T')[0],
        idioma: 'Español',
        pais: 'Perú',
        tipoMaterial: 'LibroFisico'
      });

      // Crear las copias físicas asociadas
      for (let i = 1; i <= copias; i++) {
        await apiClient.cataloging.createEjemplar(book.id, `${isbn}-C${i}`, `Estante A-${i}`, 'Bueno');
      }

      triggerSuccess(`Obra ingresada correctamente. Se generaron ${copias} ejemplares con códigos de barra de auditoría.`);
      setTitulo('');
      setIsbn('');
      setAutorNombre('');
      setEditorialNombre('');
      setCategorySearch('');
      setCopias(1);
    } catch (err: any) {
      setGlobalError({ code: err.errorResponse?.code || 'ERR_CATALOGING', title: 'Error de Ingesta', detail: err.message });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-3xl w-full mx-auto bg-card border border-border p-8 rounded-2xl text-left space-y-6 animate-in fade-in duration-300">
      <div>
        <h2 className="text-xl font-black text-text">Ingesta y Catalogación Avanzada</h2>
        <p className="text-text text-xs mt-1">Da de alta libros en el catálogo público ingresando los metadatos de autor, categoría y editorial en tiempo real.</p>
      </div>

      <form onSubmit={handleIngest} className="space-y-5">
        <div>
          <label className="text-xs font-bold text-text block uppercase tracking-wider mb-1">Título del Recurso</label>
          <input 
            type="text" 
            required
            placeholder="ej: Clean Code: A Handbook of Agile Software Craftsmanship"
            value={titulo}
            onChange={(e) => setTitulo(e.target.value)}
            className="w-full px-4 py-2.5 bg-card border border-border rounded-xl text-text placeholder-placeholder focus:outline-none focus:border-primary transition text-sm font-medium"
          />
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="text-xs font-bold text-text block uppercase tracking-wider mb-1">Código ISBN</label>
            <input 
              type="text" 
              required
              placeholder="ej: 9780132350884"
              value={isbn}
              onChange={(e) => setIsbn(e.target.value)}
              className="w-full px-4 py-2.5 bg-card border border-border rounded-xl text-text placeholder-placeholder focus:outline-none focus:border-primary transition text-sm font-mono"
            />
          </div>

          <div>
            <label className="text-xs font-bold text-text block uppercase tracking-wider mb-1">Autor Autoridad</label>
            <input 
              type="text" 
              required
              placeholder="ej: Robert C. Martin"
              value={autorNombre}
              onChange={(e) => setAutorNombre(e.target.value)}
              className="w-full px-4 py-2.5 bg-card border border-border rounded-xl text-text placeholder-placeholder focus:outline-none focus:border-primary transition text-sm font-medium"
            />
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="relative">
            <label className="text-xs font-bold text-text block uppercase tracking-wider mb-1">Categoría / Temática</label>
            <input 
              type="text" 
              placeholder="Escriba para filtrar categoría..."
              value={categorySearch}
              onChange={(e) => {
                setCategorySearch(e.target.value);
                setShowCategoryDropdown(true);
              }}
              onFocus={() => setShowCategoryDropdown(true)}
              className="w-full px-4 py-2.5 bg-card border border-border rounded-xl text-text placeholder-placeholder focus:outline-none focus:border-primary transition text-sm font-medium"
            />
            {showCategoryDropdown && (
              <>
                <div 
                  className="fixed inset-0 z-40" 
                  onClick={() => setShowCategoryDropdown(false)}
                />
                <div className="absolute z-50 w-full mt-1 bg-card border border-border rounded-xl shadow-2xl max-h-60 overflow-y-auto">
                  {filteredCategories.length > 0 ? (
                    filteredCategories.map(c => (
                      <button
                        key={c.id}
                        type="button"
                        onClick={() => {
                          setCategorySearch(c.nombre);
                          setShowCategoryDropdown(false);
                        }}
                        className="w-full text-left px-4 py-2 text-sm text-text hover:bg-primary hover:text-card transition font-medium"
                      >
                        {c.nombre}
                      </button>
                    ))
                  ) : (
                    <div className="px-4 py-2 text-xs text-text">No se encontraron categorías</div>
                  )}
                </div>
              </>
            )}
          </div>

          <div>
            <label className="text-xs font-bold text-text block uppercase tracking-wider mb-1">Casa Editorial</label>
            <input 
              type="text" 
              required
              placeholder="ej: Prentice Hall"
              value={editorialNombre}
              onChange={(e) => setEditorialNombre(e.target.value)}
              className="w-full px-4 py-2.5 bg-card border border-border rounded-xl text-text placeholder-placeholder focus:outline-none focus:border-primary transition text-sm font-medium"
            />
          </div>
        </div>

        <div>
          <label className="text-xs font-bold text-text block uppercase tracking-wider mb-1">Cantidad de Copias Físicas (Ejemplares)</label>
          <div className="flex items-center gap-3 bg-card border border-border rounded-xl p-1.5 w-32 justify-between">
            <button 
              type="button" 
              onClick={() => setCopias(c => Math.max(1, c - 1))}
              className="w-8 h-8 rounded-lg bg-card hover:bg-border/20 flex items-center justify-center font-bold transition"
            >
              -
            </button>
            <span className="font-bold text-sm text-text">{copias}</span>
            <button 
              type="button" 
              onClick={() => setCopias(c => c + 1)}
              className="w-8 h-8 rounded-lg bg-card hover:bg-border/20 flex items-center justify-center font-bold transition"
            >
              +
            </button>
          </div>
        </div>

        <div className="flex gap-3 pt-6">
          <button 
            type="submit"
            disabled={loading}
            className="px-5 py-3 bg-[#6A5AE0] hover:bg-[#594ad1] active:bg-[#483bc0] text-white font-semibold rounded-xl text-sm transition shadow-lg shadow-[#6A5AE0]/20 disabled:opacity-50"
          >
            {loading ? 'Registrando metadatos...' : 'Guardar en Catálogo'}
          </button>
          <button 
            type="button" 
            onClick={() => { setTitulo(''); setIsbn(''); setCopias(1); }}
            className="px-5 py-3 bg-card hover:bg-border/20 text-text hover:text-primary transition rounded-xl text-sm font-semibold border border-border"
          >
            Cancelar
          </button>
        </div>
      </form>
    </div>
  );
}

// ==========================================
// PANTALLA G: Dashboard de Reportes y Analíticas
// ==========================================
function DashboardScreen({ setGlobalError }: { setGlobalError: any; triggerSuccess: any }) {
  const [kpis, setKpis] = useState<any>(null);
  const [popular, setPopular] = useState<any[]>([]);
  const [problems, setProblems] = useState<any[]>([]);
  const [morosos, setMorosos] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const loadReportData = async () => {
      setLoading(true);
      try {
        const k = await apiClient.reports.getKpis();
        setKpis(k);

        const pop = await apiClient.reports.getPopular();
        setPopular(pop);

        const prob = await apiClient.reports.getProblematic();
        setProblems(prob);

        const mor = await apiClient.reports.getOverdueUsers();
        setMorosos(mor);
      } catch (e: any) {
        setGlobalError({ code: e.errorResponse?.code || 'ERR_REPORTS', title: 'Error de Reportes', detail: e.message });
      } finally {
        setLoading(false);
      }
    };

    loadReportData();
  }, []);

  const handleExportPDF = () => {
    const doc = new jsPDF();

    // Estilos principales de marca LibriKeep Pro
    doc.setFillColor(15, 23, 42); // slate-900 background for header
    doc.rect(0, 0, 210, 40, 'F');

    // Logo / Nombre
    doc.setTextColor(255, 255, 255);
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(22);
    doc.text('LibriKeep Pro', 14, 20);

    doc.setFont('helvetica', 'normal');
    doc.setFontSize(10);
    doc.setTextColor(148, 163, 184); // slate-400
    doc.text('Sistema de Gestión e Ingesta de Biblioteca', 14, 28);

    // Emisión (Julio de 2026)
    doc.setFontSize(9);
    doc.setTextColor(255, 255, 255);
    doc.text(`Fecha de Emisión: Julio de 2026`, 140, 25);

    // Separación y título de reporte
    doc.setFontSize(14);
    doc.setTextColor(15, 23, 42);
    doc.setFont('helvetica', 'bold');
    doc.text('REPORTE AUDITORÍA DE ESTADÍSTICAS Y MOROSIDAD', 14, 55);

    // KPIs resumen
    doc.setFillColor(241, 245, 249); // slate-100
    doc.roundedRect(14, 65, 182, 30, 2, 2, 'F');

    doc.setFontSize(10);
    doc.setTextColor(71, 85, 105); // slate-600
    doc.text(`Total Ejemplares Registrados: ${kpis?.totalEjemplares ?? 0}`, 20, 75);
    doc.text(`Préstamos Activos: ${kpis?.prestamosActivos ?? 0}`, 20, 85);
    doc.text(`Tasa de Morosidad: ${kpis?.tasaMorosidad ?? 0}%`, 110, 75);
    doc.text(`Usuarios Bloqueados/Morosos: ${kpis?.usuariosBloqueados ?? 0}`, 110, 85);

    // Tabla 1: Lectores Morosos
    doc.setFontSize(12);
    doc.setTextColor(15, 23, 42);
    doc.text('1. PADRÓN DE LECTORES MOROSOS Y SUSPENDIDOS', 14, 110);

    const morososRows = morosos.map(m => [
      m.nombreCompleto,
      m.dni,
      m.email,
      `${m.prestamosVencidosCount} préstamo(s)`,
      m.estadoUsuario
    ]);

    autoTable(doc, {
      startY: 115,
      head: [['Lector', 'DNI', 'Correo', 'Préstamos Vencidos', 'Estado']],
      body: morososRows.length > 0 ? morososRows : [['No hay lectores morosos registrados', '', '', '', '']],
      theme: 'grid',
      headStyles: { fillColor: [178, 112, 82] }, // primary brand color
      styles: { fontSize: 9 }
    });

    // Tabla 2: Ejemplares Dañados o en Mantenimiento
    const finalY = (doc as any).lastAutoTable.finalY + 15;
    doc.setFontSize(12);
    doc.text('2. ACTIVOS FÍSICOS EN CONFLICTO (MANTENIMIENTO/PÉRDIDA)', 14, finalY);

    const problemsRows = problems.map(p => [
      p.libroTitulo,
      p.codigoBarras,
      p.estado,
      p.observaciones
    ]);

    autoTable(doc, {
      startY: finalY + 5,
      head: [['Obra', 'Código de Barras', 'Estado Físico', 'Observaciones']],
      body: problemsRows.length > 0 ? problemsRows : [['No hay activos en conflicto', '', '', '']],
      theme: 'grid',
      headStyles: { fillColor: [225, 29, 72] }, // rose-600
      styles: { fontSize: 9 }
    });

    // Guardar PDF
    doc.save('Reporte_LibriKeep_Estadisticas.pdf');
  };

  if (loading) return <div className="text-center py-12 text-text text-sm">Procesando cubos analíticos...</div>;

  return (
    <div className="space-y-6 text-left animate-in fade-in duration-300">
      <div className="flex justify-between items-center flex-wrap gap-3">
        <div>
          <h2 className="text-xl font-black text-textOnBg font-sans">Panel de Reportes Estadísticos</h2>
          <p className="text-textOnBg/80 text-xs mt-1">Indicadores clave de rendimiento (KPIs), tasas de morosidad y control de activos físicos.</p>
        </div>
        <button 
          onClick={handleExportPDF}
          className="px-4 py-2 bg-card hover:bg-card/90 border border-border text-xs font-semibold rounded-lg text-text hover:text-primary transition"
        >
          Exportar PDF / Excel
        </button>
      </div>

      {/* Grid de KPIs */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="bg-card border border-border p-5 rounded-2xl relative overflow-hidden">
          <span className="text-[10px] text-text block uppercase font-bold tracking-wider">Total Ejemplares</span>
          <span className="text-3xl font-black text-primary mt-1 block">{kpis?.totalEjemplares}</span>
          <span className="text-[10px] text-emerald-700 font-bold mt-1.5 block">+2.4% este mes</span>
        </div>

        <div className="bg-card border border-border p-5 rounded-2xl relative overflow-hidden">
          <span className="text-[10px] text-text block uppercase font-bold tracking-wider">Préstamos Activos</span>
          <span className="text-3xl font-black text-primary mt-1 block">{kpis?.prestamosActivos}</span>
          <span className="text-[10px] text-primary font-bold mt-1.5 block">Ocupación física: 34%</span>
        </div>

        <div className="bg-card border border-border p-5 rounded-2xl relative overflow-hidden">
          <span className="text-[10px] text-text block uppercase font-bold tracking-wider">Tasa de Morosidad</span>
          <span className="text-3xl font-black text-rose-700 mt-1 block">{kpis?.tasaMorosidad}%</span>
          <span className="text-[10px] text-rose-700 font-bold mt-1.5 block">{kpis?.usuariosBloqueados} Usuarios Bloqueados [!]</span>
        </div>
      </div>

      {/* Grid de reportes avanzados */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        
        {/* Top Obras solicitadas */}
        <div className="bg-card border border-border p-6 rounded-2xl space-y-4">
          <h3 className="text-sm font-bold uppercase tracking-wider text-text">Obras Más Solicitadas (Mes Actual)</h3>
          <div className="space-y-3 font-mono text-xs">
            {popular.map((p, index) => (
              <div key={p.libroId} className="flex justify-between items-center p-3 bg-border/30 border border-border rounded-xl">
                <div>
                  <span className="text-primary font-bold mr-2">{index + 1}.</span>
                  <span className="text-text">{p.titulo}</span>
                  <span className="text-text text-[10px] block mt-0.5 ml-5">Autor: {p.autorNombre}</span>
                </div>
                <span className="font-bold text-text text-[11px] bg-card px-2 py-1 rounded border border-border shrink-0">{p.totalPrestamos} préstamos</span>
              </div>
            ))}
          </div>
        </div>

        {/* Ejemplares Dañados o Morosos */}
        <div className="space-y-6">
          <div className="bg-card border border-border p-6 rounded-2xl space-y-4">
            <h3 className="text-sm font-bold uppercase tracking-wider text-text">Reporte de Activos en Conflicto</h3>
            <div className="space-y-3">
              {problems.map(p => (
                <div key={p.ejemplarId} className="p-3 bg-border/30 border border-border rounded-xl text-xs flex justify-between items-center">
                  <div>
                    <span className="font-bold text-text block">{p.libroTitulo}</span>
                    <span className="text-[10px] text-text block mt-0.5">Motivo: {p.observaciones} | Código: {p.codigoBarras}</span>
                  </div>
                  <span className="px-2 py-0.5 rounded font-bold text-[9px] bg-rose-500/10 text-rose-700 border border-rose-500/20 uppercase shrink-0">
                    {p.estado}
                  </span>
                </div>
              ))}
            </div>
          </div>

          <div className="bg-card border border-border p-6 rounded-2xl space-y-4">
            <h3 className="text-sm font-bold uppercase tracking-wider text-text">Lectores Morosos</h3>
            <div className="space-y-3">
              {morosos.map(m => (
                <div key={m.usuarioId} className="p-3 bg-border/30 border border-border rounded-xl text-xs flex justify-between items-center">
                  <div>
                    <span className="font-bold text-text block">{m.nombreCompleto}</span>
                    <span className="text-[10px] text-text block mt-0.5">DNI: {m.dni} | {m.email}</span>
                  </div>
                  <div className="text-right shrink-0">
                    <span className="text-[10px] text-rose-700 font-bold block">{m.prestamosVencidosCount} Vencido(s)</span>
                    <span className="text-[9px] text-text block uppercase tracking-wider mt-0.5">{m.estadoUsuario}</span>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>

      </div>
    </div>
  );
}

function ReadersScreen({ currentUser, setGlobalError, triggerSuccess }: { currentUser: UsuarioDto; setGlobalError: any; triggerSuccess: any }) {
  const [users, setUsers] = useState<UsuarioDto[]>([]);
  const [dni, setDni] = useState('');
  const [nombreCompleto, setNombreCompleto] = useState('');
  const [email, setEmail] = useState('');
  const [telefono, setTelefono] = useState('');
  const [password, setPassword] = useState('Lector123');
  const [tipoMiembro, setTipoMiembro] = useState('Alumno');
  const [rol, setRol] = useState('Lector');
  const [loading, setLoading] = useState(false);
  const [loadingList, setLoadingList] = useState(false);

  // State for toggling password visibility in the table
  const [visiblePasswords, setVisiblePasswords] = useState<Record<number, boolean>>({});

  const togglePasswordVisibility = (userId: number) => {
    setVisiblePasswords(prev => ({ ...prev, [userId]: !prev[userId] }));
  };

  const formatPhone = (phone: string | undefined): string => {
    if (!phone) return '';
    const trimmed = phone.trim();
    if (trimmed.startsWith('+51') && trimmed.length === 12) {
      return `+51 ${trimmed.substring(3)}`;
    }
    return trimmed;
  };

  useEffect(() => {
    if (currentUser.rol === 'Bibliotecario') {
      setRol('Lector');
      setTipoMiembro('Alumno');
    }
  }, [currentUser]);

  const fetchUsers = async () => {
    setLoadingList(true);
    try {
      const list = await apiClient.users.list();
      setUsers(list);
    } catch (e: any) {
      setGlobalError({ code: e.errorResponse?.code || 'ERR_USERS', title: 'Error de Lectores', detail: e.message });
    } finally {
      setLoadingList(false);
    }
  };

  useEffect(() => {
    fetchUsers();
  }, []);

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      await apiClient.users.create({
        dni,
        nombreCompleto,
        email,
        password,
        telefono,
        tipoMiembro: currentUser.rol === 'Bibliotecario' ? 'Alumno' : tipoMiembro,
        rol: currentUser.rol === 'Bibliotecario' ? 'Lector' : rol
      });

      triggerSuccess(`Lector "${nombreCompleto}" registrado exitosamente en el sistema.`);
      setDni('');
      setNombreCompleto('');
      setEmail('');
      setTelefono('');
      setPassword('Lector123');
      fetchUsers();
    } catch (err: any) {
      setGlobalError({ 
        code: err.errorResponse?.code || 'ERR_USER_CREATION_FAILED', 
        title: 'Error de Registro', 
        detail: err.message 
      });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 text-left animate-in fade-in duration-300">
      {/* Formulario de registro */}
      <div className="lg:col-span-1 bg-card border border-border p-6 rounded-2xl self-start space-y-6">
        <div>
          <h2 className="text-xl font-black text-text">Registrar Lector / Usuario</h2>
          <p className="text-text text-xs mt-1">Da de alta cuentas de lectores en el padrón de la biblioteca para habilitar circulación.</p>
        </div>

        <form onSubmit={handleRegister} className="space-y-4">
          <div>
            <label className="text-xs font-bold text-text block uppercase tracking-wider mb-1">DNI / Cédula</label>
            <input 
              type="text" 
              required
              placeholder="ej: 71234567"
              value={dni}
              onChange={(e) => setDni(e.target.value)}
              className="w-full px-4 py-2.5 bg-card border border-border rounded-xl text-text placeholder-placeholder focus:outline-none focus:border-primary transition text-sm font-semibold"
            />
          </div>

          <div>
            <label className="text-xs font-bold text-text block uppercase tracking-wider mb-1">Nombre Completo</label>
            <input 
              type="text" 
              required
              placeholder="ej: Juan Pérez"
              value={nombreCompleto}
              onChange={(e) => setNombreCompleto(e.target.value)}
              className="w-full px-4 py-2.5 bg-card border border-border rounded-xl text-text placeholder-placeholder focus:outline-none focus:border-primary transition text-sm font-semibold"
            />
          </div>

          <div>
            <label className="text-xs font-bold text-text block uppercase tracking-wider mb-1">Correo Electrónico</label>
            <input 
              type="email" 
              required
              placeholder="ej: alumno@uni.edu.pe"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full px-4 py-2.5 bg-card border border-border rounded-xl text-text placeholder-placeholder focus:outline-none focus:border-primary transition text-sm font-semibold"
            />
          </div>

          <div>
            <label className="text-xs font-bold text-text block uppercase tracking-wider mb-1">Teléfono</label>
            <input 
              type="text" 
              required
              placeholder="ej: +51999888777"
              value={telefono}
              onChange={(e) => setTelefono(e.target.value)}
              className="w-full px-4 py-2.5 bg-card border border-border rounded-xl text-text placeholder-placeholder focus:outline-none focus:border-primary transition text-sm font-semibold"
            />
          </div>

          <div>
            <label className="text-xs font-bold text-text block uppercase tracking-wider mb-1">Contraseña por Defecto</label>
            <input 
              type="password" 
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full px-4 py-2.5 bg-card border border-border rounded-xl text-text focus:outline-none focus:border-primary transition text-sm font-semibold"
            />
          </div>

          {currentUser.rol === 'Administrador' && (
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="text-xs font-bold text-text block uppercase tracking-wider mb-1">Tipo Miembro</label>
                <select 
                  value={tipoMiembro} 
                  onChange={(e) => setTipoMiembro(e.target.value)}
                  className="w-full px-3 py-2.5 bg-card border border-border rounded-xl text-text focus:outline-none focus:border-primary transition text-xs font-bold"
                >
                  <option value="Alumno">Alumno</option>
                  <option value="Docente">Docente</option>
                  <option value="PersonalAdministrativo">Administrativo</option>
                  <option value="Externo">Externo</option>
                </select>
              </div>

              <div>
                <label className="text-xs font-bold text-text block uppercase tracking-wider mb-1">Rol Sistema</label>
                <select 
                  value={rol} 
                  onChange={(e) => setRol(e.target.value)}
                  className="w-full px-3 py-2.5 bg-card border border-border rounded-xl text-text focus:outline-none focus:border-primary transition text-xs font-bold"
                >
                  <option value="Lector">Lector</option>
                  <option value="Bibliotecario">Bibliotecario</option>
                  <option value="Administrador">Administrador</option>
                </select>
              </div>
            </div>
          )}

          <div className="flex gap-3 pt-4">
            <button 
              type="submit"
              disabled={loading}
              className="px-5 py-3 bg-[#6A5AE0] hover:bg-[#594ad1] active:bg-[#483bc0] text-white font-semibold rounded-xl text-sm transition shadow-lg shadow-[#6A5AE0]/20 disabled:opacity-50"
            >
              {loading ? 'Guardando en BD...' : 'Registrar Lector'}
            </button>
          </div>
        </form>
      </div>

      {/* Listado de Lectores */}
      <div className="lg:col-span-2 bg-card border border-border p-6 rounded-2xl">
        <div className="flex justify-between items-center mb-6">
          <div>
            <h3 className="text-lg font-black text-text">Padrón de Lectores y Usuarios Registrados</h3>
            <p className="text-text text-xs mt-1">Listado oficial de usuarios activos en la plataforma para consulta y auditoría de circulación.</p>
          </div>
          <button 
            onClick={fetchUsers} 
            disabled={loadingList}
            className="px-3.5 py-2 bg-card hover:bg-border/20 border border-border rounded-xl text-xs font-semibold text-text hover:text-primary transition"
          >
            {loadingList ? 'Refrescando...' : 'Refrescar'}
          </button>
        </div>

        {users.length === 0 ? (
          <div className="text-center py-12 text-text text-sm">Cargando padrón de lectores o no hay usuarios registrados...</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-xs text-left border-collapse">
              <thead>
                <tr className="border-b border-border text-text uppercase font-bold text-[10px] tracking-wider">
                  <th className="pb-3">Nombre / DNI</th>
                  <th className="pb-3">Contacto</th>
                  <th className="pb-3">Tipo Miembro</th>
                  <th className="pb-3">Rol</th>
                  <th className="pb-3 text-right">Estado</th>
                </tr>
              </thead>
              <tbody>
                {users.map(u => (
                  <tr key={u.id} className="border-b border-border text-text align-middle hover:bg-border/20 transition">
                    <td className="py-4 font-semibold">
                      <span className="block text-text text-sm">{u.nombreCompleto}</span>
                      <span className="block font-mono text-[10px] text-text mt-0.5">DNI: {u.dni}</span>
                    </td>
                    <td className="py-4">
                      <span className="block text-text">{u.email}</span>
                      {u.telefono && <span className="block text-text text-[10px] mt-0.5">Tel: {formatPhone(u.telefono)}</span>}
                      {currentUser.rol === 'Administrador' && (
                        <div className="flex items-center gap-1.5 mt-1 text-[10px] text-text/80 font-mono">
                          <span>Clave: {visiblePasswords[u.id] ? (u.password || '••••••••') : '••••••••'}</span>
                          <button
                            type="button"
                            onClick={() => togglePasswordVisibility(u.id)}
                            className="text-[#6A5AE0] hover:underline font-sans font-bold cursor-pointer focus:outline-none"
                          >
                            {visiblePasswords[u.id] ? '[Ocultar]' : '[Mostrar]'}
                          </button>
                        </div>
                      )}
                    </td>
                    <td className="py-4 font-semibold text-text">{u.tipoMiembro}</td>
                    <td className="py-4 font-semibold text-text">{u.rol}</td>
                    <td className="py-4 text-right">
                      <span className="px-2 py-0.5 rounded font-bold text-[10px] bg-emerald-500/10 text-emerald-450 border border-emerald-500/20">
                        {u.estado}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
