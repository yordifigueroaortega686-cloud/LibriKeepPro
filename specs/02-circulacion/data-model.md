# Modelo de Datos y Campos: Circulación, Préstamos y Devoluciones

Este documento detalla rigurosamente la estructura de datos, tipos lógicos y restricciones para cada una de las entidades del módulo de Circulación en **LibriKeep Pro**.

---

## 📚 ESPECIFICACIÓN CAMPO POR CAMPO DE LAS ENTIDADES

### 1. Entidad `Usuario` (Lector o Personal)
Representa la ficha del lector y del personal autorizado de la biblioteca.

| Atributo / Campo | Tipo de Información | ¿Es Obligatorio? | Significado y Regla de Negocio |
| :--- | :--- | :--- | :--- |
| Identificador de Usuario | Número entero | Sí | Clave primaria autoincremental única asignada al usuario. |
| Número de DNI | Texto | Sí | Documento de identidad del usuario. Debe ser único en el sistema (restricción de índice único). Longitud máxima de 20 caracteres. |
| Nombre Completo | Texto | Sí | Nombres y apellidos completos del usuario (longitud máxima de 200 caracteres). |
| Correo Electrónico | Texto | Sí | Correo institucional único. No se permiten registros duplicados (restricción de índice único). Longitud máxima de 150 caracteres. |
| Contraseña Hash | Texto | Sí | Hash seguro del password de acceso (longitud máxima de 500 caracteres). |
| Teléfono del Usuario | Texto | No | Número telefónico de contacto (longitud máxima de 20 caracteres). |
| Tipo de Miembro | Opción de lista | Sí | Perfil académico. Permite las opciones fijas: Alumno, Docente, PersonalAdministrativo, Bibliotecario o Externo. Se utiliza para validar las cuotas de préstamos permitidos en la regla de negocio. Longitud máxima de 50 caracteres. |
| Rol de Acceso | Opción de lista | Sí | Nivel de seguridad del usuario. Permite las opciones: Lector, Bibliotecario o Administrador (longitud máxima de 50 caracteres). |
| Estado de Ficha | Opción de lista | Sí | Estado del lector para circulación. Permite elegir entre las opciones de: Activo, BloqueoTemporal, Suspendido o Inactivo. Afecta directamente la facultad de solicitar libros físicos (regla de negocio de bloqueo). Longitud máxima de 50 caracteres. |

### 2. Entidad `Prestamo`
Registra la salida física de un activo y controla su devolución.

| Atributo / Campo | Tipo de Información | ¿Es Obligatorio? | Significado y Regla de Negocio |
| :--- | :--- | :--- | :--- |
| Identificador de Préstamo | Número entero | Sí | Clave primaria autoincremental de la transacción de préstamo. |
| Identificador de Usuario | Número entero | Sí | Clave foránea que asocia la transacción con el lector. |
| Identificador de Ejemplar | Número entero | Sí | Clave foránea que conecta la transacción con la copia física emitida. |
| Fecha de Salida | Fecha formato ISO | Sí | Fecha y hora en la que se realiza la entrega física (UTC). |
| Fecha Máxima de Devolución | Fecha formato ISO | Sí | Fecha límite para el retorno del ejemplar sin sanción (UTC). |
| Fecha de Devolución Efectiva | Fecha formato ISO | No | Fecha y hora real en la que el ejemplar es retornado (UTC). |
| Estado del Préstamo | Opción de lista | Sí | Estado de la transacción. Permite las opciones: Activo, Devuelto o Demorado (longitud máxima de 50 caracteres). |

### 3. Entidad `Sancion`
Controla las inhabilitaciones de cuentas de usuarios por retrasos.

| Atributo / Campo | Tipo de Información | ¿Es Obligatorio? | Significado y Regla de Negocio |
| :--- | :--- | :--- | :--- |
| Identificador de Sanción | Número entero | Sí | Clave primaria autoincremental de la multa de circulación. |
| Identificador de Lector | Número entero | Sí | Clave foránea que asocia el bloqueo con la ficha del usuario. |
| Identificador de Préstamo | Número entero | No | Clave foránea que enlaza la sanción al préstamo que originó el retraso. Si se elimina el préstamo, se mantiene nulo en base de datos. |
| Fecha de Inicio | Fecha formato ISO | Sí | Fecha y hora en la que comienza la inhabilitación de cuenta (UTC). |
| Fecha de Conclusión | Fecha formato ISO | Sí | Fecha límite en la que expira la inhabilitación (UTC). |
| Días de Suspensión | Número Entero | Sí | Cantidad total de días calculados de penalización (equivalente al doble de los días calendario de demora). Debe ser mayor o igual a uno. |
| Estado de la Multa | Opción de lista | Sí | Estado operativo del bloqueo. Permite las opciones: Activa, Expirada o Levantada (longitud máxima de 50 caracteres). |
