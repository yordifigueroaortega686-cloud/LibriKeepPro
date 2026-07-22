# Lista de Tareas: Catalogación e Ingesta Bibliográfica

Esta es la lista de tareas ordenada de manera lógica y secuencial para la implementación y mantenimiento de los requisitos de catalogación.

---

## 📋 CHECKLIST DE IMPLEMENTACIÓN DE CATALOGACIÓN

### Paso 1: Definición de Reglas y Validaciones del Negocio en el Dominio
- [ ] Implementar la validación y limpieza del campo ISBN en el constructor de la clase de dominio Libro, eliminando guiones y espacios en blanco.
- [ ] Añadir validación de longitud para comprobar que el código ISBN resultante contenga estrictamente diez o trece caracteres numéricos.
- [ ] Programar el lanzamiento de excepciones de dominio específicas ante incumplimiento de formato de ISBN o datos incompletos en la entidad.

### Paso 2: Construcción de la Capa de Aplicación y Servicios de Verificación
- [ ] Desarrollar los validadores de comando mediante FluentValidation para las clases de creación y edición de libros.
- [ ] Implementar la verificación preventiva en el repositorio de base de datos para validar que el código ISBN ingresado no exista previamente en el sistema.
- [ ] Programar los manejadores de comandos de MediatR encargados de coordinar la búsqueda o creación dinámica de autoridades (Autor, Categoría, Editorial) y la posterior instanciación del Libro.

### Paso 3: Configuración de Persistencia Relacional y Base de Datos
- [ ] Declarar las tablas y columnas correspondientes mediante Fluent API en la infraestructura de persistencia, definiendo las longitudes máximas y la no nulabilidad de los campos.
- [ ] Configurar el índice único sobre el campo Isbn en la tabla de Libros para garantizar la integridad a nivel físico de la base de datos.
- [ ] Configurar la restricción de índice único sobre la columna CodigoBarras de la tabla de Ejemplares y establecer el campo Estado como token de concurrencia optimista.

### Paso 4: Desarrollo de Componentes Visuales en el Frontend
- [ ] Diseñar el formulario de catalogación e ingesta avanzada de libros con campos interactivos para título, ISBN, autor, categoría y editorial.
- [ ] Implementar los servicios de llamada HTTP en el cliente web para invocar los endpoints de creación de libros y copias de forma secuencial.
- [ ] Diseñar la cuadrícula de categorías temáticas y la barra de búsqueda central del catálogo OPAC con sus respectivos placeholders de texto.

### Paso 5: Validación del Flujo y Pruebas de Calidad
- [ ] Escribir pruebas unitarias en la suite de pruebas de la Capa de Aplicación para comprobar que el pipeline detenga solicitudes con formatos de ISBN incorrectos.
- [ ] Diseñar pruebas de integración que validen el flujo completo: desde el envío del formulario, la inserción en PostgreSQL y la correcta visualización en el grid del OPAC.
