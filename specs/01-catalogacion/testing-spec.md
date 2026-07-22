# Especificación de Pruebas: Catalogación e Ingesta Bibliográfica

Este documento especifica la estrategia de control de calidad, aseguramiento y validación para el módulo de Catalogación en **LibriKeep Pro**.

---

## 🧪 ESCENARIOS DE PRUEBAS UNITARIAS

Las pruebas unitarias validan el comportamiento aislado de las entidades de dominio y los validadores de comando sin interactuar con infraestructura de red ni persistencia física:

### Caso de Prueba 1: Validación y Normalización Exitosa del ISBN
* **Propósito:** Comprobar que la entidad Libro normalice y acepte correctamente un código ISBN válido.
* **Secuencia de Verificación:**
  1. Instanciar la entidad Libro suministrando en el parámetro del ISBN un valor con guiones y espacios (por ejemplo, "978-0-13-449416-6").
  2. Verificar que el constructor limpie internamente los guiones y espacios.
  3. Comprobar mediante aserciones que la propiedad Isbn almacene el valor normalizado ("9780134494166") y que no se arroje ninguna excepción de dominio.

### Caso de Prueba 2: Rechazo de ISBN con Longitud Incorrecta
* **Propósito:** Validar que la entidad impida el registro de obras con códigos ISBN con longitud de caracteres inválida.
* **Secuencia de Verificación:**
  1. Intentar construir la entidad Libro ingresando un ISBN con longitud menor a la permitida (por ejemplo, un código de 8 dígitos).
  2. Comprobar que la instanciación aborte inmediatamente arrojando una excepción de dominio.
  3. Verificar mediante aserciones que el mensaje de error o código contenga la marca de infracción de formato de ISBN (código de error de negocio ERR_INVALID_ISBN).

### Caso de Prueba 3: Rechazo de ISBN con Caracteres Alfabéticos
* **Propósito:** Validar que el validador del comando detenga solicitudes que contengan caracteres no numéricos.
* **Secuencia de Verificación:**
  1. Ejecutar el validador del comando de creación de libros suministrando un código ISBN que contiene letras.
  2. Verificar que la validación falle y que el sistema interrumpa la canalización de MediatR arrojando una excepción de validación con el detalle del campo afectado.

---

## 🧪 ESCENARIOS DE PRUEBAS DE INTEGRACIÓN

Las pruebas de integración validan la consistencia de los flujos de información a través de las capas de negocio, base de datos y la interfaz de usuario:

### Caso de Prueba 1: Flujo Completo de Ingesta y Consulta en Catálogo
* **Propósito:** Validar la persistencia de una obra y sus copias en la base de datos física y su reflejo en la interfaz.
* **Secuencia de Verificación:**
  1. Simular una petición HTTP POST al endpoint de creación de libro con una estructura de datos válida.
  2. Comprobar que la base de datos inserte el registro de Libro, resolviendo de forma dinámica el autor, la categoría y la editorial, y confirmando la transacción.
  3. Realizar una petición GET al endpoint del catálogo OPAC y verificar que la colección devuelta incluya la obra catalogada mostrando la cantidad correcta de ejemplares asociados en estado Disponible.

### Caso de Prueba 2: Prevención de Registro de Obras con ISBN Duplicado
* **Propósito:** Verificar que la base de datos mantenga la unicidad de las obras registradas.
* **Secuencia de Verificación:**
  1. Insertar una obra en la base de datos utilizando el ISBN 9780134494100 de forma exitosa.
  2. Intentar una segunda inserción de libro utilizando la misma clave de ISBN 9780134494100.
  3. Comprobar que el repositorio capture la violación de clave única o el índice único de base de datos de PostgreSQL y aborte el guardado.
  4. Verificar que la API retorne una respuesta estructurada con código de estado HTTP 400 y código de error de negocio ERR_DUPLICATE_ISBN.
