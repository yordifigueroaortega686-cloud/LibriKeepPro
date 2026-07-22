# Modelo de Datos y Campos: Catalogación e Ingesta Bibliográfica

Este documento detalla rigurosamente la estructura de datos, tipos lógicos y restricciones para cada una de las entidades del módulo de Catalogación en **LibriKeep Pro**.

---

## 📚 ESPECIFICACIÓN CAMPO POR CAMPO DE LAS ENTIDADES

### 1. Entidad `Libro`
Representa una obra o recurso bibliográfico del catálogo general de la biblioteca.

| Atributo / Campo | Tipo de Información | ¿Es Obligatorio? | Significado y Regla de Negocio |
| :--- | :--- | :--- | :--- |
| Identificador de la Obra | Número entero | Sí | Clave primaria autoincremental única asignada por el sistema al registrar el libro. |
| Título del Recurso | Texto | Sí | Nombre oficial y completo de la obra. Tiene un límite de longitud máximo de 250 caracteres. |
| Código ISBN | Texto | Sí | Código de identificación estándar del libro. Debe contener exactamente 10 o 13 caracteres numéricos (sin guiones ni espacios). Es de carácter único en toda la base de datos (restricción de índice único). |
| Identificador del Autor | Número entero | Sí | Clave foránea que asocia la obra con su autor registrado en el sistema. |
| Identificador de la Categoría | Número entero | Sí | Clave foránea que clasifica la obra bajo una temática. |
| Identificador de la Editorial | Número entero | Sí | Clave foránea que define la casa editora de la obra. |
| Fecha de Publicación | Fecha formato ISO | Sí | Fecha en la que la obra fue editada. Se almacena con huso horario UTC en la base de datos. |
| Idioma del Material | Texto | No | Idioma en el cual está escrito el libro (longitud máxima de 50 caracteres). |
| País de Publicación | Texto | No | País donde se realizó la edición (longitud máxima de 100 caracteres). |
| Tipo de Material | Opción de lista | Sí | Clasificación del formato del recurso. Permite elegir entre las opciones fijas de: LibroFisico, LibroDigital, Revista, Tesis u Otro (longitud máxima de 50 caracteres). |

### 2. Entidad `Ejemplar`
Representa una copia o activo físico disponible en la estantería de la biblioteca.

| Atributo / Campo | Tipo de Información | ¿Es Obligatorio? | Significado y Regla de Negocio |
| :--- | :--- | :--- | :--- |
| Identificador del Ejemplar | Número entero | Sí | Clave primaria autoincremental asignada de manera única para cada copia física. |
| Identificador del Libro Relacionado | Número entero | Sí | Clave foránea que conecta la copia con el registro del Libro catalogado. |
| Código de Barras | Texto | Sí | Etiqueta única y física adherida al ejemplar. No se permiten códigos de barra duplicados en el sistema (restricción de índice único). Tiene un límite de 50 caracteres. |
| Estado del Ejemplar | Opción de lista | Sí | Estado del activo físico. Permite elegir entre las opciones de: Disponible, Prestado, Reservado, EnSala, Mantenimiento o Pérdida. Este campo se utiliza como token de concurrencia para evitar préstamos simultáneos. |
| Ubicación en Estantería | Texto | No | Pasillo, columna y estante donde se encuentra el libro (longitud máxima de 200 caracteres). |
| Observaciones del Estado | Texto | No | Notas sobre ralladuras, roturas o el nivel de desgaste físico del libro (longitud máxima de 1000 caracteres). |

### 3. Entidades de Apoyo (Autoridades)

#### Entidad `Autor`
| Atributo / Campo | Tipo de Información | ¿Es Obligatorio? | Significado y Regla de Negocio |
| :--- | :--- | :--- | :--- |
| Identificador del Autor | Número entero | Sí | Clave primaria autoincremental única asignada al autor. |
| Nombre del Autor | Texto | Sí | Nombre y apellidos de la autoridad (longitud máxima de 150 caracteres). |
| Nacionalidad | Texto | No | País de procedencia del escritor (longitud máxima de 100 caracteres). |

#### Entidad `Categoria`
| Atributo / Campo | Tipo de Información | ¿Es Obligatorio? | Significado y Regla de Negocio |
| :--- | :--- | :--- | :--- |
| Identificador de la Categoría | Número entero | Sí | Clave primaria autoincremental de la clasificación. |
| Nombre de la Materia | Texto | Sí | Nombre del área temática (longitud máxima de 100 caracteres). |
| Descripción Temática | Texto | No | Resumen de los temas que engloba la categoría (longitud máxima de 500 caracteres). |

#### Entidad `Editorial`
| Atributo / Campo | Tipo de Información | ¿Es Obligatorio? | Significado y Regla de Negocio |
| :--- | :--- | :--- | :--- |
| Identificador de la Editorial | Número entero | Sí | Clave primaria autoincremental única de la casa editora. |
| Nombre de la Editorial | Texto | Sí | Razón social o nombre comercial de la casa editorial (longitud máxima de 150 caracteres). |
