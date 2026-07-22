# Especificación Funcional: Catalogación e Ingesta Bibliográfica

Esta especificación describe el comportamiento funcional, los requisitos del sistema y las reglas de negocio del módulo de Catalogación de **LibriKeep Pro** desde la perspectiva del usuario y del operador.

---

## 👥 HISTORIAS DE USUARIO

### Historia de Usuario 1: Registro de Obras Bibliográficas
Como Bibliotecario de la institución, quiero registrar nuevas obras en el catálogo de la biblioteca, para mantener actualizada la colección intelectual y permitir que los lectores conozcan el material de estudio disponible.

### Historia de Usuario 2: Registro de Ejemplares Físicos
Como Bibliotecario de la institución, quiero registrar los ejemplares físicos individuales vinculados a una obra, asignándoles códigos de barras y ubicaciones específicas en la estantería, para controlar el inventario real disponible.

### Historia de Usuario 3: Búsqueda y Localización en Catálogo OPAC
Como Lector de la biblioteca (Alumno o Docente), quiero buscar libros en el catálogo por título, autor o código ISBN, y poder filtrar los resultados por materia o categoría, para encontrar rápidamente los recursos físicos que necesito para mi estudio o investigación.

---

## 🛡️ CRITERIOS DE ACEPTACIÓN (ESCENARIOS NARRATIVOS)

### Escenario 1: Validación y Registro Exitoso de una Nueva Obra
* **Dado que:** El Bibliotecario se encuentra en la pantalla de catalogación y el código ISBN ingresado tiene un formato válido (diez o trece dígitos numéricos) y no existe previamente en la base de datos.
* **Cuando:** Ingresa la información completa de la obra (título, autor, categoría, editorial, fecha de publicación y tipo de material) y presiona el botón para confirmar la ingesta.
* **Entonces:** El sistema limpia automáticamente cualquier guion o espacio del ISBN, crea el registro de la obra en el catálogo de forma exitosa y asocia dinámicamente al autor, la categoría y la editorial correspondientes.

### Escenario 2: Rechazo de Catalogación por Formato de ISBN Inválido
* **Dado que:** El Bibliotecario se encuentra en la pantalla de catalogación.
* **Cuando:** Intenta registrar una obra ingresando un código ISBN que contiene letras, caracteres especiales o que no tiene exactamente una longitud de diez o trece dígitos numéricos.
* **Entonces:** El sistema interrumpe el registro, rechaza el almacenamiento físico de la obra y muestra un mensaje de alerta comprensible indicando la infracción de formato con el código de error de negocio ERR_INVALID_ISBN.

### Escenario 3: Rechazo de Catalogación por ISBN Duplicado
* **Dado que:** En el catálogo de la biblioteca ya existe un libro registrado con el código ISBN 9780134494166.
* **Cuando:** El Bibliotecario intenta dar de alta una nueva obra utilizando exactamente ese mismo código ISBN 9780134494166.
* **Entonces:** El sistema comprueba la existencia de la clave en la base de datos, cancela la operación y notifica al operador que la obra ya está registrada, emitiendo el código de error de negocio ERR_DUPLICATE_ISBN.

### Escenario 4: Rechazo de Registro de Ejemplar por Código de Barras Duplicado
* **Dado que:** En el inventario físico ya existe un ejemplar registrado con el código de barras único 9780134494166-C1.
* **Cuando:** El Bibliotecario intenta registrar un nuevo ejemplar físico asociándolo a una obra ingresando ese mismo código de barras.
* **Entonces:** El sistema deniega el alta de la copia física en estantería e informa al operador sobre la duplicidad del activo, emitiendo el código de error de negocio ERR_DUPLICATE_BARCODE.

---

## ⚙️ REQUISITOS DEL SISTEMA

