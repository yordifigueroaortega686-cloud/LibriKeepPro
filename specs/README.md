# Manifiesto General: Spec Kit para LibriKeep Pro

Bienvenido a la documentación oficial de arquitectura de **LibriKeep Pro**, estructurada bajo la metodología de **Spec-Driven Development (SDD)** y el framework de **Spec Kit**. 

Toda nuestra documentación técnica está diseñada en lenguaje puramente humano y narrativo, omitiendo bloques de código fuente para garantizar que sea accesible, comprensible y mantenible para cualquier miembro del equipo de ingeniería o del negocio.

---

## 🏗️ ARQUITECTURA GENERAL DEL SISTEMA

LibriKeep Pro se compone de un backend robusto de alto rendimiento en **.NET versión 10** bajo el paradigma de **Clean Architecture** y **CQRS**, persistido en una base de datos relacional de alta integridad **PostgreSQL versión 16 (Neon Cloud)**, y un cliente web interactivo desarrollado en **React versión 19** y estilizado con **Tailwind CSS**.

El sistema se divide conceptualmente en dos módulos operacionales:
1. **Módulo de Catalogación:** Controla la ingesta de obras, la validación de códigos ISBN y la administración de copias físicas y sus ubicaciones en estantería.
2. **Módulo de Circulación:** Controla los préstamos físicos, devoluciones, reservas en cola, límites de membresías y el cálculo automático de inhabilitaciones de cuenta por morosidad.

---

## ⚙️ REQUISITOS GENERALES DEL SISTEMA

A continuación se consolidan los requisitos lógicos, operativos y de calidad aplicados a todo el ecosistema de la plataforma:

### Requisitos Funcionales Globales
1. **Autenticación y Control de Accesos:** El sistema debe restringir el acceso a las funciones operativas de circulación y catalogación basándose en los roles (Lector, Bibliotecario y Administrador) y encriptar las credenciales de usuario.
2. **Ingesta y Registro de Obras:** Permitir el alta de libros normalizando el código ISBN a formato numérico de diez o trece dígitos, y autocompletar dinámicamente autores, categorías y editoriales.
3. **Inventariado de Ejemplares Físicos:** Permitir asociar copias físicas individuales a un libro catalogado, asignando un código de barras único de activo, ubicación física y observaciones.
4. **Búsqueda Pública en Catálogo OPAC:** Ofrecer un portal público para buscar libros por título, autor e ISBN, permitiendo pre-filtrar resultados según categorías.
5. **Control de Ficha del Lector:** Permitir a los alumnos y docentes visualizar sus préstamos activos, las fechas límite de entrega, el estado de su cuota y el registro de inhabilitaciones aplicadas.
6. **Despacho y Registro de Préstamos:** Registrar salidas de ejemplares Disponibles para lectores habilitados, bloqueando el despacho si superan su límite de préstamos simultáneos (Docente hasta cinco, Alumno y Personal Administrativo hasta tres).
7. **Control de Bloqueos en Circulación:** Bloquear transacciones si el lector posee multas impagadas, inactividad o préstamos activos vencidos.
8. **Procesamiento de Devoluciones y Penalizaciones:** Cerrar préstamos evaluando el estado físico de la copia y calcular de forma automática una suspensión por el doble de días calendario de demora en caso de entregas tardías.
9. **Gestión de Reservas:** Permitir a los usuarios reservar libros sin copias físicas disponibles, derivando prioritariamente cualquier ejemplar devuelto al estado Reservado para el primer lector en cola.
10. **Reportabilidad y Dashboard:** Proveer estadísticas dinámicas de morosidad, total de activos y volumen diario de circulación, permitiendo exportar reportes de inventario a formato PDF.

