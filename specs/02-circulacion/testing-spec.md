# Especificación de Pruebas: Circulación, Préstamos y Devoluciones

Este documento especifica la estrategia de control de calidad, aseguramiento y validación para el módulo de Circulación en **LibriKeep Pro**.

---

## 🧪 ESCENARIOS DE PRUEBAS UNITARIAS

Las pruebas unitarias validan el comportamiento aislado de las entidades de dominio ante las reglas de negocio RN-01 a RN-05 sin interactuar con infraestructura de red ni persistencia física:

### Caso de Prueba 1: Impedir Préstamo de Ejemplar No Disponible (RN-01)
* **Propósito:** Comprobar que la clase de dominio Ejemplar no permita su préstamo si está en un estado inadecuado.
* **Secuencia de Verificación:**
  1. Instanciar la entidad Ejemplar asignando el estado inicial como Prestado.
  2. Intentar llamar al método Prestar de la clase Ejemplar.
  3. Verificar que el método detenga la ejecución arrojando una excepción de dominio.
  4. Comprobar mediante aserciones que el código de error retornado corresponda a la marca ERR_COPY_NOT_AVAILABLE.

### Caso de Prueba 2: Bloqueo de Circulación por Suspensión Activa (RN-02)
* **Propósito:** Comprobar que un lector con suspensiones vigentes no esté facultado para realizar transacciones de circulación.
* **Secuencia de Verificación:**
  1. Instanciar la entidad Usuario agregando una sanción activa en su colección cuyas fechas de inicio y fin contengan la fecha actual del sistema.
  2. Llamar al método de validación de capacidad de circulación en la entidad Usuario.
  3. Verificar que el método aborte la ejecución y lance una excepción de dominio con el código de error ERR_USER_SANCTIONED.

### Caso de Prueba 3: Validación del Límite de Cuota por Tipo de Miembro (RN-03)
* **Propósito:** Comprobar que el sistema rechace préstamos cuando el lector alcanza el límite máximo de su membresía.
* **Secuencia de Verificación:**
  1. Instanciar un Usuario con tipo de miembro Alumno (cuyo límite es tres préstamos activos).
  2. Invocar al método de validación de límites indicando que el lector posee tres préstamos activos acumulados en base de datos.
  3. Verificar que la validación aborte arrojando la excepción de dominio ERR_USER_MAX_LOANS_EXCEEDED.

### Caso de Prueba 4: Algoritmo Inmutable de Multas por Mora (RN-04)
* **Propósito:** Validar que la devolución tardía genere la sanción y la inhabilitación del perfil del lector de forma exacta.
* **Secuencia de Verificación:**
  1. Instanciar un préstamo activo cuya fecha máxima de entrega venció hace dos días.
  2. Llamar al método de procesamiento de devolución indicando la fecha actual de entrega y el estado Bueno del material.
  3. Verificar que la entidad retorne un objeto Sanción.
  4. Comprobar mediante aserciones que los días totales de suspensión calculados en la sanción sean exactamente cuatro días (dos días de retraso multiplicados por dos).
  5. Comprobar que el estado del Usuario cambie de forma inmediata a BloqueoTemporal.

### Caso de Prueba 5: Prioridad de Reservas en Devoluciones (RN-05)
* **Propósito:** Comprobar que la devolución de un libro con reservas activas priorice la cola de espera.
* **Secuencia de Verificación:**
  1. Invocar al método de devolución física de un ejemplar indicando que el material se entrega en buen estado y que existen reservas pendientes en cola para el libro.
  2. Verificar que la propiedad de estado de la copia física cambie a Reservado en lugar de Disponible.

---

## 🧪 ESCENARIOS DE PRUEBAS DE INTEGRACIÓN

Las pruebas de integración validan el flujo de las transacciones a través de la interfaz visual de la UI, la lógica de la Capa de Aplicación, los repositorios y la persistencia física en PostgreSQL:

### Caso de Prueba 1: Ciclo Completo de Despacho, Inserción y Control de Cuota
* **Propósito:** Comprobar que un préstamo de libro exitoso reduzca la cuota del lector y persista de forma segura.
* **Secuencia de Verificación:**
  1. Simular una petición HTTP POST al endpoint de préstamos con un lector y ejemplar válidos y disponibles.
  2. Verificar en la base de datos de PostgreSQL que se inserte la fila en la tabla de Préstamos y que el estado de la copia física cambie a Prestado.
  3. Intentar de forma sucesiva registrar préstamos para el mismo lector hasta superar su cuota permitida, verificando que la API responda con el código HTTP 400 y el error de negocio ERR_USER_MAX_LOANS_EXCEEDED, rechazando la inserción de las copias excedentes.

### Caso de Prueba 2: Concurrencia Optimista en Préstamo Simultáneo
* **Propósito:** Comprobar que dos bibliotecarios no puedan prestar la misma copia física al mismo tiempo.
* **Secuencia de Verificación:**
  1. Simular dos peticiones HTTP concurrentes dirigidas al endpoint de préstamo intentando emitir la misma copia física.
  2. Verificar que el primer request complete con éxito confirmando el préstamo y modificando la versión del estado de la copia en la base de datos.
  3. Verificar que el segundo request sea rechazado por Entity Framework Core mediante una excepción de concurrencia al detectar el cambio de estado del token, retornando el error de negocio ERR_CONCURRENCY_CONFLICT.
