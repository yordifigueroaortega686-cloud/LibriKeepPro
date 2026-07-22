# Guía de Implementación y Zonas UI: Ecosistema LibriKeep Pro

Esta guía detalla la especificación visual y la distribución de la interfaz de usuario para todas las pantallas del ecosistema de **LibriKeep Pro**, organizada mediante zonas cardinales numeradas.

---

## 🎨 1. GUÍA DE ESTILOS Y COMPONENTES BASE

El diseño visual de la plataforma busca transmitir una estética premium y de alta legibilidad basada en los siguientes lineamientos:
- **Paleta de Colores:** 
  - **Fondo Base del Aplicativo:** Azul petróleo o turquesa oscuro profundo para dar un ambiente inmersivo, elegante y moderno.
  - **Tarjetas y Contenedores:** Crema o marfil suave, creando un contraste cómodo y limpio para el ojo humano.
  - **Botones de Acción:** Púrpura o azulado oscuro, indicando interactividad principal.
  - **Acciones Críticas o Alertas:** Terracota o rojo apagado para avisos de inhabilitación, morosidad o cancelaciones.
- **Tipografía y Contraste:** Se utiliza una tipografía limpia y sin serifa de alta legibilidad. Toda la información de formularios, listados y tablas de datos se renderiza en texto oscuro sobre los contenedores claros para asegurar una lectura confortable y sin fatiga visual durante jornadas de trabajo completas.

---

## 🖥️ 2. PANTALLA 1: CATÁLOGO OPAC (VISTA PÚBLICA)

Esta pantalla permite la consulta abierta de obras y ejemplares en estantería para el público general.

*   **Zona 1 (Barra Superior / Navigation Header):** Cabecera fija horizontal de color azul océano translúcido. Muestra a la izquierda el logotipo de la marca con su insignia "PRO" en un contenedor destacado y el subtítulo secundario "Spec-Driven Library Architecture". A la derecha, contiene los accesos de navegación principal en forma de botones: "Catálogo OPAC" e "Iniciar Sesión" (el cual muestra una alerta suave en la parte superior confirmando si el usuario ha cerrado sesión).
*   **Zona 2 (Hero / Buscador Principal):** Bloque destacado que centra la pantalla. Presenta un título de gran formato denominado "Catálogo de Biblioteca OPAC" y un párrafo con letras en tono crema que detalla el volumen total de activos del inventario. Debajo se sitúa un campo de entrada interactivo con una lupa vectorial que muestra el placeholder "Buscar por título, autor o ISBN..." y un botón al costado etiquetado como "Buscar".
*   **Zona 3 (Módulos Temáticos / Grid de Categorías):** Cuadrícula responsiva que muestra tarjetas individuales para cada categoría temático-académica. Cada tarjeta contiene un ícono gráfico del tema, la etiqueta indicadora "MÓDULO TEMÁTICO", el nombre de la materia (como Ingeniería de Software) y un contador numérico de ejemplares registrados. Al realizar búsquedas, este grid se transforma dinámicamente en una lista de libros coincidentes con su respectiva información de título, autor, ISBN y un badge de disponibilidad.
*   **Zona 4 (Panel Lateral / Detalle de Existencias Físicas):** Contenedor flotante que aparece por el lateral derecho. Si no hay selección, presenta un mensaje instructivo sobre fondo crema. Al seleccionar una obra del grid, despliega de inmediato la ficha técnica completa del recurso y un listado de todas sus copias físicas con su código de barras único, su ubicación física (pasillo y estante) y las incidencias detectadas.
*   **Zona 5 (Pie de Página / Footer):** Barra inferior horizontal en color crema translúcido que muestra a la izquierda el descargo de responsabilidad y derechos de autor "© 2026 LibriKeep Pro. Diseñado bajo estándares de SDD estricto" y a la derecha un conjunto de insignias tecnológicas institucionales (OpenAPI Spec, PostgreSQL, .NET Core 10, React 19).

---

## 🖥️ 3. PANTALLA 2: PANEL DE REPORTES ESTADÍSTICOS (KPIs Y MOROSIDAD)

Vista gerencial y operativa para el Administrador para controlar la salud física y financiera de la biblioteca.

