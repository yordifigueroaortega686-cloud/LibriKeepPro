# Especificación Funcional: Circulación, Préstamos y Devoluciones

Esta especificación describe las reglas de negocio y flujos funcionales del módulo de Circulación en **LibriKeep Pro** desde la perspectiva del usuario y del operador.

---

## 👥 HISTORIAS DE USUARIO

### Historia de Usuario 1: Registro de Salida de Material (Préstamo)
Como Bibliotecario de ventanilla, quiero registrar la salida física de un ejemplar para un lector que proporciona su identificación DNI, para controlar los plazos de retorno y mantener actualizado el inventario en tiempo real.

### Historia de Usuario 2: Retorno y Cierre de Transacción (Devolución)
Como Bibliotecario de ventanilla, quiero registrar la devolución de un ejemplar físico devuelto por un lector, para liberar la copia, actualizar su estado y, si existió retraso, aplicar la inhabilitación automática de la cuenta.

### Historia de Usuario 3: Consulta de Ficha Académica
Como Lector de la institución (Alumno o Docente), quiero visualizar el listado de mis préstamos activos y fecha máxima de entrega, para evitar morosidad y sanciones en mi perfil de usuario.

### Historia de Usuario 4: Consulta de Reporte de Morosidad
Como Bibliotecario o Administrador de la institución, quiero consultar un listado consolidado de todos los lectores con morosidades o bloqueos activos, para realizar el seguimiento, notificaciones de cobro y recuperación de los ejemplares físicos.

---

## 🛡️ CRITERIOS DE ACEPTACIÓN (ESCENARIOS NARRATIVOS)

### Escenario 1: Préstamo Exitoso a Lector Habilitado
* **Dado que:** El Bibliotecario se encuentra en la vista de circulación y el lector es un Alumno con cero préstamos activos en su ficha.
* **Cuando:** Escanea el código de barras de un ejemplar en estado Disponible y confirma la transacción de préstamo.
* **Entonces:** El sistema autoriza la operación, marca el ejemplar como Prestado, genera un registro de préstamo con fecha máxima de devolución a siete días y actualiza la ficha del alumno en la base de datos.

### Escenario 2: Intento de Préstamo de un Ejemplar No Disponible (RN-01)
* **Dado que:** El Bibliotecario está en la vista de circulación y el ejemplar solicitado está registrado actualmente como Prestado.
* **Cuando:** Intenta registrar un nuevo préstamo para dicho ejemplar.
* **Entonces:** El sistema interrumpe la transacción, deniega la solicitud emitiendo el código de error de negocio ERR_COPY_NOT_AVAILABLE e informa que el ejemplar no se encuentra en estantería para préstamo.

### Escenario 3: Bloqueo por Multas o Suspensiones Activas (RN-02)
* **Dado que:** El lector tiene una suspensión de cuenta activa por entrega demorada anterior.
* **Cuando:** El Bibliotecario intenta registrar un nuevo préstamo de libro para el lector.
* **Entonces:** El sistema comprueba el estado de la ficha del lector, bloquea la transacción de despacho y devuelve el código de error de negocio ERR_USER_SANCTIONED indicando la inhabilitación vigente de la cuenta.

### Escenario 4: Superación del Límite de Cuota de Préstamos (RN-03)
* **Dado que:** El lector es un Alumno y ya tiene tres préstamos activos registrados a su nombre.
* **Cuando:** El Bibliotecario intenta registrar un cuarto préstamo simultáneo para el alumno.
* **Entonces:** El sistema comprueba los límites definidos según el tipo de miembro, rechaza el despacho del préstamo y notifica la infracción de cuota excedida mediante el código de error ERR_USER_MAX_LOANS_EXCEEDED.

### Escenario 5: Devolución con Retraso Calendario y Suspensión (RN-04)
* **Dado que:** Un lector tiene un ejemplar en préstamo cuya fecha máxima de entrega venció hace dos días.
* **Cuando:** El Bibliotecario registra la devolución física del ejemplar indicando que se entrega en buen estado.
* **Entonces:** El sistema calcula la diferencia de días calendario, marca el préstamo como Devuelto, actualiza el ejemplar a Disponible y genera una sanción automática de inhabilitación por cuatro días, bloqueando la cuenta del lector.

### Escenario 6: Devolución Prioritaria para Reserva en Cola (RN-05)
* **Dado que:** Un libro tiene reservas activas en el OPAC y no hay copias disponibles en estantes.
* **Cuando:** El Bibliotecario registra la devolución de un ejemplar físico de ese libro en buen estado.
* **Entonces:** El sistema prioriza la cola del OPAC cambiando automáticamente el estado de la copia física a Reservado, y procesa la reserva del primer lector en cola otorgándole prioridad de despacho.

### Escenario 7: Generación del Reporte de Usuarios en Estado de Morosidad
* **Dado que:** El Bibliotecario o Administrador ha accedido a la sección de reportes estadísticos.
* **Cuando:** Solicita cargar la información de morosos de la biblioteca.
* **Entonces:** El sistema consulta la base de datos y le devuelve la lista consolidada de todos los usuarios de la biblioteca que cumplen con al menos una de las siguientes condiciones:
  1. Su cuenta tiene estado BloqueoTemporal o Suspendido.
  2. Tienen algún préstamo activo cuya fecha máxima de devolución es menor que la fecha actual del sistema.
  El sistema presenta por cada usuario su identificador, DNI, nombre completo, correo institucional, la cantidad de préstamos vencidos, el número de inhabilitaciones activas y su estado de ficha actual.

---

## ⚙️ REQUISITOS DEL SISTEMA

### Requisitos Funcionales
1. **Registrar Préstamo:** El sistema debe permitir despachar un ejemplar físico en estado Disponible a un lector identificado por su DNI.
2. **Registrar Devolución:** El sistema debe permitir registrar la entrega física de un ejemplar por su código de barras, actualizando el estado de la copia e informando observaciones.
3. **Bloqueo Automático de Cuenta:** El sistema debe suspender e inhabilitar la cuenta del lector de forma automática si tiene préstamos vencidos no devueltos o sanciones vigentes.
4. **Cálculo de Penalización por Mora:** El sistema debe calcular una inhabilitación equivalente al doble de días calendario de retraso al devolver una copia demorada, cambiando el estado del lector a BloqueoTemporal.
5. **Gestión de Cola de Reservas:** El sistema debe permitir reservar un libro si no hay copias disponibles en estantería. Al devolver un ejemplar, se debe asignar con prioridad en estado Reservado al primer lector en cola.
6. **Límites de Cuota por Membresía:** El sistema debe bloquear préstamos si el lector supera la cuota permitida (Alumno = 3, Docente = 5, Personal Administrativo = 3).
7. **Reportes de Morosidad:** El sistema debe generar un listado de usuarios con inhabilitaciones o préstamos vencidos, detallando la cantidad de atrasos y sanciones activas.

### Requisitos No Funcionales
1. **Consistencia Transaccional:** Las operaciones de préstamo, devolución e inhabilitación deben ejecutarse de manera atómica bajo transacciones ACID en PostgreSQL.
2. **Manejo de Errores Estándar:** Toda respuesta fallida del servidor debe retornar obligatoriamente el formato JSON estándar RFC 7807 (Problem Details) con códigos de error textuales específicos.
3. **Auditoría de Tiempos:** Todas las marcas de tiempo de transacciones (salidas, devoluciones, vigencia de multas) deben almacenarse forzando el formato UTC.
4. **Control de Seguridad en Circulación:** El despacho de préstamos y recepción de devoluciones está restringido estrictamente al rol Administrador o Bibliotecario.


