using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Xunit;
using LibriKeep.Core.Application.Circulacion.Commands.RegistrarPrestamo;
using LibriKeep.Core.Application.Circulacion.Commands.ProcesarDevolucion;
using LibriKeep.Core.Application.Common.Interfaces;
using LibriKeep.Core.Domain.Entities;
using LibriKeep.Core.Domain.Enums;
using LibriKeep.Core.Domain.Exceptions;
using LibriKeep.Infrastructure.Persistence.Context;
using LibriKeep.Infrastructure.Persistence.Repositories;

namespace LibriKeep.IntegrationTests.Circulacion
{
    public class CirculacionIntegrationTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<LibriKeepDbContext> _contextOptions;

        public CirculacionIntegrationTests()
        {
            // Usar SQLite en memoria para simular transacciones ACID reales y tokens de concurrencia
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            _contextOptions = new DbContextOptionsBuilder<LibriKeepDbContext>()
                .UseSqlite(_connection)
                .Options;

            // Inicializar esquema
            using var context = new LibriKeepDbContext(_contextOptions);
            context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
        }

        private async Task SeedDatabaseAsync()
        {
            using var context = new LibriKeepDbContext(_contextOptions);

            // Crear Autor, Categoria, Editorial
            var autor = new Autor("Robert C. Martin", "USA");
            var categoria = new Categoria("Ingeniería", "Desarrollo");
            var editorial = new Editorial("Prentice");
            
            context.Autores.Add(autor);
            context.Categorias.Add(categoria);
            context.Editoriales.Add(editorial);
            await context.SaveChangesAsync();

            // Crear Libro y Ejemplar
            var libro = new Libro("Clean Architecture", "9780134494166", autor.Id, categoria.Id, editorial.Id, DateTime.UtcNow.AddYears(-5), "Español", "USA", TipoMaterial.LibroFisico);
            context.Libros.Add(libro);
            await context.SaveChangesAsync();

            var ejemplar = new Ejemplar(libro.Id, "9780134494166-C1", "Shelf A", "New");
            context.Ejemplares.Add(ejemplar);

            // Crear Usuario Lector
            var usuario = new Usuario("71234567", "Juan Pérez", "alumno@uni.edu.pe", "pwd", "123", TipoMiembro.Alumno, Rol.Lector);
            context.Usuarios.Add(usuario);

            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task RegistrarPrestamo_Integration_Should_Save_To_Database_And_Update_Exemplar_State()
        {
            // Arrange
            await SeedDatabaseAsync();
            using var context = new LibriKeepDbContext(_contextOptions);

            var usuario = await context.Usuarios.FirstAsync();
            var ejemplar = await context.Ejemplares.FirstAsync();

            var usuarioRepo = new UsuarioRepository(context);
            var ejemplarRepo = new EjemplarRepository(context);
            var prestamoRepo = new PrestamoRepository(context);
            var uow = new UnitOfWork(context);

            var handler = new RegistrarPrestamoCommandHandler(usuarioRepo, ejemplarRepo, prestamoRepo, uow);
            var command = new RegistrarPrestamoCommand(usuario.Id, ejemplar.Id);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);

            // Verificar persistencia real en DB
            using var verificationContext = new LibriKeepDbContext(_contextOptions);
            var persistedLoan = await verificationContext.Prestamos.FindAsync(result.Id);
            persistedLoan.Should().NotBeNull();
            persistedLoan!.Estado.Should().Be(EstadoPrestamo.Activo);

            var persistedEjemplar = await verificationContext.Ejemplares.FindAsync(ejemplar.Id);
            persistedEjemplar!.Estado.Should().Be(EstadoEjemplar.Prestado);
        }

        [Fact]
        public async Task Devolucion_Overdue_Integration_Should_Apply_Sancion_In_Database_And_Block_User()
        {
            // Arrange
            await SeedDatabaseAsync();
            using var context = new LibriKeepDbContext(_contextOptions);

            var usuario = await context.Usuarios.FirstAsync();
            var ejemplar = await context.Ejemplares.FirstAsync();
            ejemplar.Prestar(); // Ejemplar ahora prestado

            // Crear préstamo atrasado (venció hace 5 días)
            var prestamo = new Prestamo(usuario.Id, ejemplar.Id, DateTime.UtcNow.AddDays(-12), 7);
            context.Prestamos.Add(prestamo);
            await context.SaveChangesAsync();

            var prestamoRepo = new PrestamoRepository(context);
            var reservaRepo = new ReservaRepository(context);
            var sancionRepo = new SancionRepository(context);
            var uow = new UnitOfWork(context);

            var handler = new ProcesarDevolucionCommandHandler(prestamoRepo, reservaRepo, sancionRepo, uow);
            var command = new ProcesarDevolucionCommand(ejemplar.CodigoBarras, EstadoEjemplar.Disponible, "Devuelto tarde");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.DiasRetraso.Should().Be(5); // 12 días - 7 días = 5 días retraso
            result.PenalizacionAplicada.Should().BeTrue();
            result.Sancion.Should().NotBeNull();

            // Verificar base de datos
            using var verificationContext = new LibriKeepDbContext(_contextOptions);
            var dbSancion = await verificationContext.Sanciones.FirstOrDefaultAsync(s => s.UsuarioId == usuario.Id);
            dbSancion.Should().NotBeNull();
            dbSancion!.DiasSancion.Should().Be(10); // 5 días retraso * 2 = 10 días sanción

            var dbUsuario = await verificationContext.Usuarios.FindAsync(usuario.Id);
            dbUsuario!.Estado.Should().Be(EstadoUsuario.BloqueoTemporal); // Bloqueado en base de datos
        }

        [Fact]
        public async Task Concurrent_RegistrarPrestamo_Should_Throw_DbUpdateConcurrencyException_Under_Race_Conditions()
        {
            // Arrange
            await SeedDatabaseAsync();
            
            // Simular dos hilos obteniendo el mismo ejemplar disponible
            using var context1 = new LibriKeepDbContext(_contextOptions);
            using var context2 = new LibriKeepDbContext(_contextOptions);

            var usuario1 = await context1.Usuarios.FirstAsync();
            var ejemplar1 = await context1.Ejemplares.FirstAsync();

            var usuario2 = await context2.Usuarios.FirstAsync();
            var ejemplar2 = await context2.Ejemplares.FirstAsync();

            // Transacción 1: Alumno 1 presta ejemplar
            var handler1 = new RegistrarPrestamoCommandHandler(
                new UsuarioRepository(context1), new EjemplarRepository(context1), new PrestamoRepository(context1), new UnitOfWork(context1));
            
            // Transacción 2: Alumno 2 presta el MISMO ejemplar en paralelo
            var handler2 = new RegistrarPrestamoCommandHandler(
                new UsuarioRepository(context2), new EjemplarRepository(context2), new PrestamoRepository(context2), new UnitOfWork(context2));

            // Act - Hilo 1 completa y guarda cambios de forma atómica
            await handler1.Handle(new RegistrarPrestamoCommand(usuario1.Id, ejemplar1.Id), CancellationToken.None);

            // Act - Hilo 2 intenta guardar cambios del ejemplar que ya ha sido modificado
            Func<Task> act2 = async () => await handler2.Handle(new RegistrarPrestamoCommand(usuario2.Id, ejemplar2.Id), CancellationToken.None);

            // Assert - Debe fallar debido a violación de concurrencia (Concurrency Token en Estado)
            await act2.Should().ThrowAsync<DbUpdateConcurrencyException>();
        }
    }
}