*   **Zona 1 (Barra de Navegación de Administración):** Menú superior extendido y fijo en color oscuro. Contiene pestañas de navegación con accesos a: "Catálogo OPAC", "Reportes", "Préstamos", "Devoluciones", "Catalogación" y "Lectores". Al final de la barra se visualiza el nombre y rol del operador "Admin Principal - ADMINISTRADOR" junto a un botón de salida.
*   **Zona 2 (Encabezado y Acciones Globales):** Sección superior con el título principal "Panel de Reportes Estadísticos" y un párrafo introductorio de los KPIs. En la esquina superior derecha se ubica un botón de descarga rápida para exportar el informe consolidado denominado "Exportar PDF / Excel".
*   **Zona 3 (Tarjetas de Métricas de Alto Nivel - KPIs Top):** Fila de tres tarjetas rectangulares en fondo marfil:
    *   *Tarjeta 1:* "TOTAL EJEMPLARES" que expone el conteo histórico de copias en letra gigante y un indicador de crecimiento en color verde.
    *   *Tarjeta 2:* "PRÉSTAMOS ACTIVOS" con el total de libros en circulación y la tasa de ocupación física del inventario.
    *   *Tarjeta 3:* "TASA DE MOROSIDAD" que muestra el porcentaje en color terracota/alerta y el número total de alumnos e inhabilitados vigentes.
*   **Zona 4 (Paneles Inferiores de Desglose Detallado):** Grid de información de dos columnas principales:
    *   *Columna Izquierda (Ancha):* Panel "OBRAS MÁS SOLICITADAS (MES ACTUAL)" que detalla el listado ordenado de libros más leídos.
    *   *Columna Derecha (Superior):* Panel "REPORTE DE ACTIVOS EN CONFLICTO" con ejemplares reportados como dañados o perdidos.
    *   *Columna Derecha (Inferior):* Panel "LECTORES MOROSOS" con el listado dinámico de usuarios suspendidos, su DNI, total de retrasos acumulados y contacto institucional.
*   **Zona 5 (Footer):** Mantiene el pie de página institucional del sistema.

---

## 🖥️ 4. PANTALLA 3: REGISTRO DE NUEVO PRÉSTAMO (CIRCULACIÓN)

Formulario de ventanilla para el Bibliotecario para autorizar y registrar salidas de material de forma física.

*   **Zona 1 (Barra de Navegación Administrador):** Barra horizontal de administración con la pestaña "Préstamos" seleccionada visualmente con un subrayado brillante.
*   **Zona 2 (Formulario Principal de Salida de Ejemplar - Lado Izquierdo):** Panel de fondo marfil que contiene:
    *   Título destacado "Registrar Nuevo Préstamo" y un texto explicativo con los límites vigentes y causales de bloqueo.
    *   Campo "DNI DEL LECTOR" con placeholder descriptivo "Escribe DNI o nombre del lector...".
    *   Campo "CÓDIGO DE BARRAS DEL EJEMPLAR" con placeholder "Escribe código de barras o título de la obra...".
    *   Campo "FECHA LÍMITE DE DEVOLUCIÓN" que contiene un componente calendario para elegir el retorno (establecido de forma automática a siete días calendario por defecto).
    *   Dos botones inferiores alineados: "Confirmar Préstamo" en color púrpura y "Cancelar" en tono neutro.
*   **Zona 3 (Panel de Métricas Rápidas y Salidas - Lado Derecho):** Panel lateral dividido en:
    *   *Sección Superior:* Contenedor "MÉTRICAS DE CIRCULACIÓN" con dos contadores grandes denominados "PRESTADOS HOY" y "MOROSOS".
    *   *Sección Inferior:* Historial de "SALIDAS RECIENTES" que lista los últimos préstamos con su DNI y hora, o en su defecto un aviso claro de "No hay préstamos activos".
*   **Zona 4 (Footer):** Estándar del sistema.

---

## 🖥️ 5. PANTALLA 4: PROCESAR DEVOLUCIÓN EFECTIVA (RETORNO Y SANCIONES)

Pantalla de retorno que gestiona las recepciones físicas y el cálculo automatizado de inhabilitaciones.

