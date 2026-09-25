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

---

## 📐 Plan de Despliegue, Escalamiento Horizontal y Costos

Esta sección formaliza el dimensionamiento técnico, el modelo de tolerancia a fallas y la estructura de costos del módulo de catalogación en un escenario de carga de producción real, conforme a los principios del Laboratorio 2 de arquitectura de LibriKeep Pro.

### 1. Dimensionamiento Formal de Instancias (Target: 1,200 req/s)

El objetivo de diseño es sostener un flujo de tráfico pico de **1,200 solicitudes por segundo**, con la siguiente distribución de carga:

| Tipo de Tráfico | Proporción | Volumen (req/s) |
|---|---|---|
| Lecturas (catálogo, detalle de obra) | 90 % | 1,080 req/s |
| Escrituras (catalogación, edición) | 10 % | 120 req/s |
| **Total** | **100 %** | **1,200 req/s** |

Cada instancia del servidor de aplicación (.NET 10 en contenedor) opera al **70 % de su capacidad máxima nominal** de 400 req/s para preservar margen de seguridad ante ráfagas de tráfico. Esto equivale a una **capacidad útil de 280 req/s por instancia**.

**Cálculo de instancias necesarias:**

- Instancias en operación normal: ⌈ 1,200 / 280 ⌉ = **5 instancias activas**
- Instancias con tolerancia a falla de un nodo (modelo N − 1): el sistema debe sostener el tráfico completo con una instancia caída, por lo tanto: **6 instancias en el pool** (5 activas + 1 en reserva caliente)

Esta configuración garantiza continuidad de servicio ante la pérdida de cualquier nodo individual sin degradar el throughput ni superar el umbral del 70 % de carga por instancia.

### 2. Carga Efectiva sobre la Base de Datos

La estrategia de caché Cache-Aside sobre Redis (TTL = 60 s) intercepta el **80 % de las solicitudes de lectura** antes de que alcancen a PostgreSQL. El cálculo de operaciones que llegan efectivamente a la base de datos es el siguiente:

| Operación | Volumen bruto | Tasa de acierto caché | Volumen a BD |
|---|---|---|---|
| Lecturas | 1,080 req/s | 80 % (acierto) | 216 op/s |
| Escrituras | 120 req/s | Sin caché (siempre a BD) | 120 op/s |
| **Total efectivo** | — | — | **336 op/s** |

Este modelo reduce la presión sobre PostgreSQL en un 72 % respecto de un esquema sin caché.

**Enrutamiento de carga a base de datos:**

- Las **216 lecturas por segundo** se distribuyen entre las réplicas de lectura de PostgreSQL (consistencia eventual).
- Las **120 escrituras por segundo** se dirigen exclusivamente al nodo primario de PostgreSQL.
- El **autor del cambio** (operador catalogador) siempre lee del nodo primario inmediatamente después de confirmar su transacción, garantizando la regla de consistencia inmediata *read-your-own-writes*.

### 3. Estrategia ante Caída del Servicio de Caché

Si el servicio de Redis queda fuera de línea, el 100 % de las 1,080 lecturas por segundo impactaría directamente a PostgreSQL, pudiendo producir un colapso del nodo primario. Para mitigar este riesgo se aplica obligatoriamente la estrategia **Single-Flight / Mutex**:

Ante un cache miss simultáneo de múltiples solicitudes concurrentes para la misma clave, el sistema permite que **únicamente la primera solicitud** consulte la base de datos. Las solicitudes restantes para esa misma clave quedan suspendidas (en espera) y reutilizan el resultado de la primera consulta una vez que esta se resuelve. Este mecanismo impide el efecto avalancha (*cache stampede*) y limita el número de consultas a BD por clave a exactamente una por ventana de tiempo, independientemente de la concurrencia de entrada.

### 4. Matriz Comparativa de Costos de Despliegue

El sistema LibriKeep Pro está diseñado para operar íntegramente en capa gratuita sin sacrificar la arquitectura de escalabilidad. A continuación se comparan las dos rutas de despliegue formalmente evaluadas:

#### Opción A: Despliegue en Capa Gratuita

| Componente | Proveedor | Costo Mensual |
|---|---|---|
| Frontend (Next.js / React) | Vercel Hobby Plan | $0.00 |
| Backend API (.NET 10) | Render Free / Fly.io Free | $0.00 |
| Base de datos PostgreSQL | Neon Postgres Free (0.5 GB) | $0.00 |
| Caché y sesiones | Upstash Redis Free (10,000 cmd/día) | $0.00 |
| CDN y Object Storage | Cloudflare R2 + CDN Free (10 GB/mes) | $0.00 |
| **Total mensual estimado** | | **$0.00 USD/mes** |

#### Opción B: Nube Empresarial AWS (referencia)

| Componente | Servicio AWS | Costo Mensual Est. |
|---|---|---|
| Cómputo API (6 instancias t3.small) | EC2 Auto Scaling Group | ~$90.00 |
| Base de datos primaria + réplica | Amazon RDS PostgreSQL Multi-AZ | ~$70.00 |
| Caché Redis | Amazon ElastiCache (cache.t3.micro) | ~$25.00 |
| CDN y Object Storage | CloudFront + S3 Standard | ~$20.00 |
| Balanceador de carga | Application Load Balancer | ~$10.00 |
| **Total mensual estimado** | | **~$215.00 USD/mes** |

La Opción A es la ruta obligatoria para el despliegue inicial de LibriKeep Pro, conforme a la Regla VIII-5 de la Constitución del Proyecto. La Opción B se documenta como referencia de valoración para una eventual migración a infraestructura empresarial dedicada, previa aprobación del equipo de arquitectura.

## 4. Plan de Despliegue y Escalamiento Horizontal

### A. Dimensionamiento y Tolerancia a Fallos (N+1)
- **Carga objetivo:** 1,200 solicitudes/segundo (90% lecturas, 10% escrituras)[cite: 8].
- **Capacidad por instancia web:** Capacidad nominal de 400 req/s, limitada operativamente al 70% (280 req/s útiles)[cite: 8].
- **Cálculo de instancias requeridas:**
  - Operación normal: 1,200 / 280 = 4.28 -> 5 instancias[cite: 8].
  - Tolerancia a la caída de 1 nodo (N - 1): Requiere 6 instancias en balanceo horizontal para sostener el 100% del tráfico sin degradación[cite: 8].

### B. Amortiguación de Persistencia
- Acierto de caché esperado: 80% sobre las 1,080 lecturas/s[cite: 8].
- Operaciones reales a PostgreSQL: 216 lecturas fallidas + 120 escrituras = 336 op/s (reduciendo en más del 70% el impacto en base de datos)[cite: 8].
- En caso de caída total de caché, se implementa patrón Single-Flight/Mutex para evitar saturación de conexiones (Cache Stampede)[cite: 8].

### C. Matriz de Costos de Despliegue
- **Entorno de Validación / Demostración (Gratuito - $0.00 USD/mes):**
  - Frontend: Vercel (Edge CDN)
  - Backend API: Render / Fly.io (Docker Linux Alpine)
  - Base de Datos: Neon.tech (PostgreSQL Serverless con PgBouncer)
  - Caché: Upstash Redis (10,000 comandos/día)
  - Multimedia: Cloudflare R2 (Sin costo de egress)
- **Entorno Productivo Empresarial (AWS Cloud - ~$215 USD/mes):** ALB + ECS Fargate + RDS PostgreSQL Multi-AZ + ElastiCache Redis[cite: 8].

