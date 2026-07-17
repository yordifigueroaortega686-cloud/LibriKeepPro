using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using LibriKeep.Core.Application.Circulacion.Commands.RegistrarPrestamo;
using LibriKeep.Core.Application.Common.Interfaces;
using LibriKeep.Core.Domain.Entities;
using LibriKeep.Core.Domain.Enums;
using LibriKeep.Core.Domain.Exceptions;

namespace LibriKeep.Core.Application.Tests.Circulacion
{
    public class RegistrarPrestamoCommandHandlerTests
    {
        private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
        private readonly Mock<IEjemplarRepository> _ejemplarRepoMock;
        private readonly Mock<IPrestamoRepository> _prestamoRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly RegistrarPrestamoCommandHandler _handler;

        public RegistrarPrestamoCommandHandlerTests()
        {
            _usuarioRepoMock = new Mock<IUsuarioRepository>();
            _ejemplarRepoMock = new Mock<IEjemplarRepository>();
            _prestamoRepoMock = new Mock<IPrestamoRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _handler = new RegistrarPrestamoCommandHandler(
                _usuarioRepoMock.Object,
                _ejemplarRepoMock.Object,
                _prestamoRepoMock.Object,
                _uowMock.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Create_Prestamo_When_All_Rules_Are_Satisfied()
        {
            // Arrange
            var usuario = new Usuario("71234567", "Juan Pérez", "alumno@uni.edu.pe", "pwdHash", "+51999888777", TipoMiembro.Alumno, Rol.Lector);
            var ejemplar = new Ejemplar(1, "9780134494166-C1", "Estante A", "Nuevo");

            _usuarioRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);
            _ejemplarRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ejemplar);
            _prestamoRepoMock.Setup(r => r.GetActiveLoansCountForUsuarioAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            var command = new RegistrarPrestamoCommand(10, 1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.UsuarioId.Should().Be(usuario.Id);
            result.EjemplarId.Should().Be(ejemplar.Id);
            result.Estado.Should().Be(EstadoPrestamo.Activo);
            ejemplar.Estado.Should().Be(EstadoEjemplar.Prestado);

            _prestamoRepoMock.Verify(r => r.AddAsync(It.IsAny<Prestamo>(), It.IsAny<CancellationToken>()), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _uowMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Throw_Exception_When_Exemplar_Is_Not_Disponible()
        {
            // Arrange
            var usuario = new Usuario("71234567", "Juan Pérez", "alumno@uni.edu.pe", "pwdHash", "+51999888777", TipoMiembro.Alumno, Rol.Lector);
            var ejemplar = new Ejemplar(1, "9780134494166-C1", "Estante A", "Prestado");
            ejemplar.Prestar(); // Cambiar estado a Prestado

            _usuarioRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);
            _ejemplarRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ejemplar);

            var command = new RegistrarPrestamoCommand(10, 1);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .Where(e => e.ErrorCode == "ERR_COPY_NOT_AVAILABLE");
            _uowMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Throw_Exception_When_User_Is_Sanctioned()
        {
            // Arrange
            var usuario = new Usuario("71234567", "Juan Pérez", "alumno@uni.edu.pe", "pwdHash", "+51999888777", TipoMiembro.Alumno, Rol.Lector);
            
            // Agregar una sanción activa
            var sancion = new Sancion(usuario.Id, 1, DateTime.UtcNow.AddDays(-1), 5); // Activa por 5 días
            usuario.Sanciones.Add(sancion);

            var ejemplar = new Ejemplar(1, "9780134494166-C1", "Estante A", "Nuevo");

            _usuarioRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);
            _ejemplarRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ejemplar);

            var command = new RegistrarPrestamoCommand(10, 1);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .Where(e => e.ErrorCode == "ERR_USER_SANCTIONED");
            _uowMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Throw_Exception_When_User_Exceeds_Quota()
        {
            // Arrange
            var usuario = new Usuario("71234567", "Juan Pérez", "alumno@uni.edu.pe", "pwdHash", "+51999888777", TipoMiembro.Alumno, Rol.Lector);
            
            // Simular 3 préstamos activos agregados al usuario en memoria
            var libro = new Libro("Clean Code", "9783161484100", 1, 1, 1, DateTime.UtcNow, "Español", "USA", TipoMaterial.LibroFisico);
            var ejemplar1 = new Ejemplar(1, "code-1", "A", "N");
            var ejemplar2 = new Ejemplar(1, "code-2", "A", "N");
            var ejemplar3 = new Ejemplar(1, "code-3", "A", "N");
            
            usuario.Prestamos.Add(new Prestamo(usuario.Id, ejemplar1.Id, DateTime.UtcNow, 7));
            usuario.Prestamos.Add(new Prestamo(usuario.Id, ejemplar2.Id, DateTime.UtcNow, 7));
            usuario.Prestamos.Add(new Prestamo(usuario.Id, ejemplar3.Id, DateTime.UtcNow, 7));

            var ejemplar = new Ejemplar(1, "9780134494166-C1", "Estante A", "Nuevo");

            _usuarioRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);
            _ejemplarRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ejemplar);
            _prestamoRepoMock.Setup(r => r.GetActiveLoansCountForUsuarioAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(3);

            var command = new RegistrarPrestamoCommand(10, 1);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .Where(e => e.ErrorCode == "ERR_USER_MAX_LOANS_EXCEEDED");
            _uowMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
