# LibriKeep Pro: Documentación Técnica Maestra y Especificación de Arquitectura

**Documento Principal (Master Specification Index)**  
**Proyecto:** LibriKeep Pro — Sistema Empresarial de Gestión Bibliotecaria, Control de Activos y Circulación Bajo Contrato  
**Ubicación:** `X:\LibriKeepProject\LibriKeepPro.md`

---

## 📐 1. Visión General del Sistema y Metodología Spec-Driven Development (SDD)

### 1.1 Propósito
**LibriKeep Pro** es una solución empresarial de software diseñada para automatizar y garantizar la integridad de las operaciones de inventario bibliográfico, gestión de autoridades (Autores, Categorías y Editoriales), préstamos atómicos con control de concurrencia y la aplicación inmutable de penalizaciones por morosidad.

### 1.2 Principios Fundamentales SDD
El desarrollo de la plataforma se gobierna estrictamente bajo la metodología **Spec-Driven Development (SDD)**:
- **El Contrato OpenAPI 3.0 como Fuente Inmutable de Verdad:** El archivo `openapi.yaml` y los documentos especificados en `docs/specs/` definen de forma previa los contratos REST, payloads JSON, reglas de validación y modelos relacionales antes de escribir código de infraestructura.
- **Generación Automática de Artefactos (Codegen):** Las interfaces C# del backend (`LibriKeepControllers.g.cs`) y los tipos TypeScript para el frontend se autogeneran desde OpenAPI utilizando NSwag.
- **Estandarización de Respuestas de Error (RFC 7807):** Toda excepción o rechazo por regla de negocio responde bajo el formato estándar **Problem Details (RFC 7807)** con un código interno unificado (`ERR_*`).

### 1.3 Matriz de Trazabilidad de Requerimientos y Reglas de Negocio

| Código | Requerimiento / Regla de Negocio | Endpoint SDD / Contrato | Handler Backend (.NET 10) | Componente Frontend (React 19) |
| :--- | :--- | :--- | :--- | :--- |
| **RF-1.1** | Gestión CRUD de Obras Bibliográficas | `POST/GET /api/catalogacion/libros` | `CrearLibroCommandHandler` | `CatalogingView` / Formulario |
| **RF-1.2** | Gestión de Autoridades | `GET /api/autores`, `/api/categorias` | `ObtenerAutoresQueryHandler` | `CatalogingView` / Dropdowns |
| **RF-1.3** | Validación de ISBN Único | `POST /api/catalogacion/libros` | `CrearLibroCommandValidator` | `CatalogingView` / Input ISBN |
| **RN-01** | Ejemplar en Estado Disponible | `POST /api/circulacion/prestamos` | `RegistrarPrestamoCommandHandler` | `CirculationView` / Validador |
| **RN-02** | Bloqueo por Morosidad o Sanción | `POST /api/circulacion/prestamos` | `RegistrarPrestamoCommandValidator` | `ReaderDashboard` / Banner Rose |
| **RN-03** | Cuota Máxima por Perfil | `POST /api/circulacion/prestamos` | `RegistrarPrestamoCommandHandler` | `CirculationView` / Contador |
| **RN-04** | Algoritmo Penalización ($\text{Dias} \times 2$) | `POST /api/circulacion/devoluciones`| `ProcesarDevolucionCommandHandler` | `CirculationView` / Modal Multa |
| **RN-05** | Prioridad en Cola de Reservas | `POST /api/circulacion/devoluciones`| `ProcesarDevolucionCommandHandler` | `OPACView` / Badge Reservado |

---

## 🛠️ 2. Stack Tecnológico Real de Producción (Extraído del Proyecto)

### 2.1 Backend API (.NET 10.0 / C# 13)

| Componente / Paquete NuGet | Versión Exacta | Capa donde Aplica | Función y Propósito |
| :--- | :---: | :--- | :--- |
| `Microsoft.NET.Sdk.Web` | .NET 10.0 | `Presentation.API` | Framework host ejecutable para Web API REST |
| `MediatR` | 14.2.0 | `Core.Application` | Mediador CQRS desacoplado para Comandos y Consultas |
| `FluentValidation` | 11.9.2 | `Core.Application` | Validaciones defensivas en la canalización MediatR |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.3 | `Infrastructure.Persistence` | Proveedor relacional EF Core 10 para PostgreSQL |
| `Microsoft.EntityFrameworkCore` | 10.0.9 | `Infrastructure.Persistence` | ORM de persistencia y mapeos Fluent API |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.9 | `Presentation.API` | Herramientas de generación de migraciones |
| `Microsoft.EntityFrameworkCore.InMemory` | 10.0.9 | `Infrastructure.Persistence` | Base de datos en memoria para suite de testing |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.9 | `Presentation.API` | Autenticación basada en tokens JWT firmados |
| `Microsoft.AspNetCore.OpenApi` | 10.0.8 | `Presentation.API` | Especificación de esquema OpenAPI nativo |
| `NSwag.AspNetCore` | 14.7.1 | `Presentation.API` | Generación de Swagger UI y controladores C# |

### 2.2 Frontend Web (React 19 / TypeScript 5.2 / Vite 5.3)

