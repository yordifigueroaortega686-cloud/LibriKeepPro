# Planificación de Arquitectura: Catalogación e Ingesta Bibliográfica

Este plan detalla la estructura técnica conceptual y la organización de los componentes que integran el módulo de Catalogación en **LibriKeep Pro**.

---

## 🏛️ RECORRIDO POR CAPAS

La información en el módulo de catalogación fluye a través de las cuatro capas de la arquitectura limpia de la siguiente manera:

### 1. Capa de Presentación (Presentation.API y Web UI)
El flujo se organiza en la pantalla del administrador o en la consola del catalogador. El formulario de catalogación captura los datos en campos de texto y realiza una solicitud HTTP mediante un método de tipo POST al endpoint del servidor. El controlador de la API REST recibe la solicitud, verifica el rol del operador del sistema mediante su token de seguridad y delega la ejecución enviando un comando inmutable al bus de MediatR.

### 2. Capa de Aplicación (Core.Application)
El comando enviado por el controlador es interceptado por un comportamiento de pipeline de validación. Este pipeline ejecuta los validadores de FluentValidation asociados al comando de creación del libro. Si los datos del formulario no son válidos (por ejemplo, longitud de título excedida o formato de ISBN inválido), se arroja una excepción de validación que detiene el procesamiento. Si la validación es aprobada, el manejador de comandos toma el control, realiza consultas iniciales en los repositorios de persistencia para verificar que no haya ISBNs duplicados y procede a construir la entidad de negocio.

### 3. Capa de Dominio (Core.Domain)
El manejador de la Capa de Aplicación invoca al constructor de la clase Libro definida en la Capa de Dominio. Durante el proceso de instanciación del objeto, la propia clase ejecuta sus invariantes de negocio puras, realizando una limpieza automática del código ISBN (eliminación de espacios y guiones) y comprobando que tenga exactamente diez o trece dígitos. Si falla la comprobación del ISBN, la entidad lanza una excepción de dominio de tipo DomainException con su código correspondiente, impidiendo la creación del objeto.

### 4. Capa de Persistencia (Infrastructure.Persistence)
Si el objeto de dominio Libro es creado satisfactoriamente, el manejador de la Capa de Aplicación lo registra en el repositorio de libros y confirma la transacción a través de la interfaz de la unidad de trabajo. Entity Framework Core traduce la entidad y sus relaciones (Autor, Categoría, Editorial) a comandos relacionales de PostgreSQL. Las configuraciones de Fluent API garantizan que las restricciones de longitud y la clave única sobre el campo del ISBN sean respetadas por la base de datos de Neon Cloud.

---

## 🏗️ IMPACTO DE CAMBIOS EN LA SOLUCIÓN

Para implementar y dar mantenimiento al módulo de catalogación, se identifican los siguientes componentes del proyecto que se ven afectados o que requieren configuración:

* Capa de Dominio: Entidades Libro, Ejemplar, Categoria, Autor y Editorial en el directorio de entidades del dominio. Son clases puras con setters privados que encapsulan la validación de sus respectivos invariantes.
* Capa de Aplicación: Comandos y manejadores de creación y edición de libros en la carpeta de casos de uso de catalogación. Requiere la inyección del repositorio de libros, autores, categorías y editoriales.
* Capa de Persistencia: Configuraciones de Fluent API correspondientes a las tablas de catalogación y autoridades en la clase de configuraciones de persistencia, además de la inclusión de los DbSets correspondientes en el DbContext de LibriKeep.
* Capa de Presentación: Controladores de la API expuestos para la consulta de catálogo y el alta de material bibliográfico, y las pantallas del frontend de catalogación en la interfaz de React.
