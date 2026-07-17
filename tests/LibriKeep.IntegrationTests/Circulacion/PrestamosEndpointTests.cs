using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Xunit;
using LibriKeep.Infrastructure.Persistence.Context;
using LibriKeep.Core.Domain.Entities;
using LibriKeep.Core.Domain.Enums;
using LibriKeep.Presentation.API.Controllers;

namespace LibriKeep.IntegrationTests.Circulacion
{
    public class PrestamosEndpointTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public PrestamosEndpointTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task RegistrarPrestamo_Endpoint_Should_CreateLoan_And_UpdateExemplarState()
        {
            // Arrange - Obtener cliente HTTP
            var client = _factory.CreateClient();

            // Seed database inside the isolated scope of the WebApplicationFactory
            int usuarioId;
            int ejemplarId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<LibriKeepDbContext>();
                
                // Limpiar base de datos para asegurar un estado limpio
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();

                // Crear datos base
                var autor = new Autor("Robert C. Martin", "USA");
                var categoria = new Categoria("Ingeniería", "Desarrollo");
                var editorial = new Editorial("Prentice Hall");

                context.Autores.Add(autor);
                context.Categorias.Add(categoria);
                context.Editoriales.Add(editorial);
                await context.SaveChangesAsync();

                var libro = new Libro("Clean Code", "9780132350884", autor.Id, categoria.Id, editorial.Id, DateTime.UtcNow.AddYears(-15), "Español", "USA", TipoMaterial.LibroFisico);
                context.Libros.Add(libro);
                await context.SaveChangesAsync();

                var ejemplar = new Ejemplar(libro.Id, "9780132350884-C1", "Shelf B", "Nuevo");
                context.Ejemplares.Add(ejemplar);

                // Crear Usuario Bibliotecario (para autorizar la petición HTTP)
                var bibliotecario = new Usuario("79998887", "Maria Gomez", "maria.gomez@uni.edu.pe", "password", "+51999999999", TipoMiembro.Docente, Rol.Bibliotecario);
                context.Usuarios.Add(bibliotecario);

                // Crear Usuario Lector (quien recibirá el préstamo)
                var lector = new Usuario("71234567", "Juan Pérez", "alumno@uni.edu.pe", "password", "+51999888777", TipoMiembro.Alumno, Rol.Lector);
                context.Usuarios.Add(lector);

                await context.SaveChangesAsync();

                usuarioId = lector.Id;
                ejemplarId = ejemplar.Id;
            }

            // Configurar cabecera de autorización simulando el token del bibliotecario
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "mock-jwt-token-for-maria.gomez@uni.edu.pe");

            var requestBody = new
            {
                usuarioId = usuarioId,
                ejemplarId = ejemplarId,
                fechaDevolucion = DateTimeOffset.UtcNow.AddDays(7)
            };

            // Act - Enviar solicitud HTTP POST
            var response = await client.PostAsJsonAsync("/api/prestamos", requestBody);

            // Assert - Comprobar código de respuesta HTTP exitoso (201 Created)
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            // Validar respuesta del body
            var responseBody = await response.Content.ReadFromJsonAsync<PrestamoDto>();
            responseBody.Should().NotBeNull();
            responseBody!.Id.Should().BeGreaterThan(0);

            // Validar cambios persistidos en la base de datos
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<LibriKeepDbContext>();
                
                // Validar que el préstamo fue registrado en la base de datos
                var prestamo = await context.Prestamos
                    .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId && p.EjemplarId == ejemplarId);
                
                prestamo.Should().NotBeNull();
                prestamo!.Estado.Should().Be(EstadoPrestamo.Activo);

                // Validar que se "descontó" el stock marcando el ejemplar como Prestado (RN-01)
                var ejemplar = await context.Ejemplares.FindAsync(ejemplarId);
                ejemplar.Should().NotBeNull();
                ejemplar!.Estado.Should().Be(EstadoEjemplar.Prestado);
            }
        }
    }
}