*   **Zona 1 (Barra de Navegación Administrador):** Menú de navegación con la opción "Devoluciones" activa y resaltada.
*   **Zona 2 (Contenedor Central de Devolución):** Tarjeta central amplia de color marfil que recopila:
    *   Título "Procesar Devolución Efectiva" y bajada descriptiva sobre el cálculo del doble de días de inhabilitación ante retrasos.
    *   Lista de selección "SELECCIONAR PRÉSTAMO ACTIVO" con el placeholder "-- Elige un préstamo activo --".
    *   Grupo de botones de opción (Radio Buttons) para indicar el "Estado Físico" del ejemplar devuelto: "Bueno (Disponible)", "Dañado (Mantenimiento)" o "Pérdida / Extraviado".
    *   Área de comentarios extendida "OBSERVACIONES" con la sugerencia "Escribe comentarios sobre rayas, roturas de cubiertas, etc.".
    *   Botón destacado de acción "Registrar Retorno de Obra" en color terracota/rojo de acento, y el botón "Cancelar" al lado.
*   **Zona 3 (Footer):** Estándar del sistema.

---

## 🖥️ 6. PANTALLA 5: INGESTA Y CATALOGACIÓN AVANZADA (ALTA DE LIBROS)

Pantalla dedicada al registro y catalogación de nuevos títulos y ejemplares en el catálogo general de la institución.

*   **Zona 1 (Barra de Navegación Administrador):** Menú de navegación con la opción "Catalogación" activa.
*   **Zona 2 (Formulario Central de Registro de Recursos):** Panel central con fondo marfil que estructura:
    *   Título principal "Ingesta y Catalogación Avanzada" y descripción del registro.
    *   Campo de texto principal a todo lo ancho: "TÍTULO DEL RECURSO" (ejemplo: "Clean Code: A Handbook of Agile Software Craftsmanship").
    *   Fila de dos columnas con los campos: "CÓDIGO ISBN" (ejemplo: "9780132350884") y "AUTOR AUTORIDAD" (ejemplo: "Robert C. Martin").
    *   Segunda fila de dos columnas con los campos: "CATEGORÍA / TEMÁTICA" (menú desplegable/buscador) y "CASA EDITORIAL" (ejemplo: "Prentice Hall").
    *   Selector interactivo de "CANTIDAD DE COPIAS FÍSICAS (EJEMPLARES)" con controles numéricos de suma y resta.
    *   Botón de confirmación "Guardar en Catálogo" en color púrpura y botón de escape "Cancelar".
*   **Zona 3 (Footer):** Estándar del sistema.

---

## 🖥️ 7. PANTALLA 6: GESTIÓN DE LECTORES Y PADRÓN DE USUARIOS

Pantalla para registrar y administrar los datos y estado de cuenta de los miembros y el personal.

*   **Zona 1 (Barra de Navegación Administrador):** Menú con la pestaña "Lectores" seleccionada.
*   **Zona 2 (Formulario de Registro - Columna Izquierda):** Contenedor de entrada con:
    *   Título "Registrar Lector / Usuario".
    *   Campos verticales para ingresar: "DNI / CÉDULA", "NOMBRE COMPLETO", "CORREO ELECTRÓNICO", "TELÉFONO" y "CONTRASEÑA POR DEFECTO".
    *   Menús desplegables horizontales: "TIPO MIEMBRO" (opciones: Alumno, Docente, Personal Administrativo) y "ROL SISTEMA" (opciones: Lector, Bibliotecario, Administrador).
    *   Botón de guardado final "Registrar Lector" en la parte inferior.
*   **Zona 3 (Tabla/Padrón de Usuarios - Columna Derecha):** Bloque amplio de visualización de datos:
    *   Encabezado "Padrón de Lectores y Usuarios Registrados" con un botón al extremo derecho para "Refrescar" la información en caliente.
    *   Tabla estructurada con cabeceras de columnas: "NOMBRE / DNI", "CONTACTO" (que despliega el correo, número telefónico y un link interactivo para revelar la contraseña asignada), "TIPO MIEMBRO", "ROL" y "ESTADO" (representado con una insignia verde indicando "Activo" o terracota indicando "Inhabilitado").
*   **Zona 4 (Footer):** Estándar del sistema.
