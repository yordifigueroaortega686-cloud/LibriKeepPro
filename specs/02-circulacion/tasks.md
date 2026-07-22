# Lista de Tareas: Circulación, Préstamos y Devoluciones

Esta es la lista de tareas ordenada de manera lógica y secuencial para la implementación y mantenimiento de los requisitos de circulación y control de préstamos.

---

## 📋 CHECKLIST DE IMPLEMENTACIÓN DE CIRCULACIÓN

### Paso 1: Definición de Reglas y Validaciones en el Dominio
- [ ] Programar en la entidad de dominio Ejemplar la comprobación de estado Disponible para el método de préstamo.
- [ ] Implementar los métodos de verificación en la entidad de dominio Usuario para validar la inhabilitación por sanciones activas, inactividad o deudas de multas.
- [ ] Programar en la entidad Usuario el método que comprueba si los préstamos acumulados activos superan los límites permitidos según el tipo de miembro (Alumno, Docente, Personal Administrativo).
- [ ] Desarrollar en la entidad Préstamo el cálculo de mora y el algoritmo inmutable de inhabilitación equivalente al doble de días de retraso.
- [ ] Configurar la lógica de prioridad de reservas en el método de devolución física de la copia.

### Paso 2: Construcción de Casos de Uso y Servicios de Aplicación
- [ ] Desarrollar los validadores de esquema de comandos de circulación mediante FluentValidation.
- [ ] Programar el manejador del comando de Préstamo con inyección de repositorios y control de transacciones de base de datos atómicas.
- [ ] Programar el manejador de la Devolución con inyección de reservas y persistencia de sanciones automáticas si se detectan retrasos calendario.

### Paso 3: Configuración Relacional de Base de Datos e Infraestructura
- [ ] Declarar las tablas relacionales para Usuarios, Préstamos, Sanciones y Reservas en el proyecto de persistencia de Entity Framework Core.
- [ ] Configurar las claves foráneas correspondientes con sus restricciones de eliminación y el token de concurrencia optimista sobre el estado físico de la copia.
- [ ] Configurar los índices de búsqueda únicos en base de datos para la columna Dni y la columna Email de la tabla de Usuarios.

### Paso 4: Construcción de Pantallas de UI en el Frontend
- [ ] Diseñar el formulario de despacho de préstamos con campos autocompletados para DNI del lector y código de barras del ejemplar físico.
- [ ] Diseñar el panel de devoluciones físicas permitiendo seleccionar el préstamo activo y marcar el estado de entrega en Bueno, Dañado o Pérdida.
- [ ] Implementar los componentes visuales de alertas para mostrar notificaciones críticas si el lector posee multas o préstamos demorados.
- [ ] Conectar los endpoints de la API con los servicios del cliente de React e inyectar el token JWT de seguridad.

### Paso 5: Validación Operativa mediante Pruebas de Calidad
- [ ] Escribir pruebas unitarias que validen de manera aislada que un ejemplar no disponible rechace solicitudes de préstamo.
- [ ] Diseñar escenarios de pruebas de integración para validar el ciclo completo: despacho de libro, simulación de mora, retorno retrasado, inhabilitación del lector en PostgreSQL y comprobación de bloqueo inmediato en el próximo intento de préstamo.
