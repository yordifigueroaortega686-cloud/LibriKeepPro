# Constitución del Proyecto: LibriKeep Pro

Este documento constituye el manifiesto supremo y acuerdo inquebrantable de ingeniería de software para **LibriKeep Pro**. Establece las directrices y estándares que guían de manera estricta el diseño, desarrollo, persistencia e interfaz de usuario del sistema.

---

## 🌟 PROPÓSITO DEL SISTEMA

LibriKeep Pro es una plataforma empresarial diseñada para resolver el problema de la gestión ineficiente de inventarios bibliográficos y el control manual de circulación de recursos físicos en instituciones educativas. La pérdida de activos, la falta de visibilidad en tiempo real de la disponibilidad de copias, la ausencia de control estricto sobre límites de préstamos y la lentitud para procesar devoluciones y registrar multas, afectan directamente la experiencia del usuario y la productividad de los operadores. 

Este sistema proporciona un motor automatizado que controla en tiempo real el ciclo de vida completo de cada obra y ejemplar físico (desde su catalogación e ingesta hasta su salida, devolución, reserva y posible sanción), garantizando una consistencia absoluta de los datos y una visibilidad transparente para el lector final a través de su interfaz pública de búsqueda.

---

## 🛠️ ARSENAL Y STACK TECNOLÓGICO (EXPLICADO PARA PERSONAS)

El stack tecnológico seleccionado para LibriKeep Pro responde a necesidades estratégicas de velocidad, seguridad, modularidad e interactividad visual:

### Servidor de Aplicación: Backend en .NET versión 10
Justificado por su velocidad de ejecución y robustez inigualable para procesar flujos transaccionales concurrentes. Al emplear C# versión 13 y el framework de arquitectura limpia (Clean Architecture), se asegura un desacoplamiento lógico riguroso que facilita el mantenimiento preventivo y correctivo del software. Las peticiones y operaciones son canalizadas a través de un pipeline automatizado que valida la información de entrada antes de interactuar con el dominio, aislando la lógica de negocio pura de cualquier detalle de base de datos o API externa.

### Motor de Persistencia: Base de Datos en PostgreSQL
Garantiza la integridad relacional de la información y evita pérdidas de datos ante fallos transaccionales. PostgreSQL es un motor robusto y preparado para transacciones con niveles de aislamiento confiables. La integración se realiza a través de Entity Framework Core versión 10.0.9 utilizando el proveedor Npgsql, configurando los campos y restricciones (tales como llaves primarias, llaves foráneas, índices de unicidad y conversiones de datos) mediante código declarativo de Fluent API en la capa de persistencia. Esto impide que los modelos de base de datos contaminen las clases puras del dominio bibliotecario. Además, se obliga a que todas las fechas se registren en formato universal UTC para prevenir desajustes de horarios entre la aplicación y la base de datos física.

### Interfaz del Lector y Operaciones: Frontend en React versión 19 / Next.js con Tailwind CSS
Proporciona una interfaz visual ágil, moderna, intuitiva y adaptable a cualquier dispositivo móvil o de escritorio. La reactividad de los componentes de React asegura actualizaciones de pantalla instantáneas (por ejemplo, al cambiar filtros de búsqueda o seleccionar una obra para inspeccionar ejemplares físicos en estantería), reduciendo la necesidad de recargar la página. Tailwind CSS permite un control visual preciso basado en estilos utilitarios atómicos, implementando la paleta de colores corporativa (tonos terracota, arena tostada, azul océano y crema suave) que facilita una lectura confortable y reduce la fatiga visual de los operadores que trabajan jornadas completas frente a la pantalla.

### Infraestructura de Alojamiento y Nube
* Servidor de Base de Datos: Neon Cloud, que ofrece PostgreSQL Serverless de alta disponibilidad y escalabilidad automática.
* Servidor de API REST: Render, que hospeda los contenedores del backend en producción con integraciones automáticas desde repositorios controlados de código.
* Cliente Frontend: Vercel, proporcionando una distribución rápida de contenido a nivel global y latencias mínimas para el usuario final.

---

## 🛡️ REGLAS DE CALIDAD E INTEGRIDAD DEL SISTEMA

Para garantizar la estabilidad operacional del ecosistema, el desarrollo de LibriKeep Pro está sujeto a las siguientes reglas constitutivas e inviolables de calidad:

### Tolerancia Cero a Errores Ambiguos (Estándar RFC 7807)
Está estrictamente prohibido emitir mensajes de error genéricos del tipo "Ocurrió un error inesperado" o códigos de error numéricos sin significado de negocio. Cualquier anomalía detectada por el servidor, ya sea de validación, violación de invariantes o excepciones del sistema, debe ser interceptada por un middleware global y retornada al cliente en formato estandarizado RFC 7807 (Problem Details). Este formato debe incluir obligatoriamente el código textual descriptivo de la infracción (ej. ERR_USER_SANCTIONED, ERR_COPY_NOT_AVAILABLE) y un mensaje con lenguaje amigable y claro para que el lector u operador final entienda exactamente el motivo de la falla y cómo regularizar su estado.

