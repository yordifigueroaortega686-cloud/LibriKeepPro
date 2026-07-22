# Planificación de Arquitectura: Circulación, Préstamos y Devoluciones

Este plan detalla el recorrido conceptual de la información y el impacto de los cambios arquitectónicos en la solución para el motor de Circulación de **LibriKeep Pro**.

---

## 🏛️ RECORRIDO POR CAPAS

Las transacciones de préstamos, devoluciones y reservas se procesan a través de las siguientes capas del sistema:

### 1. Capa de Presentación (Presentation.API y Web UI)
El operador de la biblioteca interactúa con el formulario de préstamos (ingresando el DNI del lector y el código de barras) o el selector de devoluciones físicas. La interfaz web envía las peticiones HTTP correspondientes al servidor. El controlador de la API expone los endpoints correspondientes para recibir y autorizar las peticiones delegando las acciones en MediatR mediante el envío de comandos específicos para préstamo o devolución.

### 2. Capa de Aplicación (Core.Application)
Los comandos son validados inicialmente para verificar la existencia de parámetros correctos. Una vez validados, los manejadores correspondientes inician transacciones de base de datos a través de la unidad de trabajo. Los manejadores obtienen las entidades necesarias (Usuario y Ejemplar para préstamos, o Préstamo, Ejemplar y Reservas para devoluciones) desde sus repositorios y llaman a los métodos lógicos de negocio en las entidades.

### 3. Capa de Dominio (Core.Domain)
Toda lógica e invariante de negocio inalterable se ejecuta en los métodos de dominio de las entidades:
* Registro de Préstamo: El método de validación de circulación del Usuario comprueba que no existan retrasos ni suspensiones activas, y verifica los límites máximos permitidos según el tipo de miembro. El Ejemplar ejecuta su método de préstamo modificando su estado a Prestado y validando que estuviera Disponible.
* Devolución Física: La devolución se procesa en la entidad Préstamo, la cual calcula la cantidad de días calendario de diferencia entre la fecha de entrega y la fecha máxima permitida. Si hay retraso, genera una instancia de la entidad Sanción y modifica el perfil del Usuario a BloqueoTemporal. Asimismo, la copia física actualiza su estado a Disponible o Reservado según la existencia de reservas de la obra.

### 4. Capa de Persistencia (Infrastructure.Persistence)
El manejador de la Capa de Aplicación confirma los cambios en la base de datos a través de la unidad de trabajo. Entity Framework Core procesa las modificaciones traduciendo la inhabilitación del lector, el cierre del préstamo, la generación de multas y los cambios de estado físico de la copia a transacciones sql relacionales dirigidas a PostgreSQL. Las configuraciones de Fluent API garantizan los índices de claves y la consistencia de los datos persistidos.

---

## 🏗️ IMPACTO DE CAMBIOS EN LA SOLUCIÓN

La implementación y mantenimiento de las reglas de circulación se localiza en los siguientes componentes:

* Capa de Dominio: Entidades Usuario (Lector), Prestamo, Sancion, Reserva y Ejemplar. Albergan el core lógico de las reglas RN-01 a RN-05.
* Capa de Aplicación: Casos de uso de registro de préstamos, devolución y creación de reservas. Contienen la lógica para orquestar la llamada a repositorios e iniciar las transacciones atómicas.
* Capa de Persistencia: Configuraciones Fluent API relacionales para mapear las llaves foráneas y el token de concurrencia del estado del ejemplar físico.
* Capa de Presentación: Controladores de la API expuestos para ventanilla y perfiles de usuario, y las pantallas del Bibliotecario (Préstamos y Devoluciones) y del Lector en el frontend de React.
