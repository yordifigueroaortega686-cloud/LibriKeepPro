using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using LibriKeep.Core.Application.Circulacion.Commands.CrearReserva;
using LibriKeep.Core.Application.Common.Interfaces;
using LibriKeep.Core.Domain.Entities;
using LibriKeep.Core.Domain.Enums;
using LibriKeep.Core.Domain.Exceptions;

namespace LibriKeep.Core.Application.Tests.Circulacion
{
    public class CrearReservaCommandHandlerTests
    {
        private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
        private readonly Mock<ILibroRepository> _libroRepoMock;
        private readonly Mock<IReservaRepository> _reservaRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly CrearReservaCommandHandler _handler;

        public CrearReservaCommandHandlerTests()
        {
            _usuarioRepoMock = new Mock<IUsuarioRepository>();
            _libroRepoMock = new Mock<ILibroRepository>();
            _reservaRepoMock = new Mock<IReservaRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _handler = new CrearReservaCommandHandler(
                _usuarioRepoMock.Object,
                _libroRepoMock.Object,
                _reservaRepoMock.Object,
                _uowMock.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Create_Reserva_When_Book_Is_Fully_Borrowed()
        {
            // Arrange
            var usuario = new Usuario("71234567", "Juan Pérez", "alumno@uni.edu.pe", "pwd", "123", TipoMiembro.Alumno, Rol.Lector);
            var libro = new Libro("Clean Architecture", "9780134494166", 1, 1, 1, DateTime.UtcNow, "Español", "USA", TipoMaterial.LibroFisico);
            
            // Añadir un ejemplar prestado (sin ejemplares disponibles)
            var ejemplar = new Ejemplar(libro.Id, "9780134494166-C1", "Shelf", "Obs");
            ejemplar.Prestar(); // Pasa a Prestado
            libro.Ejemplares.Add(ejemplar);

            _usuarioRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);
            _libroRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(libro);

            var command = new CrearReservaCommand(10, 1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.UsuarioId.Should().Be(usuario.Id);
            result.LibroId.Should().Be(libro.Id);
            result.PosicionCola.Should().Be(1);
            result.Estado.Should().Be(EstadoReserva.Activa);

            _reservaRepoMock.Verify(r => r.AddAsync(It.IsAny<Reserva>(), It.IsAny<CancellationToken>()), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Throw_Exception_When_Book_Has_Available_Copies()
        {
            // Arrange
            var usuario = new Usuario("71234567", "Juan Pérez", "alumno@uni.edu.pe", "pwd", "123", TipoMiembro.Alumno, Rol.Lector);
            var libro = new Libro("Clean Architecture", "9780134494166", 1, 1, 1, DateTime.UtcNow, "Español", "USA", TipoMaterial.LibroFisico);
            
            // Añadir un ejemplar disponible
            var ejemplar = new Ejemplar(libro.Id, "9780134494166-C1", "Shelf", "Obs");
            libro.Ejemplares.Add(ejemplar); // Queda disponible

            _usuarioRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);
            _libroRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(libro);

            var command = new CrearReservaCommand(10, 1);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .Where(e => e.ErrorCode == "ERR_BOOK_AVAILABLE");
            _uowMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Throw_Exception_When_User_Has_Active_Blockage()
        {
            // Arrange
            var usuario = new Usuario("71234567", "Juan Pérez", "alumno@uni.edu.pe", "pwd", "123", TipoMiembro.Alumno, Rol.Lector);
            var sancion = new Sancion(usuario.Id, null, DateTime.UtcNow.AddDays(-1), 3); // Sanción activa
            usuario.Sanciones.Add(sancion);

            var libro = new Libro("Clean Architecture", "9780134494166", 1, 1, 1, DateTime.UtcNow, "Español", "USA", TipoMaterial.LibroFisico);
            var ejemplar = new Ejemplar(libro.Id, "9780134494166-C1", "Shelf", "Obs");
            ejemplar.Prestar();
            libro.Ejemplares.Add(ejemplar);

            _usuarioRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);
            _libroRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(libro);

            var command = new CrearReservaCommand(10, 1);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .Where(e => e.ErrorCode == "ERR_USER_SANCTIONED");
            _uowMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