### Validación Previa Incondicional
Ninguna petición que mute la información de la base de datos (creación, edición o eliminación) puede ser procesada por los manejadores de negocio si no ha superado previamente un filtro de esquema y validez defensiva. Toda solicitud se valida de manera obligatoria en la capa de Aplicación mediante reglas explícitas de formato (como la longitud permitida de caracteres, el tipo de dato recibido y patrones numéricos). Las reglas de persistencia, como la unicidad del ISBN en obras o del código de barras en copias físicas, se comprueban inmediatamente en los repositorios de datos antes de confirmar cualquier guardado, previniendo la corrupción de la base de datos.

## ⚙️ Principio VIII: Arquitectura de Escalamiento, Resiliencia y Eficiencia de Costos

Este principio establece las restricciones de diseño arquitectónico que garantizan la escalabilidad horizontal, la tolerancia a fallas y la viabilidad económica del sistema en entornos de producción real.

### Regla VIII-1: Desacoplamiento de Multimedia vía CDN y Object Storage

La capa de aplicación web **nunca** servirá activos binarios pesados ni contenido multimedia de forma directa desde los servidores de aplicación. Todo archivo estático —portadas de obras, documentos adjuntos, recursos audiovisuales— se almacena obligatoriamente en **Cloudflare R2** (almacenamiento de objetos compatible con la API de S3) y se distribuye globalmente a través de la red de distribución de contenido **Cloudflare CDN**. Esta regla es inviolable: su incumplimiento degrada el rendimiento de la API REST y compromete la capacidad de escalar horizontalmente.

### Regla VIII-2: Capa Web Sin Estado (Stateless)

Todas las instancias del servidor de aplicación en **.NET 10** deben operar en modo completamente apátrida. Está prohibido almacenar estado de sesión de usuario, tokens de autenticación o datos de contexto en la memoria local de proceso (in-process). Cualquier estado compartido entre réplicas —incluyendo tokens de sesión JWT, preferencias de operador y marcadores de operaciones en curso— debe residir de forma exclusiva en **Redis externo**. Esta restricción es el prerrequisito arquitectónico que habilita el escalamiento horizontal mediante la adición de instancias adicionales detrás del balanceador de carga sin coordinación entre nodos.

### Regla VIII-3: Estrategia de Caché Cache-Aside con TTL de 60 Segundos

Las consultas de lectura pública de alta frecuencia —catálogo bibliográfico, detalle de obras, disponibilidad de ejemplares— implementan obligatoriamente el patrón **Cache-Aside** sobre Redis con un tiempo de vida (TTL) de **60 segundos**. El flujo de lectura es el siguiente: la aplicación consulta Redis primero; si existe la entrada (cache hit), la retorna directamente sin tocar la base de datos; si no existe (cache miss), consulta PostgreSQL, almacena el resultado en Redis con el TTL establecido y retorna la respuesta al cliente.

Ante una caída total del servicio de caché, se aplica obligatoriamente la estrategia **Single-Flight / Mutex** para evitar el efecto avalancha (cache stampede): únicamente una solicitud concurrente por clave puede consultar la base de datos simultáneamente; las restantes aguardan y reutilizan el resultado de la primera consulta resuelta.

### Regla VIII-4: Persistencia con Separación de Lecturas y Escrituras

El sistema emplea un modelo de persistencia híbrida con separación explícita de nodos:

- **Nodo Primario de PostgreSQL:** Receptor exclusivo de todas las operaciones de mutación (INSERT, UPDATE, DELETE) y de las lecturas del **autor del cambio** (operador catalogador que acaba de confirmar una transacción). Esta regla garantiza consistencia de lectura inmediata (read-your-own-writes) para el operador que genera el cambio, eliminando sorpresas de datos obsoletos.
- **Réplicas de Lectura de PostgreSQL:** Destino de todas las consultas del catálogo público con tolerancia a consistencia eventual. Estas réplicas absorben el volumen masivo de lecturas de usuarios finales, protegiendo al nodo primario de la carga de consultas de solo lectura.

### Regla VIII-5: Viabilidad de Despliegue en Capa Gratuita ($0 USD)

El sistema debe ser íntegramente desplegable con un presupuesto de **$0.00 USD/mes** utilizando exclusivamente las capas gratuitas de los siguientes proveedores certificados para LibriKeep Pro:

| Componente | Proveedor Certificado | Capa Gratuita |
|---|---|---|
| Frontend (Next.js/React) | Vercel | Hobby Plan |
| Backend API (.NET 10) | Render o Fly.io | Free Tier |
| Base de datos PostgreSQL | Neon Postgres | Free Tier (0.5 GB) |
| Caché Redis | Upstash Redis | Free Tier (10,000 cmd/día) |
| CDN y Object Storage | Cloudflare R2 / CDN | Free Tier (10 GB/mes) |

Ninguna decisión de diseño o de selección de proveedor puede imponer un costo recurrente que impida el despliegue completo del sistema dentro de este presupuesto, salvo decisión explícita y documentada del equipo de arquitectura.