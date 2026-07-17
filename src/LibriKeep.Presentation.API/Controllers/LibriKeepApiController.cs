using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediatR;
using LibriKeep.Core.Application.Circulacion.Commands.RegistrarPrestamo;
using LibriKeep.Core.Application.Circulacion.Commands.ProcesarDevolucion;
using LibriKeep.Core.Application.Circulacion.Commands.CrearReserva;
using LibriKeep.Core.Domain.Entities;
using LibriKeep.Core.Domain.Enums;
using LibriKeep.Infrastructure.Persistence.Context;

namespace LibriKeep.Presentation.API.Controllers
{
    [ApiController]
    public class LibriKeepApiController : ControllerControllerBase
    {
        private readonly IMediator _mediator;
        private readonly LibriKeepDbContext _context;

        public LibriKeepApiController(IMediator mediator, LibriKeepDbContext context)
        {
            _mediator = mediator;
            _context = context;
        }

        private async Task EnsureSeededAsync()
        {
            if (await _context.Autores.AnyAsync()) return;

            // Seed Autores
            var autor1 = new Autor("Robert C. Martin", "USA");
            var autor2 = new Autor("Erich Gamma", "Suizo");
            await _context.Autores.AddRangeAsync(autor1, autor2);
            await _context.SaveChangesAsync();

            // Seed Categorias
            var cat1 = new Categoria("Ingeniería de Software", "Temática de software");
            var cat2 = new Categoria("Algoritmos", "Temática de algoritmos");
            await _context.Categorias.AddRangeAsync(cat1, cat2);
            await _context.SaveChangesAsync();

            // Seed Editoriales
            var ed1 = new Editorial("Prentice Hall");
            var ed2 = new Editorial("Addison-Wesley");
            await _context.Editoriales.AddRangeAsync(ed1, ed2);
            await _context.SaveChangesAsync();

            // Seed a default lector User (Juan Pérez) and Bibliotecario
            var defaultLector = new Usuario("71234567", "Juan Pérez", "alumno@uni.edu.pe", "password", "+51999888777", TipoMiembro.Alumno, Rol.Lector);
            var defaultBibliotecaria = new Usuario("77777777", "María Gómez", "maria.gomez@biblioteca.edu.pe", "password", "+51999777666", TipoMiembro.Bibliotecario, Rol.Bibliotecario);
            await _context.Usuarios.AddRangeAsync(defaultLector, defaultBibliotecaria);
            await _context.SaveChangesAsync();

            // Seed default books
            var libro1 = new Libro("Clean Architecture", "9780134494166", autor1.Id, cat1.Id, ed1.Id, DateTime.UtcNow.AddYears(-5), "Español", "USA", TipoMaterial.LibroFisico);
            var libro2 = new Libro("Clean Code", "9780134494100", autor1.Id, cat1.Id, ed1.Id, DateTime.UtcNow.AddYears(-7), "Español", "USA", TipoMaterial.LibroFisico);
            await _context.Libros.AddRangeAsync(libro1, libro2);
            await _context.SaveChangesAsync();

            // Seed copy exemplares
            var ej1 = new Ejemplar(libro1.Id, "9780134494166-C1", "Estante A1", "Buen estado");
            var ej2 = new Ejemplar(libro1.Id, "9780134494166-C2", "Estante A1", "Buen estado");
            var ej3 = new Ejemplar(libro2.Id, "9876543210", "Estante B1", "Buen estado");
            var ej4 = new Ejemplar(libro2.Id, "1234567891234-C1", "Estante B1", "Buen estado");
            await _context.Ejemplares.AddRangeAsync(ej1, ej2, ej3, ej4);
            await _context.SaveChangesAsync();
        }

        // ==========================================
        // 1. AUTENTICACIÓN
        // ==========================================
        public override async Task<ActionResult<LoginResponse>> Login([FromBody] Body body, CancellationToken cancellationToken = default)
        {
            await EnsureSeededAsync();
            try
            {
                var emailLower = body.Email?.ToLower() ?? "";
                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower, cancellationToken);
                
                if (usuario == null)
                {
                    // Fallback to mock user if not found in seeded database (e.g. for default admin)
                    var isLibrarian = emailLower.Contains("bibliotecario") || emailLower.Contains("maria.gomez");
                    var isAdmin = emailLower.Contains("admin");

                    var rol = isAdmin ? UsuarioDtoRol.Administrador : (isLibrarian ? UsuarioDtoRol.Bibliotecario : UsuarioDtoRol.Lector);
                    var tipo = isLibrarian ? UsuarioDtoTipoMiembro.Bibliotecario : UsuarioDtoTipoMiembro.Alumno;
                    var nombre = isAdmin ? "Admin Principal" : (isLibrarian ? "María Gómez (Bibliotecaria)" : "Juan Pérez");

                    var mockUser = new UsuarioDto
                    {
                        Id = isAdmin ? 1 : (isLibrarian ? 5 : 10),
                        Dni = "71234567",
                        NombreCompleto = nombre,
                        Email = body.Email ?? "",
                        Telefono = "+51999888777",
                        TipoMiembro = tipo,
                        Rol = rol,
                        Estado = UsuarioDtoEstado.Activo
                    };

                    return Ok(new LoginResponse
                    {
                        Token = "mock-jwt-token-for-" + body.Email,
                        Usuario = mockUser
                    });
                }

                // If found in DB, return the actual user from DB!
                var mem = usuario.TipoMiembro switch
                {
                    TipoMiembro.Alumno => UsuarioDtoTipoMiembro.Alumno,
                    TipoMiembro.Docente => UsuarioDtoTipoMiembro.Docente,
                    _ => UsuarioDtoTipoMiembro.PersonalAdministrativo
                };
                var r = usuario.Rol switch
                {
                    Rol.Lector => UsuarioDtoRol.Lector,
                    Rol.Bibliotecario => UsuarioDtoRol.Bibliotecario,
                    _ => UsuarioDtoRol.Administrador
                };
                var est = usuario.Estado switch
                {
                    EstadoUsuario.Activo => UsuarioDtoEstado.Activo,
                    EstadoUsuario.BloqueoTemporal => UsuarioDtoEstado.BloqueoTemporal,
                    EstadoUsuario.Suspendido => UsuarioDtoEstado.Suspendido,
                    _ => UsuarioDtoEstado.Inactivo
                };

                var userDto = new UsuarioDto
                {
                    Id = usuario.Id,
                    Dni = usuario.Dni,
                    NombreCompleto = usuario.NombreCompleto,
                    Email = usuario.Email,
                    Telefono = usuario.Telefono,
                    TipoMiembro = mem,
                    Rol = r,
                    Estado = est
                };

                return Ok(new LoginResponse
                {
                    Token = "mock-jwt-token-for-" + usuario.Email,
                    Usuario = userDto
                });
            }
            catch (Exception)
            {
                return BadRequest(new ErrorResponse { Code = "ERR_AUTH_FAILED", Title = "Fallo", Detail = "Error al iniciar sesión." });
            }
        }