### Requisitos No Funcionales Globales
1. **Desempeño Operativo:** Las consultas del catálogo OPAC y filtrado temático deben retornar respuestas en menos de un segundo bajo condiciones de red normales.
2. **Consistencia Transaccional:** Las operaciones relacionales de préstamo, devolución, cobro de multas y reservas de libros deben ser atómicas para asegurar la consistencia.
3. **Concurrencia Optimista:** La base de datos debe interceptar y mitigar modificaciones simultáneas sobre el estado del inventario utilizando tokens de concurrencia optimista.
4. **Almacenamiento Horario UTC:** Todas las marcas de tiempo registradas en las transacciones de circulación y vigencia de sanciones deben formatearse y almacenarse en UTC.
5. **Manejo de Respuestas de Error:** Cualquier anomalía en el servidor debe retornar una respuesta estructurada bajo el estándar internacional RFC 7807 (Problem Details) con códigos de error semánticos.
6. **Mapeo Relacional de Datos:** El acceso físico a PostgreSQL debe regirse estrictamente por configuraciones de Fluent API en la Capa de Persistencia, forzando índices y unicidades.
7. **Estilo Visual Adaptable:** La interfaz debe estar estructurada mediante Tailwind CSS respetando la paleta de colores terracota, azul océano y crema suave, optimizando la visualización en cualquier dispositivo.

---

## 🔄 EL FLUJO DE DOCUMENTACIÓN EN 7 FASES

Para dar mantenimiento a la plataforma o integrar futuras características, el equipo de desarrollo debe navegar y actualizar los archivos estructurados en las siguientes siete fases continuas dentro de cada módulo:

### Fase 1: Constitución del Proyecto
* **Ubicación:** `.specify/memory/constitution.md`
* **Propósito:** El manifiesto supremo. Describe el stack de tecnologías autorizadas de backend, base de datos y frontend, justificando su uso, y establece las políticas estrictas de calidad, validación incondicional y respuesta RFC 7807 ante anomalías.

### Fase 2: Especificación Funcional (Spec)
* **Ubicación:** `specs/[modulo]/spec.md`
* **Propósito:** El "Qué" del negocio. Detalla el alcance desde el punto de vista del usuario final mediante historias de usuario y escenarios narrativos en formato de Dado que, Cuando y Entonces.

### Fase 3: Planificación de Arquitectura (Plan)
* **Ubicación:** `specs/[modulo]/plan.md`
* **Propósito:** El "Cómo" conceptual. Explica narrativamente el recorrido que realiza la información a través de las cuatro capas físicas del backend (Dominio, Aplicación, Persistencia y Presentación) y el impacto de los cambios a realizar.

### Fase 4: Modelo de Datos y Campos (Data Model)
* **Ubicación:** `specs/[modulo]/data-model.md`
* **Propósito:** La estructura de la información. Especifica mediante tablas detalladas cada uno de los campos de las tablas de base de datos, indicando su nombre funcional, tipo lógico, obligatoriedad, reglas de negocio e índices.

### Fase 5: Lista de Tareas (Tasks)
* **Ubicación:** `specs/[modulo]/tasks.md`
* **Propósito:** La hoja de ruta de ejecución. Checklist ordenado cronológicamente para la codificación segura en las capas del backend, configuraciones de base de datos y frontend.

### Fase 6: Guía de Implementación y Zonas UI (Implement)
* **Ubicación:** `specs/[modulo]/implement-guide.md`
* **Propósito:** La experiencia de usuario y comportamiento de API. Describe visualmente las pantallas mediante cinco zonas cardinales numeradas (Header, Buscador, Grid de Módulos, Panel Lateral y Footer) detallando el texto exacto de sugerencia (placeholder) y el comportamiento de las respuestas del servidor.

### Fase 7: Especificación de Pruebas (Testing)
* **Ubicación:** `specs/[modulo]/testing-spec.md`
* **Propósito:** El aseguramiento de calidad. Describe los casos de prueba unitaria de las entidades de dominio y los flujos completos de pruebas de integración para validar el cumplimiento de las reglas del negocio.

---

## 🚀 GUÍA DE NAVEGACIÓN Y MEJORAS FUTURAS

Cuando se desee agregar un nuevo módulo (por ejemplo, un módulo de notificaciones automáticas):
1. Iniciar la Fase 2 creando `specs/notificaciones/spec.md` para plasmar historias de usuario del envío de avisos.
2. Proceder con la planificación en `plan.md` y estructurar los campos necesarios en `data-model.md`.
3. Desglosar los pasos de codificación en `tasks.md`, diseñar la integración visual en `implement-guide.md` y establecer los escenarios de validación en `testing-spec.md`.
4. Una vez completado el flujo, actualizar este manifiesto general para añadir el nuevo componente al mapa general de la biblioteca.