| Componente / Paquete npm | Versión Exacta | Ubicación en `LibriKeep.Web` | Función y Propósito |
| :--- | :---: | :--- | :--- |
| `react` | 19.0.0 | `src/` | Librería principal de componentes reactivos UI |
| `react-dom` | 19.0.0 | `src/` | Renderizador React para el árbol DOM |
| `typescript` | 5.2.2 | `root` | Lenguaje fuertemente tipado para desarrollo |
| `vite` | 5.3.1 | `root` | Servidor de desarrollo y empaquetador de assets |
| `tailwindcss` | 3.4.4 | `src/index.css` | Framework CSS utilitario para diseño visual |
| `lucide-react` | 0.400.0 | `components/` | Librería de íconos vectoriales UI |
| `jspdf` | 4.2.1 | `components/admin/` | Generación del motor cliente de documentos PDF |
| `jspdf-autotable` | 5.0.8 | `components/admin/` | Formateo automático de tablas en reportes PDF |
| `@playwright/test` | 1.61.1 | `tests/` | Suite automatizada de pruebas End-to-End |

---

## 🏛️ 3. Mapa de Arquitectura de la Solución

```
+------------------------------------------------------------------------------------+
|                            SOLUCIÓN LIBRIKEEP PRO                                  |
+------------------------------------------------------------------------------------+
|                                                                                    |
|   FRONTEND WEB (LibriKeep.Web - React 19 + Vite 5.3 + Tailwind 3.4)                 |
|   └── Single Page Application (SPA) con Modo Oscuro Empresarial                    |
|                                                                                    |
|                                     │                                              |
|                                     │ HTTPS / JSON REST API (JWT Bearer)           |
|                                     ▼                                              |
|                                                                                    |
|   PRESENTACIÓN API (LibriKeep.Presentation.API - .NET 10.0)                        |
|   ├── Controladores Autogenerados NSwag + Partials                                 |
|   └── Middleware Global RFC 7807 (Problem Details)                                 |
|                                                                                    |
|                                     │                                              |
|                                     │ CQRS Mediated Requests (MediatR 14.2)        |
|                                     ▼                                              |
|                                                                                    |
|   CAPA DE APLICACIÓN (LibriKeep.Core.Application - .NET 10.0)                      |
|   ├── Handlers CQRS (Commands & Queries)                                           |
|   ├── ValidationBehavior (FluentValidation 11.9)                                   |
|   └── Repositorios Abstractos & DTOs                                               |
|                                                                                    |
|                                     │                                              |
|                                     │ Reglas de Negocio Puras                      |
|                                     ▼                                              |
|                                                                                    |
|   DOMINIO CORE (LibriKeep.Core.Domain - .NET 10.0 Pure C#)                         |
|   ├── Entidades (Libro, Ejemplar, Prestamo, Sancion, Usuario)                      |
|   └── Enumerados (EstadoEjemplar, EstadoPrestamo, TipoMiembro, Rol)                |
|                                                                                    |
|                                     ▲                                              |
|                                     │ Inyección Scoped                             |
|                                     │                                              |
|   PERSISTENCIA (LibriKeep.Infrastructure.Persistence - .NET 10.0)                  |
|   ├── LibriKeepDbContext (EF Core 10.0.9 + Npgsql 10.0.3)                          |
|   └── Neon Cloud PostgreSQL 16 (Transacciones ACID)                                |
|                                                                                    |
+------------------------------------------------------------------------------------+
```

---

## 📚 4. Índice General de Especificaciones Detalladas (`docs/specs/`)

Consulta la especificación técnica desglosada campo por campo y zona por zona en los siguientes archivos:

1. ⚙️ **[01. Arquitectura Backend & PostgreSQL](file:///x:/LibriKeepProject/docs/specs/01-arquitectura-backend.md)**  
   *Variables de entorno, pipeline MediatR, mapa completo de errores RFC 7807 (`ERR_*`) y paquetes NuGet.*

2. 📦 **[02. Backend Catalogación](file:///x:/LibriKeepProject/docs/specs/02-backend-catalogacion.md)**  
   *Especificación tabular campo por campo de entidades (`Libro`, `Ejemplar`, `Autor`, `Categoria`, `Editorial`), Fluent API, DTOs de Request/Response y códigos RFC 7807.*

3. 🔄 **[03. Backend Circulación](file:///x:/LibriKeepProject/docs/specs/03-backend-circulacion.md)**  
   *Tablas detalladas de `Prestamo`, `Sancion`, `Usuario`, ejecución inmutable de **RN-01 a RN-05**, DTOs y respuestas RFC 7807.*

4. 🎨 **[04. Frontend UI y Componentes](file:///x:/LibriKeepProject/docs/specs/04-frontend-ui-componentes.md)**  
   *Desglose por Zonas Cardinales numeradas (Zona 1 a N) para las 4 Vistas (OPAC, Lector, Bibliotecario, Admin) con elementos, tipos TypeScript, Tailwind y endpoints API.*

5. 🚀 **[05. Despliegue y CI/CD](file:///x:/LibriKeepProject/docs/specs/05-despliegue-ci-cd.md)**  
   *Tabla completa de variables de entorno para Render, Vercel y Neon DB, bypass de caché, directivas CORS y health check.*