        // ==========================================
        // 2. OPAC (PÚBLICO)
        // ==========================================
        public override async Task<ActionResult<PaginatedBooks>> SearchOpacBooks(string? query = null, int? autorId = null, int? categoriaId = null, string? tipoMaterial = null, int? page = 1, int? pageSize = 10, CancellationToken cancellationToken = default)
        {
            await EnsureSeededAsync();
            try
            {
                var q = _context.Libros
                    .Include(l => l.Autor)
                    .Include(l => l.Categoria)
                    .Include(l => l.Editorial)
                    .Include(l => l.Ejemplares)
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrEmpty(query))
                {
                    var queryLower = query.ToLower();
                    q = q.Where(l => l.Titulo.ToLower().Contains(queryLower) || 
                                     l.Isbn.Contains(query) || 
                                     l.Autor.Nombre.ToLower().Contains(queryLower) ||
                                     l.Ejemplares.Any(e => e.CodigoBarras == query));
                }

                if (autorId.HasValue)
                {
                    q = q.Where(l => l.AutorId == autorId.Value);
                }

                if (categoriaId.HasValue)
                {
                    q = q.Where(l => l.CategoriaId == categoriaId.Value);
                }

                var total = await q.CountAsync(cancellationToken);
                var pSize = pageSize ?? 10;
                var pNum = page ?? 1;

                var list = await q
                    .OrderBy(l => l.Titulo)
                    .Skip((pNum - 1) * pSize)
                    .Take(pSize)
                    .ToListAsync(cancellationToken);

                var items = list.Select(l =>
                {
                    var matType = l.TipoMaterial switch
                    {
                        TipoMaterial.LibroFisico => BookDtoTipoMaterial.LibroFisico,
                        TipoMaterial.LibroDigital => BookDtoTipoMaterial.LibroDigital,
                        TipoMaterial.Revista => BookDtoTipoMaterial.Revista,
                        TipoMaterial.Tesis => BookDtoTipoMaterial.Tesis,
                        _ => BookDtoTipoMaterial.Otro
                    };

                    return new BookDto
                    {
                        Id = l.Id,
                        Titulo = l.Titulo,
                        Isbn = l.Isbn,
                        Autor = new AutorDto { Id = l.Autor.Id, Nombre = l.Autor.Nombre, Nacionalidad = l.Autor.Nacionalidad },
                        Categoria = new CategoriaDto { Id = l.Categoria.Id, Nombre = l.Categoria.Nombre, Descripcion = l.Categoria.Descripcion },
                        Editorial = new EditorialDto { Id = l.Editorial.Id, Nombre = l.Editorial.Nombre },
                        FechaPublicacion = l.FechaPublicacion,
                        Idioma = l.Idioma,
                        Pais = l.Pais,
                        TipoMaterial = matType,
                        TotalCopias = l.Ejemplares.Count,
                        CopiasDisponibles = l.Ejemplares.Count(e => e.Estado == EstadoEjemplar.Disponible)
                    };
                }).ToList();

                return Ok(new PaginatedBooks
                {
                    TotalItems = total,
                    Page = pNum,
                    PageSize = pSize,
                    TotalPages = (int)Math.Ceiling((double)total / pSize),
                    Items = items
                });
            }
            catch (Exception)
            {
                return Ok(new PaginatedBooks { TotalItems = 0, Page = 1, PageSize = 10, TotalPages = 0, Items = new List<BookDto>() });
            }
        }

        public override async Task<ActionResult<BookDetail>> GetOpacBookById(int id, CancellationToken cancellationToken = default)
        {
            await EnsureSeededAsync();
            try
            {
                var libro = await _context.Libros
                    .Include(l => l.Autor)
                    .Include(l => l.Categoria)
                    .Include(l => l.Editorial)
                    .Include(l => l.Ejemplares)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

                if (libro == null)
                {
                    return NotFound(new ErrorResponse { Code = "ERR_BOOK_NOT_FOUND", Title = "No Encontrado", Detail = "El libro especificado no existe." });
                }

                var matType = libro.TipoMaterial switch
                {
                    TipoMaterial.LibroFisico => BookDtoTipoMaterial.LibroFisico,
                    TipoMaterial.LibroDigital => BookDtoTipoMaterial.LibroDigital,
                    TipoMaterial.Revista => BookDtoTipoMaterial.Revista,
                    TipoMaterial.Tesis => BookDtoTipoMaterial.Tesis,
                    _ => BookDtoTipoMaterial.Otro
                };

                var book = new BookDto
                {
                    Id = libro.Id,
                    Titulo = libro.Titulo,
                    Isbn = libro.Isbn,
                    Autor = new AutorDto { Id = libro.Autor.Id, Nombre = libro.Autor.Nombre, Nacionalidad = libro.Autor.Nacionalidad },
                    Categoria = new CategoriaDto { Id = libro.Categoria.Id, Nombre = libro.Categoria.Nombre, Descripcion = libro.Categoria.Descripcion },
                    Editorial = new EditorialDto { Id = libro.Editorial.Id, Nombre = libro.Editorial.Nombre },
                    FechaPublicacion = libro.FechaPublicacion,
                    Idioma = libro.Idioma,
                    Pais = libro.Pais,
                    TipoMaterial = matType,
                    TotalCopias = libro.Ejemplares.Count,
                    CopiasDisponibles = libro.Ejemplares.Count(e => e.Estado == EstadoEjemplar.Disponible)
                };

                var ejemplares = libro.Ejemplares.Select(e =>
                {
                    var est = e.Estado switch
                    {
                        EstadoEjemplar.Disponible => EjemplarDtoEstado.Disponible,
                        EstadoEjemplar.Prestado => EjemplarDtoEstado.Prestado,
                        EstadoEjemplar.EnSala => EjemplarDtoEstado.EnSala,
                        EstadoEjemplar.Mantenimiento => EjemplarDtoEstado.Mantenimiento,
                        EstadoEjemplar.Pérdida => EjemplarDtoEstado.Pérdida,
                        _ => EjemplarDtoEstado.Reservado
                    };

                    return new EjemplarDto
                    {
                        Id = e.Id,
                        LibroId = e.LibroId,
                        CodigoBarras = e.CodigoBarras,
                        Estado = est,
                        UbicacionFisica = e.UbicacionFisica,
                        Observaciones = e.Observaciones
                    };
                }).ToList();

                return Ok(new BookDetail { Libro = book, Ejemplares = ejemplares });
            }
            catch (Exception)
            {
                return NotFound(new ErrorResponse { Code = "ERR_BOOK_NOT_FOUND", Title = "No Encontrado", Detail = "El libro no pudo ser recuperado." });
            }
        }

        // ==========================================
        // 3. CATALOGACIÓN & EJEMPLARES
        // ==========================================
        public override async Task<ActionResult<PaginatedBooks>> GetBooks(string? query = null, int? autorId = null, int? categoriaId = null, int? page = 1, int? pageSize = 10, CancellationToken cancellationToken = default)
        {
            return await SearchOpacBooks(query, autorId, categoriaId, null, page, pageSize, cancellationToken);
        }

        public override async Task<ActionResult<BookDto>> CreateBook([FromBody] CreateBookDto body, CancellationToken cancellationToken = default)
        {
            await EnsureSeededAsync();
            var operatorRole = await GetOperatorRoleAsync(cancellationToken);
            if (operatorRole != Rol.Administrador && operatorRole != Rol.Bibliotecario)
            {
                return StatusCode(403, new ErrorResponse { Code = "ERR_FORBIDDEN", Title = "Acceso Denegado", Detail = "Solo el personal de biblioteca autorizado (Bibliotecario o Administrador) puede realizar esta acción." });
            }

            try
            {
                var matType = body.TipoMaterial switch
                {
                    CreateBookDtoTipoMaterial.LibroFisico => TipoMaterial.LibroFisico,
                    CreateBookDtoTipoMaterial.LibroDigital => TipoMaterial.LibroDigital,
                    CreateBookDtoTipoMaterial.Revista => TipoMaterial.Revista,
                    CreateBookDtoTipoMaterial.Tesis => TipoMaterial.Tesis,
                    _ => TipoMaterial.Otro
                };

                var existing = await _context.Libros
                    .FirstOrDefaultAsync(l => l.Isbn == body.Isbn, cancellationToken);
                if (existing != null)
                {
                    return BadRequest(new ErrorResponse { Code = "ERR_DUPLICATE_ISBN", Title = "Conflicto", Detail = "El libro con este ISBN ya existe." });
                }

                Autor autor;
                if (!string.IsNullOrWhiteSpace(body.AutorNombre))
                {
                    var cleanName = body.AutorNombre.Trim();
                    autor = await _context.Autores.FirstOrDefaultAsync(a => a.Nombre.ToLower() == cleanName.ToLower(), cancellationToken);
                    if (autor == null)
                    {
                        autor = new Autor(cleanName, "Nacionalidad no registrada");
                        await _context.Autores.AddAsync(autor, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }
                else
                {
                    autor = await _context.Autores.FindAsync(new object[] { body.AutorId }, cancellationToken);
                    if (autor == null)
                    {
                        autor = new Autor("Robert C. Martin", "USA");
                        await _context.Autores.AddAsync(autor, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }

                Categoria categoria;
                if (!string.IsNullOrWhiteSpace(body.CategoriaNombre))
                {
                    var cleanName = body.CategoriaNombre.Trim();
                    categoria = await _context.Categorias.FirstOrDefaultAsync(c => c.Nombre.ToLower() == cleanName.ToLower(), cancellationToken);
                    if (categoria == null)
                    {
                        categoria = new Categoria(cleanName, "Creado dinámicamente durante la catalogación");
                        await _context.Categorias.AddAsync(categoria, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }
                else
                {
                    categoria = await _context.Categorias.FindAsync(new object[] { body.CategoriaId }, cancellationToken);
                    if (categoria == null)
                    {
                        categoria = new Categoria("Ingeniería de Software", "Temática de software");
                        await _context.Categorias.AddAsync(categoria, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }

                Editorial editorial;
                if (!string.IsNullOrWhiteSpace(body.EditorialNombre))
                {
                    var cleanName = body.EditorialNombre.Trim();
                    editorial = await _context.Editoriales.FirstOrDefaultAsync(e => e.Nombre.ToLower() == cleanName.ToLower(), cancellationToken);
                    if (editorial == null)
                    {
                        editorial = new Editorial(cleanName);
                        await _context.Editoriales.AddAsync(editorial, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }
                else
                {
                    editorial = await _context.Editoriales.FindAsync(new object[] { body.EditorialId }, cancellationToken);
                    if (editorial == null)
                    {
                        editorial = new Editorial("Prentice Hall");
                        await _context.Editoriales.AddAsync(editorial, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }

                var libro = new Libro(body.Titulo, body.Isbn, autor.Id, categoria.Id, editorial.Id, body.FechaPublicacion.DateTime, body.Idioma, body.Pais, matType);
                await _context.Libros.AddAsync(libro, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                var matTypeDto = body.TipoMaterial switch
                {
                    CreateBookDtoTipoMaterial.LibroFisico => BookDtoTipoMaterial.LibroFisico,
                    CreateBookDtoTipoMaterial.LibroDigital => BookDtoTipoMaterial.LibroDigital,
                    CreateBookDtoTipoMaterial.Revista => BookDtoTipoMaterial.Revista,
                    CreateBookDtoTipoMaterial.Tesis => BookDtoTipoMaterial.Tesis,
                    _ => BookDtoTipoMaterial.Otro
                };

                var created = new BookDto
                {
                    Id = libro.Id,
                    Titulo = libro.Titulo,
                    Isbn = libro.Isbn,
                    Autor = new AutorDto { Id = autor.Id, Nombre = autor.Nombre, Nacionalidad = autor.Nacionalidad },
                    Categoria = new CategoriaDto { Id = categoria.Id, Nombre = categoria.Nombre, Descripcion = categoria.Descripcion },
                    Editorial = new EditorialDto { Id = editorial.Id, Nombre = editorial.Nombre },
                    FechaPublicacion = libro.FechaPublicacion,
                    Idioma = libro.Idioma,
                    Pais = libro.Pais,
                    TipoMaterial = matTypeDto,
                    TotalCopias = 0,
                    CopiasDisponibles = 0
                };

                return CreatedAtAction(nameof(GetBookById), new { id = libro.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponse { Code = "ERR_CATALOGING_FAILED", Title = "Error de Catalogación", Detail = $"No se pudo crear el libro: {ex.Message}" });
            }
        }

        public override async Task<ActionResult<BookDto>> GetBookById(int id, CancellationToken cancellationToken = default)
        {
            await EnsureSeededAsync();
            try
            {
                var libro = await _context.Libros
                    .Include(l => l.Autor)
                    .Include(l => l.Categoria)
                    .Include(l => l.Editorial)
                    .Include(l => l.Ejemplares)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

                if (libro == null)
                {
                    return NotFound(new ErrorResponse { Code = "ERR_BOOK_NOT_FOUND", Title = "No Encontrado", Detail = "El libro no existe." });
                }

                var matType = libro.TipoMaterial switch
                {
                    TipoMaterial.LibroFisico => BookDtoTipoMaterial.LibroFisico,
                    TipoMaterial.LibroDigital => BookDtoTipoMaterial.LibroDigital,
                    TipoMaterial.Revista => BookDtoTipoMaterial.Revista,
                    TipoMaterial.Tesis => BookDtoTipoMaterial.Tesis,
                    _ => BookDtoTipoMaterial.Otro
                };

                return Ok(new BookDto
                {
                    Id = libro.Id,
                    Titulo = libro.Titulo,
                    Isbn = libro.Isbn,
                    Autor = new AutorDto { Id = libro.Autor.Id, Nombre = libro.Autor.Nombre, Nacionalidad = libro.Autor.Nacionalidad },
                    Categoria = new CategoriaDto { Id = libro.Categoria.Id, Nombre = libro.Categoria.Nombre, Descripcion = libro.Categoria.Descripcion },
                    Editorial = new EditorialDto { Id = libro.Editorial.Id, Nombre = libro.Editorial.Nombre },
                    FechaPublicacion = libro.FechaPublicacion,
                    Idioma = libro.Idioma,
                    Pais = libro.Pais,
                    TipoMaterial = matType,
                    TotalCopias = libro.Ejemplares.Count,
                    CopiasDisponibles = libro.Ejemplares.Count(e => e.Estado == EstadoEjemplar.Disponible)
                });
            }
            catch (Exception)
            {
                return NotFound(new ErrorResponse { Code = "ERR_BOOK_NOT_FOUND", Title = "No Encontrado", Detail = "No se pudo consultar el libro." });
            }
        }

        public override async Task<ActionResult<BookDto>> UpdateBook(int id, [FromBody] UpdateBookDto body, CancellationToken cancellationToken = default)
        {
            try
            {
                var libro = await _context.Libros.FindAsync(new object[] { id }, cancellationToken);
                if (libro == null)
                {
                    return NotFound(new ErrorResponse { Code = "ERR_BOOK_NOT_FOUND", Title = "No Encontrado", Detail = "El libro no existe." });
                }

                var matType = body.TipoMaterial switch
                {
                    UpdateBookDtoTipoMaterial.LibroFisico => TipoMaterial.LibroFisico,
                    UpdateBookDtoTipoMaterial.LibroDigital => TipoMaterial.LibroDigital,
                    UpdateBookDtoTipoMaterial.Revista => TipoMaterial.Revista,
                    UpdateBookDtoTipoMaterial.Tesis => TipoMaterial.Tesis,
                    _ => TipoMaterial.Otro
                };

                libro.Update(body.Titulo, body.Isbn, body.AutorId, body.CategoriaId, body.EditorialId, body.FechaPublicacion.DateTime, body.Idioma, body.Pais, matType);
                await _context.SaveChangesAsync(cancellationToken);

                return await GetBookById(id, cancellationToken);
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponse { Code = "ERR_UPDATE_FAILED", Title = "Error de Actualización", Detail = $"No se pudo actualizar el libro: {ex.Message}" });
            }
        }

        public override async Task<IActionResult> DeleteBook(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var libro = await _context.Libros.FindAsync(new object[] { id }, cancellationToken);
                if (libro == null) return NotFound();

                _context.Libros.Remove(libro);
                await _context.SaveChangesAsync(cancellationToken);
                return NoContent();
            }
            catch (Exception)
            {
                return BadRequest(new ErrorResponse { Code = "ERR_DELETE_FAILED", Title = "Error de Eliminación", Detail = "No se puede eliminar por dependencias activas." });
            }
        }

        public override async Task<ActionResult<ICollection<EjemplarDto>>> GetEjemplaresByLibro(int libroId, CancellationToken cancellationToken = default)
        {
            try
            {
                var list = await _context.Ejemplares
                    .AsNoTracking()
                    .Where(e => e.LibroId == libroId)
                    .ToListAsync(cancellationToken);

                var dtos = list.Select(e =>
                {
                    var est = e.Estado switch
                    {
                        EstadoEjemplar.Disponible => EjemplarDtoEstado.Disponible,
                        EstadoEjemplar.Prestado => EjemplarDtoEstado.Prestado,
                        EstadoEjemplar.EnSala => EjemplarDtoEstado.EnSala,
                        EstadoEjemplar.Mantenimiento => EjemplarDtoEstado.Mantenimiento,
                        EstadoEjemplar.Pérdida => EjemplarDtoEstado.Pérdida,
                        _ => EjemplarDtoEstado.Reservado
                    };

                    return new EjemplarDto
                    {
                        Id = e.Id,
                        LibroId = e.LibroId,
                        CodigoBarras = e.CodigoBarras,
                        Estado = est,
                        UbicacionFisica = e.UbicacionFisica,
                        Observaciones = e.Observaciones
                    };
                }).ToList();

                return Ok((ICollection<EjemplarDto>)dtos);
            }
            catch (Exception)
            {
                return Ok((ICollection<EjemplarDto>)new List<EjemplarDto>());
            }
        }

        public override async Task<ActionResult<EjemplarDto>> CreateEjemplar(int libroId, [FromBody] CreateEjemplarDto body, CancellationToken cancellationToken = default)
        {
            try
            {
                var ejemplar = new Ejemplar(libroId, body.CodigoBarras, body.UbicacionFisica, body.Observaciones);
                await _context.Ejemplares.AddAsync(ejemplar, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                return CreatedAtAction(nameof(GetEjemplarById), new { id = ejemplar.Id }, new EjemplarDto
                {
                    Id = ejemplar.Id,
                    LibroId = libroId,
                    CodigoBarras = ejemplar.CodigoBarras,
                    Estado = EjemplarDtoEstado.Disponible,
                    UbicacionFisica = ejemplar.UbicacionFisica,
                    Observaciones = ejemplar.Observaciones
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponse { Code = "ERR_EJEMPLAR_CREATION_FAILED", Title = "Error de Ingesta de Copia", Detail = $"No se pudo crear la copia física: {ex.Message}" });
            }
        }

        public override async Task<ActionResult<EjemplarDto>> GetEjemplarById(int id, CancellationToken cancellationToken = default)
        {
            return Ok(new EjemplarDto
            {
                Id = id,
                LibroId = 1,
                CodigoBarras = "9780134494166-C1",
                Estado = EjemplarDtoEstado.Disponible,
                UbicacionFisica = "Estante A",
                Observaciones = "Bueno"
            });
        }

        public override async Task<ActionResult<EjemplarDto>> UpdateEjemplar(int id, [FromBody] UpdateEjemplarDto body, CancellationToken cancellationToken = default)
        {
            var est = body.Estado switch
            {
                UpdateEjemplarDtoEstado.Disponible => EjemplarDtoEstado.Disponible,
                UpdateEjemplarDtoEstado.Prestado => EjemplarDtoEstado.Prestado,
                UpdateEjemplarDtoEstado.EnSala => EjemplarDtoEstado.EnSala,
                UpdateEjemplarDtoEstado.Mantenimiento => EjemplarDtoEstado.Mantenimiento,
                UpdateEjemplarDtoEstado.Pérdida => EjemplarDtoEstado.Pérdida,
                UpdateEjemplarDtoEstado.Reservado => EjemplarDtoEstado.Reservado,
                _ => EjemplarDtoEstado.Disponible
            };

            return Ok(new EjemplarDto
            {
                Id = id,
                LibroId = 1,
                CodigoBarras = "9780134494166-C1",
                Estado = est,
                UbicacionFisica = body.UbicacionFisica,
                Observaciones = body.Observaciones
            });
        }

        public override async Task<IActionResult> DeleteEjemplar(int id, CancellationToken cancellationToken = default)
        {
            return NoContent();
        }

        // ==========================================
        // 4. AUTORIDADES (MOCK)
        // ==========================================
        public override async Task<ActionResult<ICollection<AutorDto>>> GetAutores(string? query = null, CancellationToken cancellationToken = default)
        {
            await EnsureSeededAsync();
            try
            {
                var q = _context.Autores.AsNoTracking().AsQueryable();
                if (!string.IsNullOrEmpty(query))
                {
                    q = q.Where(a => a.Nombre.ToLower().Contains(query.ToLower()));
                }
                var list = await q.ToListAsync(cancellationToken);
                var dtos = list.Select(a => new AutorDto { Id = a.Id, Nombre = a.Nombre, Nacionalidad = a.Nacionalidad }).ToList();
                return Ok((ICollection<AutorDto>)dtos);
            }
            catch
            {
                return Ok((ICollection<AutorDto>)new List<AutorDto>());
            }
        }

        public override async Task<ActionResult<AutorDto>> CreateAutor([FromBody] CreateAutorDto body, CancellationToken cancellationToken = default)
        {
            return CreatedAtAction(nameof(GetAutores), new AutorDto { Id = 1, Nombre = body.Nombre, Nacionalidad = body.Nacionalidad });
        }

        public override async Task<ActionResult<AutorDto>> UpdateAutor(int id, [FromBody] CreateAutorDto body, CancellationToken cancellationToken = default)
        {
            return Ok(new AutorDto { Id = id, Nombre = body.Nombre, Nacionalidad = body.Nacionalidad });
        }

        public override async Task<IActionResult> DeleteAutor(int id, CancellationToken cancellationToken = default)
        {
            return NoContent();
        }

        public override async Task<ActionResult<ICollection<CategoriaDto>>> GetCategorias(CancellationToken cancellationToken = default)
        {
            await EnsureSeededAsync();
            try
            {
                var list = await _context.Categorias.AsNoTracking().ToListAsync(cancellationToken);
                var dtos = list.Select(c => new CategoriaDto { Id = c.Id, Nombre = c.Nombre, Descripcion = c.Descripcion }).ToList();
                return Ok((ICollection<CategoriaDto>)dtos);
            }
            catch
            {
                return Ok((ICollection<CategoriaDto>)new List<CategoriaDto>());
            }
        }

        public override async Task<ActionResult<CategoriaDto>> CreateCategoria([FromBody] CreateCategoriaDto body, CancellationToken cancellationToken = default)
        {
            return CreatedAtAction(nameof(GetCategorias), new CategoriaDto { Id = 1, Nombre = body.Nombre, Descripcion = body.Descripcion });
        }

        public override async Task<IActionResult> UpdateCategoria(int id, [FromBody] CreateCategoriaDto body, CancellationToken cancellationToken = default)
        {
            return Ok(new CategoriaDto { Id = id, Nombre = body.Nombre, Descripcion = body.Descripcion });
        }

        public override async Task<IActionResult> DeleteCategoria(int id, CancellationToken cancellationToken = default)
        {
            return NoContent();
        }

        public override async Task<ActionResult<ICollection<EditorialDto>>> GetEditoriales(CancellationToken cancellationToken = default)
        {
            await EnsureSeededAsync();
            try
            {
                var list = await _context.Editoriales.AsNoTracking().ToListAsync(cancellationToken);
                var dtos = list.Select(e => new EditorialDto { Id = e.Id, Nombre = e.Nombre }).ToList();
                return Ok((ICollection<EditorialDto>)dtos);
            }
            catch
            {
                return Ok((ICollection<EditorialDto>)new List<EditorialDto>());
            }
        }

        public override async Task<ActionResult<EditorialDto>> CreateEditorial([FromBody] CreateEditorialDto body, CancellationToken cancellationToken = default)
        {
            return CreatedAtAction(nameof(GetEditoriales), new EditorialDto { Id = 1, Nombre = body.Nombre });
        }

        public override async Task<IActionResult> UpdateEditorial(int id, [FromBody] CreateEditorialDto body, CancellationToken cancellationToken = default)
        {
            return Ok(new EditorialDto { Id = id, Nombre = body.Nombre });
        }

        public override async Task<IActionResult> DeleteEditorial(int id, CancellationToken cancellationToken = default)
        {
            return NoContent();
        }

        // ==========================================
        // 5. USUARIOS
        // ==========================================
        public override async Task<ActionResult<ICollection<UsuarioDto>>> GetUsuarios(string? search = null, CancellationToken cancellationToken = default)
        {
            await EnsureSeededAsync();
            try
            {
                var q = _context.Usuarios.AsNoTracking().AsQueryable();
                if (!string.IsNullOrEmpty(search))
                {
                    var searchLower = search.ToLower();
                    q = q.Where(u => u.NombreCompleto.ToLower().Contains(searchLower) || u.Dni.Contains(search));
                }
                var list = await q.ToListAsync(cancellationToken);
                var operatorRole = await GetOperatorRoleAsync(cancellationToken);
                var dtos = list.Select(u =>
                {
                    var mem = u.TipoMiembro switch
                    {
                        TipoMiembro.Alumno => UsuarioDtoTipoMiembro.Alumno,
                        TipoMiembro.Docente => UsuarioDtoTipoMiembro.Docente,
                        _ => UsuarioDtoTipoMiembro.PersonalAdministrativo
                    };
                    var r = u.Rol switch
                    {
                        Rol.Lector => UsuarioDtoRol.Lector,
                        Rol.Bibliotecario => UsuarioDtoRol.Bibliotecario,
                        _ => UsuarioDtoRol.Administrador
                    };
                    var est = u.Estado switch
                    {
                        EstadoUsuario.Activo => UsuarioDtoEstado.Activo,
                        EstadoUsuario.BloqueoTemporal => UsuarioDtoEstado.BloqueoTemporal,
                        EstadoUsuario.Suspendido => UsuarioDtoEstado.Suspendido,
                        _ => UsuarioDtoEstado.Inactivo
                    };
                    return new UsuarioDto 
                    { 
                        Id = u.Id, 
                        Dni = u.Dni, 
                        NombreCompleto = u.NombreCompleto, 
                        Email = u.Email, 
                        Telefono = u.Telefono, 
                        TipoMiembro = mem, 
                        Rol = r, 
                        Estado = est,
                        Password = operatorRole == Rol.Administrador ? u.PasswordHash : null
                    };
                }).ToList();
                return Ok((ICollection<UsuarioDto>)dtos);
            }
            catch (Exception)
            {
                return Ok((ICollection<UsuarioDto>)new List<UsuarioDto>());
            }
        }

        private async Task<Rol> GetOperatorRoleAsync(CancellationToken cancellationToken)
        {
            string? authHeader = Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Rol.Lector;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var email = token.Replace("mock-jwt-token-for-", "").Trim().ToLower();

            var dbUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);
            if (dbUser != null)
            {
                return dbUser.Rol;
            }

            if (email.Contains("admin"))
            {
                return Rol.Administrador;
            }
            if (email.Contains("bibliotecario") || email.Contains("maria.gomez"))
            {
                return Rol.Bibliotecario;
            }

            return Rol.Lector;
        }

        public override async Task<ActionResult<UsuarioDto>> CreateUsuario([FromBody] CreateUsuarioDto body, CancellationToken cancellationToken = default)
        {
            await EnsureSeededAsync();
            try
            {
                var rol = body.Rol switch
                {
                    CreateUsuarioDtoRol.Lector => Rol.Lector,
                    CreateUsuarioDtoRol.Bibliotecario => Rol.Bibliotecario,
                    _ => Rol.Administrador
                };

                var operatorRole = await GetOperatorRoleAsync(cancellationToken);
                if (operatorRole == Rol.Bibliotecario && (rol == Rol.Bibliotecario || rol == Rol.Administrador))
                {
                    return StatusCode(403, new ErrorResponse 
                    { 
                        Code = "ERR_FORBIDDEN", 
                        Title = "Acceso Denegado", 
                        Detail = "Un bibliotecario no puede registrar usuarios con roles elevados (Bibliotecario o Administrador)." 
                    });
                }
                if (operatorRole == Rol.Lector)
                {
                    return StatusCode(403, new ErrorResponse 
                    { 
                        Code = "ERR_FORBIDDEN", 
                        Title = "Acceso Denegado", 
                        Detail = "Solo el personal autorizado puede registrar nuevos usuarios." 
                    });
                }

                var existingDni = await _context.Usuarios.AnyAsync(u => u.Dni == body.Dni, cancellationToken);
                if (existingDni)
                {
                    return BadRequest(new ErrorResponse { Code = "ERR_DUPLICATE_DNI", Title = "Duplicado", Detail = "El DNI ingresado ya se encuentra registrado." });
                }

                var existingEmail = await _context.Usuarios.AnyAsync(u => u.Email == body.Email, cancellationToken);
                if (existingEmail)
                {
                    return BadRequest(new ErrorResponse { Code = "ERR_DUPLICATE_EMAIL", Title = "Duplicado", Detail = "El correo electrónico ya se encuentra registrado." });
                }

                var tipoMiembro = body.TipoMiembro switch
                {
                    CreateUsuarioDtoTipoMiembro.Alumno => TipoMiembro.Alumno,
                    CreateUsuarioDtoTipoMiembro.Docente => TipoMiembro.Docente,
                    CreateUsuarioDtoTipoMiembro.PersonalAdministrativo => TipoMiembro.PersonalAdministrativo,
                    CreateUsuarioDtoTipoMiembro.Bibliotecario => TipoMiembro.Bibliotecario,
                    _ => TipoMiembro.Externo
                };

                var usuario = new Usuario(body.Dni, body.NombreCompleto, body.Email, body.Password, body.Telefono, tipoMiembro, rol);
                await _context.Usuarios.AddAsync(usuario, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                var mem = body.TipoMiembro switch
                {
                    CreateUsuarioDtoTipoMiembro.Alumno => UsuarioDtoTipoMiembro.Alumno,
                    CreateUsuarioDtoTipoMiembro.Docente => UsuarioDtoTipoMiembro.Docente,
                    CreateUsuarioDtoTipoMiembro.PersonalAdministrativo => UsuarioDtoTipoMiembro.PersonalAdministrativo,
                    CreateUsuarioDtoTipoMiembro.Bibliotecario => UsuarioDtoTipoMiembro.Bibliotecario,
                    _ => UsuarioDtoTipoMiembro.Externo
                };

                var r = body.Rol switch
                {
                    CreateUsuarioDtoRol.Lector => UsuarioDtoRol.Lector,
                    CreateUsuarioDtoRol.Bibliotecario => UsuarioDtoRol.Bibliotecario,
                    _ => UsuarioDtoRol.Administrador
                };

                var dto = new UsuarioDto
                {
                    Id = usuario.Id,
                    Dni = usuario.Dni,
                    NombreCompleto = usuario.NombreCompleto,
                    Email = usuario.Email,
                    Telefono = usuario.Telefono,
                    TipoMiembro = mem,
                    Rol = r,
                    Estado = UsuarioDtoEstado.Activo,
                    Password = operatorRole == Rol.Administrador ? usuario.PasswordHash : null
                };

                return CreatedAtAction(nameof(GetUsuarioById), new { id = usuario.Id }, dto);
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponse { Code = "ERR_USER_CREATION_FAILED", Title = "Error", Detail = $"No se pudo registrar el lector: {ex.Message}" });
            }
        }

        public override async Task<ActionResult<UsuarioDto>> GetUsuarioById(int id, CancellationToken cancellationToken = default)
        {
            await EnsureSeededAsync();
            try
            {
                var u = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (u == null)
                {
                    return NotFound(new ErrorResponse { Code = "ERR_USER_NOT_FOUND", Title = "No Encontrado", Detail = "El usuario no existe." });
                }

                var mem = u.TipoMiembro switch
                {
                    TipoMiembro.Alumno => UsuarioDtoTipoMiembro.Alumno,
                    TipoMiembro.Docente => UsuarioDtoTipoMiembro.Docente,
                    _ => UsuarioDtoTipoMiembro.PersonalAdministrativo
                };
                var r = u.Rol switch
                {
                    Rol.Lector => UsuarioDtoRol.Lector,
                    Rol.Bibliotecario => UsuarioDtoRol.Bibliotecario,
                    _ => UsuarioDtoRol.Administrador
                };
                var est = u.Estado switch
                {
                    EstadoUsuario.Activo => UsuarioDtoEstado.Activo,
                    EstadoUsuario.BloqueoTemporal => UsuarioDtoEstado.BloqueoTemporal,
                    EstadoUsuario.Suspendido => UsuarioDtoEstado.Suspendido,
                    _ => UsuarioDtoEstado.Inactivo
                };

                var operatorRole = await GetOperatorRoleAsync(cancellationToken);

                return Ok(new UsuarioDto
                {
                    Id = u.Id,
                    Dni = u.Dni,
                    NombreCompleto = u.NombreCompleto,
                    Email = u.Email,
                    Telefono = u.Telefono,
                    TipoMiembro = mem,
                    Rol = r,
                    Estado = est,
                    Password = operatorRole == Rol.Administrador ? u.PasswordHash : null
                });
            }
            catch
            {
                return NotFound(new ErrorResponse { Code = "ERR_USER_NOT_FOUND", Title = "Error", Detail = "No se pudo recuperar el usuario." });
            }
        }

        public override async Task<ActionResult<UsuarioDto>> UpdateUsuario(int id, [FromBody] UpdateUsuarioDto body, CancellationToken cancellationToken = default)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(new object[] { id }, cancellationToken);
                if (usuario == null)
                {
                    return NotFound(new ErrorResponse { Code = "ERR_USER_NOT_FOUND", Title = "No Encontrado", Detail = "El usuario no existe." });
                }

                var tipoMiembro = body.TipoMiembro switch
                {
                    UpdateUsuarioDtoTipoMiembro.Alumno => TipoMiembro.Alumno,
                    UpdateUsuarioDtoTipoMiembro.Docente => TipoMiembro.Docente,
                    UpdateUsuarioDtoTipoMiembro.PersonalAdministrativo => TipoMiembro.PersonalAdministrativo,
                    UpdateUsuarioDtoTipoMiembro.Bibliotecario => TipoMiembro.Bibliotecario,
                    _ => TipoMiembro.Externo
                };

                // Actualizar
                usuario.Update(body.NombreCompleto, body.Email, body.Telefono, tipoMiembro, usuario.Rol, usuario.Estado);
                await _context.SaveChangesAsync(cancellationToken);

                return await GetUsuarioById(id, cancellationToken);
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponse { Code = "ERR_USER_UPDATE_FAILED", Title = "Error", Detail = $"No se pudo actualizar el perfil: {ex.Message}" });
            }
        }

        public override async Task<IActionResult> DeleteUsuario(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var u = await _context.Usuarios.FindAsync(new object[] { id }, cancellationToken);
                if (u == null) return NotFound();

                _context.Usuarios.Remove(u);
                await _context.SaveChangesAsync(cancellationToken);
                return NoContent();
            }
            catch
            {
                return BadRequest(new ErrorResponse { Code = "ERR_USER_DELETE_FAILED", Title = "Error", Detail = "No se puede eliminar el usuario por dependencias activas." });
            }
        }

        public override async Task<ActionResult<UsuarioPerfilDto>> GetUsuarioPerfil(int id, CancellationToken cancellationToken = default)
        {
            await EnsureSeededAsync();
            try
            {
                var usuario = await _context.Usuarios
                    .Include(u => u.Prestamos)
                    .Include(u => u.Sanciones)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

                if (usuario == null)
                {
                    return NotFound(new ErrorResponse { Code = "ERR_USER_NOT_FOUND", Title = "No Encontrado", Detail = "El usuario no existe." });
                }

                var mem = usuario.TipoMiembro switch
                {
                    TipoMiembro.Alumno => UsuarioDtoTipoMiembro.Alumno,
                    TipoMiembro.Docente => UsuarioDtoTipoMiembro.Docente,
                    _ => UsuarioDtoTipoMiembro.PersonalAdministrativo
                };
                var r = usuario.Rol switch
                {
                    Rol.Lector => UsuarioDtoRol.Lector,
                    Rol.Bibliotecario => UsuarioDtoRol.Bibliotecario,
                    _ => UsuarioDtoRol.Administrador
                };
                var est = usuario.Estado switch
                {
                    EstadoUsuario.Activo => UsuarioDtoEstado.Activo,
                    EstadoUsuario.BloqueoTemporal => UsuarioDtoEstado.BloqueoTemporal,
                    EstadoUsuario.Suspendido => UsuarioDtoEstado.Suspendido,
                    _ => UsuarioDtoEstado.Inactivo
                };

                var userDto = new UsuarioDto
                {
                    Id = usuario.Id,
                    Dni = usuario.Dni,
                    NombreCompleto = usuario.NombreCompleto,
                    Email = usuario.Email,
                    Telefono = usuario.Telefono,
                    TipoMiembro = mem,
                    Rol = r,
                    Estado = est
                };

                var activeLoansCount = usuario.Prestamos.Count(p => p.Estado == EstadoPrestamo.Activo);
                var maxLoans = usuario.TipoMiembro switch
                {
                    TipoMiembro.Docente => 5,
                    TipoMiembro.Alumno => 3,
                    _ => 4
                };

                var hasActiveSancion = usuario.Sanciones.Any(s => s.Estado == EstadoSancion.Activa);
                var hasOverdueLoan = usuario.Prestamos.Any(p => p.Estado == EstadoPrestamo.Activo && p.FechaMaxDevolucion < DateTime.UtcNow);

                return Ok(new UsuarioPerfilDto
                {
                    Usuario = userDto,
                    PrestamosActivosCount = activeLoansCount,
                    PrestamosPermitidosCount = maxLoans,
                    TieneMultaImpagada = false,
                    TieneSancionActiva = hasActiveSancion,
                    TienePrestamoVencido = hasOverdueLoan
                });
            }
            catch (Exception)
            {
                return NotFound(new ErrorResponse { Code = "ERR_USER_NOT_FOUND", Title = "Error", Detail = "No se pudo recuperar el perfil." });
            }
        }

        public override async Task<ActionResult<ICollection<SancionDto>>> GetUsuarioSanciones(int id, CancellationToken cancellationToken = default)
        {
            var list = new List<SancionDto>
            {
                new() { Id = 22, UsuarioId = id, PrestamoId = 45, FechaInicio = DateTimeOffset.UtcNow.AddDays(-10), FechaFin = DateTimeOffset.UtcNow.AddDays(-8), DiasSancion = 2, Estado = SancionDtoEstado.Expirada }
            };
            return Ok(list);
        }

        // ==========================================
        // 6. CIRCULACIÓN (CORE MEDIATR)
        // ==========================================
        public override async Task<ActionResult<PaginatedPrestamos>> GetPrestamos(int? usuarioId = null, Estado? estado = null, int? page = 1, int? pageSize = 10, CancellationToken cancellationToken = default)
        {
            await EnsureSeededAsync();
            try
            {
                var q = _context.Prestamos
                    .Include(p => p.Usuario)
                    .Include(p => p.Ejemplar)
                    .ThenInclude(e => e.Libro)
                    .AsNoTracking()
                    .AsQueryable();

                if (usuarioId.HasValue)
                {
                    q = q.Where(p => p.UsuarioId == usuarioId.Value);
                }

                if (estado.HasValue)
                {
                    var domainState = estado.Value switch
                    {
                        Estado.Activo => EstadoPrestamo.Activo,
                        Estado.Devuelto => EstadoPrestamo.Devuelto,
                        _ => EstadoPrestamo.Demorado
                    };
                    q = q.Where(p => p.Estado == domainState);
                }

                var total = await q.CountAsync(cancellationToken);
                var pSize = pageSize ?? 10;
                var pNum = page ?? 1;

                var list = await q
                    .OrderByDescending(p => p.FechaSalida)
                    .Skip((pNum - 1) * pSize)
                    .Take(pSize)
                    .ToListAsync(cancellationToken);

                var items = list.Select(p =>
                {
                    var dtoState = p.Estado switch
                    {
                        EstadoPrestamo.Activo => PrestamoDtoEstado.Activo,
                        EstadoPrestamo.Devuelto => PrestamoDtoEstado.Devuelto,
                        _ => PrestamoDtoEstado.Demorado
                    };

                    return new PrestamoDto
                    {
                        Id = p.Id,
                        UsuarioId = p.UsuarioId,
                        UsuarioNombre = p.Usuario?.NombreCompleto ?? "Desconocido",
                        EjemplarId = p.EjemplarId,
                        EjemplarCodigoBarras = p.Ejemplar?.CodigoBarras ?? "Desconocido",
                        LibroTitulo = p.Ejemplar?.Libro?.Titulo ?? "Desconocido",
                        FechaSalida = p.FechaSalida,
                        FechaMaxDevolucion = p.FechaMaxDevolucion,
                        FechaDevolucionEfectiva = p.FechaDevolucionEfectiva,
                        Estado = dtoState
                    };
                }).ToList();

                return Ok(new PaginatedPrestamos
                {
                    TotalItems = total,
                    Page = pNum,
                    PageSize = pSize,
                    TotalPages = (int)Math.Ceiling((double)total / pSize),
                    Items = items
                });
            }
            catch (Exception)
            {
                return Ok(new PaginatedPrestamos { TotalItems = 0, Page = 1, PageSize = 10, TotalPages = 0, Items = new List<PrestamoDto>() });
            }
        }

        [HttpPost("prestamos")]
        [HttpPost("circulacion/prestamos")]
        public override async Task<ActionResult<PrestamoDto>> RegistrarPrestamo([FromBody] RegistrarPrestamoDto body, CancellationToken cancellationToken = default)
        {
            await EnsureSeededAsync();
            var operatorRole = await GetOperatorRoleAsync(cancellationToken);
            if (operatorRole != Rol.Administrador && operatorRole != Rol.Bibliotecario)
            {
                return StatusCode(403, new ErrorResponse { Code = "ERR_FORBIDDEN", Title = "Acceso Denegado", Detail = "Solo el personal de biblioteca autorizado (Bibliotecario o Administrador) puede realizar esta acción." });
            }

            var command = new RegistrarPrestamoCommand(body.UsuarioId, body.EjemplarId, body.FechaDevolucion?.DateTime);
            var prestamo = await _mediator.Send(command, cancellationToken);

            var dtoState = prestamo.Estado switch
            {
                EstadoPrestamo.Activo => PrestamoDtoEstado.Activo,
                EstadoPrestamo.Devuelto => PrestamoDtoEstado.Devuelto,
                _ => PrestamoDtoEstado.Demorado
            };

            var dto = new PrestamoDto
            {
                Id = prestamo.Id,
                UsuarioId = prestamo.UsuarioId,
                UsuarioNombre = prestamo.Usuario?.NombreCompleto ?? "Juan Pérez",
                EjemplarId = prestamo.EjemplarId,
                EjemplarCodigoBarras = prestamo.Ejemplar?.CodigoBarras ?? "9780134494166-C1",
                LibroTitulo = prestamo.Ejemplar?.Libro?.Titulo ?? "Clean Architecture",
                FechaSalida = prestamo.FechaSalida,
                FechaMaxDevolucion = prestamo.FechaMaxDevolucion,
                FechaDevolucionEfectiva = prestamo.FechaDevolucionEfectiva,
                Estado = dtoState
            };

            return CreatedAtAction(nameof(GetPrestamoById), new { id = dto.Id }, dto);
        }

        public override async Task<ActionResult<PrestamoDto>> GetPrestamoById(int id, CancellationToken cancellationToken = default)
        {
            await EnsureSeededAsync();
            try
            {
                var p = await _context.Prestamos
                    .Include(p => p.Usuario)
                    .Include(p => p.Ejemplar)
                    .ThenInclude(e => e.Libro)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

                if (p == null)
                {
                    return NotFound(new ErrorResponse { Code = "ERR_LOAN_NOT_FOUND", Title = "No Encontrado", Detail = "El préstamo no existe." });
                }

                var dtoState = p.Estado switch
                {
                    EstadoPrestamo.Activo => PrestamoDtoEstado.Activo,
                    EstadoPrestamo.Devuelto => PrestamoDtoEstado.Devuelto,
                    _ => PrestamoDtoEstado.Demorado
                };

                return Ok(new PrestamoDto
                {
                    Id = p.Id,
                    UsuarioId = p.UsuarioId,
                    UsuarioNombre = p.Usuario?.NombreCompleto ?? "Desconocido",
                    EjemplarId = p.EjemplarId,
                    EjemplarCodigoBarras = p.Ejemplar?.CodigoBarras ?? "Desconocido",
                    LibroTitulo = p.Ejemplar?.Libro?.Titulo ?? "Desconocido",
                    FechaSalida = p.FechaSalida,
                    FechaMaxDevolucion = p.FechaMaxDevolucion,
                    FechaDevolucionEfectiva = p.FechaDevolucionEfectiva,
                    Estado = dtoState
                });
            }
            catch
            {
                return NotFound(new ErrorResponse { Code = "ERR_LOAN_NOT_FOUND", Title = "Error", Detail = "No se pudo recuperar el préstamo." });
            }
        }

        public override async Task<ActionResult<DevolucionResponseDto>> ProcesarDevolucion([FromBody] ProcesarDevolucionDto body, CancellationToken cancellationToken = default)
        {
            var operatorRole = await GetOperatorRoleAsync(cancellationToken);
            if (operatorRole != Rol.Administrador && operatorRole != Rol.Bibliotecario)
            {
                return StatusCode(403, new ErrorResponse { Code = "ERR_FORBIDDEN", Title = "Acceso Denegado", Detail = "Solo el personal de biblioteca autorizado (Bibliotecario o Administrador) puede realizar esta acción." });
            }

            var estadoEntrega = body.EstadoEntrega switch
            {
                ProcesarDevolucionDtoEstadoEntrega.Bueno => EstadoEjemplar.Disponible,
                ProcesarDevolucionDtoEstadoEntrega.Dañado => EstadoEjemplar.Mantenimiento,
                ProcesarDevolucionDtoEstadoEntrega.Pérdida => EstadoEjemplar.Pérdida,
                _ => EstadoEjemplar.Disponible
            };

            var command = new ProcesarDevolucionCommand(body.CodigoBarras, estadoEntrega, body.Observaciones);
            var result = await _mediator.Send(command, cancellationToken);

            SancionDto? sancionDto = null;
            if (result.Sancion != null)
            {
                var sancState = result.Sancion.Estado switch
                {
                    EstadoSancion.Activa => SancionDtoEstado.Activa,
                    EstadoSancion.Expirada => SancionDtoEstado.Expirada,
                    _ => SancionDtoEstado.Levantada
                };

                sancionDto = new SancionDto
                {
                    Id = result.Sancion.Id,
                    UsuarioId = result.Sancion.UsuarioId,
                    PrestamoId = result.Sancion.PrestamoId ?? 0,
                    FechaInicio = result.Sancion.FechaInicio,
                    FechaFin = result.Sancion.FechaFin,
                    DiasSancion = result.Sancion.DiasSancion,
                    Estado = sancState
                };
            }

            var resultState = result.NuevoEstadoEjemplar switch
            {
                EstadoEjemplar.Disponible => DevolucionResponseDtoNuevoEstadoEjemplar.Disponible,
                EstadoEjemplar.Reservado => DevolucionResponseDtoNuevoEstadoEjemplar.Reservado,
                EstadoEjemplar.Mantenimiento => DevolucionResponseDtoNuevoEstadoEjemplar.Mantenimiento,
                _ => DevolucionResponseDtoNuevoEstadoEjemplar.Pérdida
            };

            var response = new DevolucionResponseDto
            {
                PrestamoId = result.PrestamoId,
                FechaDevolucionEfectiva = result.FechaDevolucionEfectiva,
                DiasRetraso = result.DiasRetraso,
                PenalizacionAplicada = result.PenalizacionAplicada,
                Sancion = sancionDto,
                NuevoEstadoEjemplar = resultState
            };

            return Ok(response);
        }

        public override async Task<ActionResult<ICollection<ReservaDto>>> GetReservas(int? libroId = null, int? usuarioId = null, CancellationToken cancellationToken = default)
        {
            var list = new List<ReservaDto>
            {
                new() { Id = 12, UsuarioId = usuarioId ?? 10, UsuarioNombre = "Juan Pérez", LibroId = libroId ?? 1, LibroTitulo = "Clean Architecture", FechaReserva = DateTimeOffset.UtcNow, PosicionCola = 1, Estado = ReservaDtoEstado.Activa }
            };
            return Ok(list);
        }

        public override async Task<ActionResult<ReservaDto>> CrearReserva([FromBody] CrearReservaDto body, CancellationToken cancellationToken = default)
        {
            var command = new CrearReservaCommand(body.UsuarioId, body.LibroId);
            var reserva = await _mediator.Send(command, cancellationToken);

            var resState = reserva.Estado switch
            {
                EstadoReserva.Activa => ReservaDtoEstado.Activa,
                EstadoReserva.Procesada => ReservaDtoEstado.Procesada,
                EstadoReserva.Cancelada => ReservaDtoEstado.Cancelada,
                _ => ReservaDtoEstado.Vencida
            };

            var dto = new ReservaDto
            {
                Id = reserva.Id,
                UsuarioId = reserva.UsuarioId,
                UsuarioNombre = reserva.Usuario?.NombreCompleto ?? "Juan Pérez",
                LibroId = reserva.LibroId,
                LibroTitulo = reserva.Libro?.Titulo ?? "Clean Architecture",
                FechaReserva = reserva.FechaReserva,
                PosicionCola = reserva.PosicionCola,
                Estado = resState
            };

            return CreatedAtAction(nameof(GetReservas), dto);
        }

        public override async Task<IActionResult> CancelarReserva(int id, CancellationToken cancellationToken = default)
        {
            return NoContent();
        }

        // ==========================================
        // 7. CONFIGURACIÓN (MOCK)
        // ==========================================
        public override async Task<ActionResult<ConfiguracionDto>> GetConfiguracion(CancellationToken cancellationToken = default)
        {
            return Ok(new ConfiguracionDto
            {
                MaxPrestamosAlumno = 3,
                MaxPrestamosDocente = 5,
                MaxPrestamosAdministrativo = 3,
                DiasPrestamoDefecto = 7,
                DiasSuspensionPorDiaRetraso = 2,
                HorasGraciaReserva = 48
            });
        }

        public override async Task<ActionResult<ConfiguracionDto>> UpdateConfiguracion([FromBody] ConfiguracionDto body, CancellationToken cancellationToken = default)
        {
            return Ok(body);
        }

        // ==========================================
        // 8. REPORTES & KPI
        // ==========================================
        // ==========================================
        // 8. REPORTES & KPI
        // ==========================================
        public override async Task<ActionResult<EstadisticasDto>> GetEstadisticas(CancellationToken cancellationToken = default)
        {
            try
            {
                int totalEjemplares = await _context.Ejemplares.CountAsync(cancellationToken);
                int prestamosActivos = await _context.Prestamos.CountAsync(p => p.Estado == EstadoPrestamo.Activo || p.Estado == EstadoPrestamo.Demorado, cancellationToken);
                int usuariosBloqueados = await _context.Usuarios.CountAsync(u => u.Estado == EstadoUsuario.BloqueoTemporal || u.Estado == EstadoUsuario.Suspendido, cancellationToken);
                
                float tasaMorosidad = 0f;
                if (prestamosActivos > 0)
                {
                    int demorados = await _context.Prestamos.CountAsync(p => p.Estado == EstadoPrestamo.Demorado || (p.Estado == EstadoPrestamo.Activo && p.FechaMaxDevolucion < DateTime.UtcNow), cancellationToken);
                    tasaMorosidad = (float)Math.Round((double)demorados / prestamosActivos * 100, 1);
                }

                return Ok(new EstadisticasDto
                {
                    TotalEjemplares = totalEjemplares,
                    PrestamosActivos = prestamosActivos,
                    TasaMorosidad = tasaMorosidad,
                    UsuariosBloqueados = usuariosBloqueados
                });
            }
            catch (Exception)
            {
                return Ok(new EstadisticasDto
                {
                    TotalEjemplares = 0,
                    PrestamosActivos = 0,
                    TasaMorosidad = 0.0f,
                    UsuariosBloqueados = 0
                });
            }
        }

        public override async Task<ActionResult<ICollection<ReporteMasSolicitadoDto>>> GetMasSolicitados(CancellationToken cancellationToken = default)
        {
            try
            {
                var popularList = await _context.Prestamos
                    .Where(p => p.Ejemplar != null && p.Ejemplar.Libro != null)
                    .GroupBy(p => new { p.Ejemplar.Libro.Id, p.Ejemplar.Libro.Titulo, AutorNombre = p.Ejemplar.Libro.Autor != null ? p.Ejemplar.Libro.Autor.Nombre : "Desconocido" })
                    .Select(g => new ReporteMasSolicitadoDto
                    {
                        LibroId = g.Key.Id,
                        Titulo = g.Key.Titulo,
                        AutorNombre = g.Key.AutorNombre,
                        TotalPrestamos = g.Count()
                    })
                    .OrderByDescending(x => x.TotalPrestamos)
                    .Take(5)
                    .ToListAsync(cancellationToken);

                for (int i = 0; i < popularList.Count; i++)
                {
                    popularList[i].Rank = i + 1;
                }

                return Ok((ICollection<ReporteMasSolicitadoDto>)popularList);
            }
            catch (Exception)
            {
                return Ok((ICollection<ReporteMasSolicitadoDto>)new List<ReporteMasSolicitadoDto>());
            }
        }

        public override async Task<ActionResult<ICollection<ReporteEjemplarProblemaDto>>> GetMantenimientoPerdidos(CancellationToken cancellationToken = default)
        {
            try
            {
                var problematicList = await _context.Ejemplares
                    .Include(e => e.Libro)
                    .Where(e => e.Estado == EstadoEjemplar.Mantenimiento || e.Estado == EstadoEjemplar.Pérdida)
                    .ToListAsync(cancellationToken);

                var list = problematicList.Select(e => new ReporteEjemplarProblemaDto
                {
                    EjemplarId = e.Id,
                    CodigoBarras = e.CodigoBarras,
                    LibroTitulo = e.Libro?.Titulo ?? "Desconocido",
                    Estado = e.Estado == EstadoEjemplar.Mantenimiento ? ReporteEjemplarProblemaDtoEstado.Mantenimiento : ReporteEjemplarProblemaDtoEstado.Pérdida,
                    Observaciones = string.IsNullOrEmpty(e.Observaciones) ? "Ninguna registrada" : e.Observaciones,
                    UltimaFechaActualizacion = DateTimeOffset.UtcNow
                }).ToList();

                return Ok((ICollection<ReporteEjemplarProblemaDto>)list);
            }
            catch (Exception)
            {
                return Ok((ICollection<ReporteEjemplarProblemaDto>)new List<ReporteEjemplarProblemaDto>());
            }
        }

        public override async Task<ActionResult<ICollection<ReporteUsuarioMorosoDto>>> GetUsuariosMorosos(CancellationToken cancellationToken = default)
        {
            try
            {
                var morosoUsers = await _context.Usuarios
                    .Include(u => u.Prestamos)
                    .Include(u => u.Sanciones)
                    .Where(u => u.Estado == EstadoUsuario.BloqueoTemporal || 
                                u.Estado == EstadoUsuario.Suspendido ||
                                u.Prestamos.Any(p => (p.Estado == EstadoPrestamo.Activo || p.Estado == EstadoPrestamo.Demorado) && p.FechaMaxDevolucion < DateTime.UtcNow))
                    .ToListAsync(cancellationToken);

                var list = morosoUsers.Select(u => new ReporteUsuarioMorosoDto
                {
                    UsuarioId = u.Id,
                    Dni = u.Dni,
                    NombreCompleto = u.NombreCompleto,
                    Email = u.Email,
                    PrestamosVencidosCount = u.Prestamos.Count(p => (p.Estado == EstadoPrestamo.Activo || p.Estado == EstadoPrestamo.Demorado) && p.FechaMaxDevolucion < DateTime.UtcNow),
                    SancionesActivasCount = u.Sanciones.Count(s => s.Estado == EstadoSancion.Activa),
                    EstadoUsuario = u.Estado switch
                    {
                        EstadoUsuario.Activo => "Activo",
                        EstadoUsuario.BloqueoTemporal => "BloqueoTemporal",
                        EstadoUsuario.Suspendido => "Suspendido",
                        _ => "Inactivo"
                    }
                }).ToList();

                return Ok((ICollection<ReporteUsuarioMorosoDto>)list);
            }
            catch (Exception)
            {
                return Ok((ICollection<ReporteUsuarioMorosoDto>)new List<ReporteUsuarioMorosoDto>());
            }
        }
    }
}
