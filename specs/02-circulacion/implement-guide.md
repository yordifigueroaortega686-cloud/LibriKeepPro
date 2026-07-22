# Guía de Implementación y Zonas UI: Circulación, Préstamos y Devoluciones

Esta guía detalla el comportamiento operativo de los servicios del servidor y el diseño visual de las pantallas asociadas al módulo de Circulación de **LibriKeep Pro**.

---

## ⚡ COMPORTAMIENTO DEL SERVIDOR

El servidor de LibriKeep Pro interactúa con los operadores y lectores procesando las transacciones físicas con los siguientes códigos y respuestas:

### Salida Exitosa (Préstamo)
Al registrar satisfactoriamente un préstamo, el servidor responde con el código de estado HTTP 201 (Creado). Retorna en el cuerpo de la respuesta un objeto estructurado que contiene el identificador único del préstamo, el identificador y nombre completo del lector, el identificador, código de barras y título del libro, la fecha de salida, la fecha máxima de entrega establecida y el estado de la transacción fijado como Activo.

### Retorno Exitoso (Devolución)
Al procesar la devolución de un ejemplar, el servidor responde con un código de estado HTTP 200 (Aceptado). Retorna un objeto detallando el identificador del préstamo, la fecha y hora de devolución efectiva, la cantidad de días calendario de retraso calculados, un indicador de penalización aplicada, el detalle de la suspensión si aplica y el nuevo estado del inventario físico del ejemplar (Disponible, Reservado, Mantenimiento o Pérdida).

### Respuestas de Infracción y Error
Si la transacción infringe alguna regla del dominio (como lector inhabilitado o ejemplar prestado), el servidor deniega la operación y devuelve un código de estado HTTP 400 (Petición Incorrecta). La respuesta adopta el formato estándar RFC 7807, incluyendo el código textual exacto de la regla violada (tales como ERR_COPY_NOT_AVAILABLE, ERR_USER_SANCTIONED, ERR_USER_OVERDUE_LOANS o ERR_USER_MAX_LOANS_EXCEEDED) y un mensaje amigable detallando los motivos del rechazo. Ante intentos de realizar operaciones sin las credenciales del personal de biblioteca, se emite una respuesta HTTP 403 con el código de error ERR_FORBIDDEN.

---

## 🖥️ RECORRIDO POR ZONAS VISUALES DE LA PANTALLA (MOSTRADOR DE CIRCULACIÓN)

Las pantallas del Bibliotecario (Préstamos y Devoluciones) se estructuran visualmente a través de las siguientes cinco zonas cardinales:

### Zona 1: Barra Superior de Navegación (Header)
* **Descripción Visual:** Cabecera fija horizontal de la aplicación. Utiliza el color azul océano apagado translúcido con desenfoque de fondo y borde inferior color arena.
* **Contenido y Elementos:** Muestra el isologo de la biblioteca y el título del sistema a la izquierda. A la derecha, habilita de forma condicional para el personal de biblioteca las pestañas de navegación "Préstamos" y "Devoluciones". Al extremo derecho, expone el nombre completo del operador, su rol "BIBLIOTECARIO" y el botón de cierre de sesión.

### Zona 2: Formulario de Despacho / Selección de Préstamo
* **Descripción Visual:** Panel de control principal ubicado en la sección central izquierda de la pantalla, con fondo crema suave y contornos color arena.
* **Contenido y Elementos:**
  * En la pantalla de Préstamos: Expone un campo de texto interactivo con la sugerencia de placeholder exacta: "Ingrese DNI o nombre del lector" que filtra y autocompleta usuarios, un campo de texto con la sugerencia: "Scan barcode o ingrese código" que filtra ejemplares físicos disponibles, un selector de fecha límite de devolución y el botón "Confirmar Préstamo" en fondo morado.
  * En la pantalla de Devoluciones: Expone un menú desplegable interactivo con la sugerencia: "-- Elige un préstamo activo --" que lista los préstamos vigentes de la biblioteca de forma ordenada.

### Zona 3: Métricas de Circulación y Resultados de Devolución
* **Descripción Visual:** Panel de resultados e indicadores en la sección inferior central.
* **Contenido y Elementos:**
  * En la pantalla de Préstamos: Muestra una tarjeta informativa detallando las métricas rápidas del mostrador (como la cantidad de préstamos activos despachados hoy y el total de lectores con morosidad).
  * En la pantalla de Devoluciones: Muestra un grupo de opciones circulares (radio buttons) para seleccionar el "Estado Físico de la Obra" (Bueno, Dañado, Pérdida), un área de observaciones y el botón "Registrar Retorno de Obra" en fondo rojo. Al completarse con éxito, se oculta el formulario y muestra una notificación detallando si se generó una suspensión por morosidad (mostrando días calendario de retraso y fecha límite de bloqueo).

### Zona 4: Panel Lateral de Ficha de Auditoría
* **Descripción Visual:** Columna vertical derecha.
* **Contenido y Elementos:**
  * En la pantalla de Préstamos: Muestra el listado de los últimos préstamos activos de la biblioteca con insignias de estado (verde para Activo y rojo parpadeante para Demorado).
  * En la pantalla de Devoluciones: Al seleccionar un préstamo activo en la Zona 2, despliega automáticamente la ficha verídica del lector (nombre completo y número de DNI) y los detalles del libro y fecha de vencimiento acordada para su rápida validación antes de confirmar la recepción física.

### Zona 5: Pie de Página (Footer)
* **Descripción Visual:** Barra informativa horizontal en el límite inferior de la pantalla de color crema translúcido.
* **Contenido y Elementos:** Muestra los créditos de propiedad intelectual del sistema y del framework Spec Kit.