### Requisitos Funcionales
1. **RF-1.1 Gestión de Obras Bibliográficas (CRUD):** El sistema debe permitir a los bibliotecarios registrar, editar y dar de baja obras bibliográficas, almacenando campos clave como: título, ISBN, autor, categoría temática, editorial, fecha de publicación, idioma, país y tipo de material (Libro Físico, Libro Digital, Revista, Tesis, etc.).
2. **RF-1.2 Ingesta y Limpieza de ISBN:** El sistema debe normalizar automáticamente el código ISBN introducido, eliminando espacios y guiones intermedios, soportando formatos válidos de 10 y 13 dígitos numéricos.
3. **RF-1.3 Control de ISBN Único:** Se debe impedir el registro duplicado de libros que compartan un mismo código ISBN.
4. **RF-1.4 Gestión Dinámica de Autoridades:** Durante el registro de una obra, el sistema debe sugerir de forma interactiva nombres de autores, categorías y editoriales preexistentes para evitar redundancias. En caso de no existir, debe permitir crearlos en caliente en la misma interfaz.
5. **RF-1.5 Inventariado y Gestión de Ejemplares Físicos:** El sistema debe permitir asociar múltiples copias físicas (ejemplares) a una misma obra. Cada copia debe registrar un código de barras de activo único, su ubicación en estantería (pasillo/estante) y notas sobre su estado físico de conservación.
6. **RF-1.6 Ciclo de Vida del Ejemplar:** Se debe controlar y actualizar el estado operativo de cada copia física. Los estados permitidos son: Disponible, Prestado, Reservado, En Sala, Mantenimiento o Pérdida.
7. **RF-1.7 Catálogo de Búsqueda Pública OPAC:** El sistema debe proveer una barra de búsqueda para todo el público que permita filtrar y encontrar obras por coincidencia parcial en su título, autor o código ISBN, con pre-filtrado mediante categorías temáticas y recuentos dinámicos en tiempo real de copias físicas disponibles.
8. **RF-1.8 Detalle de Ficha y Copias:** Al seleccionar un libro en el OPAC, el sistema debe desplegar la ficha bibliográfica detallada de la obra y el listado individual de sus copias físicas con su estado de disponibilidad actual.

### Requisitos No Funcionales
1. **RNF-3.1 Rendimiento (Tiempo de Respuesta):** Las búsquedas y filtrados temáticos del catálogo público OPAC deben retornar y renderizar resultados en la interfaz de usuario en menos de un segundo bajo condiciones normales de red.
2. **RNF-3.2 Consistencia Transaccional (ACID):** Todas las operaciones críticas de préstamos, devoluciones, inhabilitaciones y reservas deben ejecutarse de manera atómica para evitar estados inconsistentes (por ejemplo, registrar un préstamo sin cambiar el estado del ejemplar).
3. **RNF-3.3 Concurrencia Optimista:** Para evitar colisiones de estado en accesos concurrentes sobre copias físicas de libros, la base de datos debe validar la consistencia del registro utilizando tokens de concurrencia.
4. **RNF-3.4 Estandarización de Errores (RFC 7807 - Problem Details):** Toda anomalía, rechazo por validación de campos o infracción de regla de negocio debe ser interceptada por un middleware global en el backend y retornada al cliente usando el estándar internacional RFC 7807 (Problem Details) con códigos de error semánticos textuales (ej: ERR_INVALID_ISBN, ERR_DUPLICATE_BARCODE, ERR_USER_SANCTIONED, ERR_COPY_NOT_AVAILABLE).
5. **RNF-3.5 Zona Horaria Unificada (UTC):** Todas las marcas de tiempo en las transacciones del sistema (fechas de salida, retorno de libros y cálculo de vigencia de sanciones) deben registrarse y gestionarse en formato universal UTC a fin de evitar inconsistencias horarias entre el servidor y la base de datos.
6. **RNF-3.6 Mapeo Relacional de Datos (Fluent API):** La base de datos relacional PostgreSQL debe estructurarse estrictamente mediante Fluent API en la Capa de Persistencia, forzando índices de unicidad, no nulabilidad y longitudes máximas de campos directamente en la base de datos, sin contaminar las entidades del dominio con atributos.
7. **RNF-3.7 Seguridad y Control de Acceso (RBAC):** La creación, modificación o eliminación de datos en el inventario físico y catálogo bibliográfico está estrictamente restringida a usuarios autenticados con los roles de Administrador o Bibliotecario. Las llamadas al API se autentican mediante tokens JWT Bearer.
8. **RNF-3.8 Adaptabilidad y Estilo Visual (UI/UX):** La interfaz visual en el navegador debe ser responsiva y consistente en computadoras de escritorio, tablets y móviles. Debe implementarse con componentes en React 19 / Next.js estilizados mediante Tailwind CSS bajo la paleta corporativa empresarial (tonos terracota, arena tostada, azul océano y crema suave).
9. **RNF-3.9 Disponibilidad de Infraestructura:** El sistema debe operar en la nube con balanceo de carga (Backend en Render, Frontend en Vercel, Base de datos en Neon Cloud PostgreSQL serverless) para garantizar un tiempo de actividad del 99%.
10. **RNF-3.10 Soporte de Localización (Internacionalización):** El almacenamiento en base de datos, formularios e interfaz deben soportar caracteres especiales y tildes del idioma español.

