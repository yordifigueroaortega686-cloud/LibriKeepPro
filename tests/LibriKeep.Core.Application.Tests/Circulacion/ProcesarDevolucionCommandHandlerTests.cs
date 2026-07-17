using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using LibriKeep.Core.Application.Circulacion.Commands.ProcesarDevolucion;
using LibriKeep.Core.Application.Common.Interfaces;
using LibriKeep.Core.Domain.Entities;
using LibriKeep.Core.Domain.Enums;
using LibriKeep.Core.Domain.Exceptions;

namespace LibriKeep.Core.Application.Tests.Circulacion
{
    public class ProcesarDevolucionCommandHandlerTests
    {
        private readonly Mock<IPrestamoRepository> _prestamoRepoMock;
        private readonly Mock<IReservaRepository> _reservaRepoMock;
        private readonly Mock<ISancionRepository> _sancionRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly ProcesarDevolucionCommandHandler _handler;

        public ProcesarDevolucionCommandHandlerTests()
        {
            _prestamoRepoMock = new Mock<IPrestamoRepository>();
            _reservaRepoMock = new Mock<IReservaRepository>();
            _sancionRepoMock = new Mock<ISancionRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _handler = new ProcesarDevolucionCommandHandler(
                _prestamoRepoMock.Object,
                _reservaRepoMock.Object,
                _sancionRepoMock.Object,
                _uowMock.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Process_Return_Without_Sancion_When_Not_Overdue()
        {
            // Arrange
            var usuario = new Usuario("71234567", "Juan Pérez", "alumno@uni.edu.pe", "pwd", "123", TipoMiembro.Alumno, Rol.Lector);
            var ejemplar = new Ejemplar(1, "9780134494166-C1", "Shelf", "Obs");
            ejemplar.Prestar(); // Cambia a estado Prestado

            var prestamo = new Prestamo(usuario.Id, ejemplar.Id, DateTime.UtcNow.AddDays(-2), 7); // Plazo de 7 días, vencimiento en +5 días
            
            // Usamos reflexión para setear la propiedad Ejemplar de navegación si es necesario,
            // pero el constructor de Prestamo no la asigna automáticamente si la pasamos por ID.
            // Para simplificar el test en memoria, asignamos las propiedades de navegación directamente por setter privado o reflexión,
            // o simulamos que EF Core ya las resolvió. Vamos a usar reflexión para inyectar Ejemplar en Prestamo.
            typeof(Prestamo).GetProperty("Ejemplar")?.SetValue(prestamo, ejemplar);
            typeof(Prestamo).GetProperty("Usuario")?.SetValue(prestamo, usuario);

            _prestamoRepoMock.Setup(r => r.GetActiveLoanByCodigoBarrasAsync("9780134494166-C1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(prestamo);
            _reservaRepoMock.Setup(r => r.HasActiveReservationsForLibroAsync(ejemplar.LibroId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var command = new ProcesarDevolucionCommand("9780134494166-C1", EstadoEjemplar.Disponible, "Devolución normal");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.PrestamoId.Should().Be(prestamo.Id);
            result.DiasRetraso.Should().Be(0);
            result.PenalizacionAplicada.Should().BeFalse();
            result.Sancion.Should().BeNull();
            ejemplar.Estado.Should().Be(EstadoEjemplar.Disponible);

            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _uowMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Apply_Sancion_When_Overdue()
        {
            // Arrange
            var usuario = new Usuario("71234567", "Juan Pérez", "alumno@uni.edu.pe", "pwd", "123", TipoMiembro.Alumno, Rol.Lector);
            var ejemplar = new Ejemplar(1, "9780134494166-C1", "Shelf", "Obs");
            ejemplar.Prestar();

            // Préstamo iniciado hace 10 días, con plazo de 7 días (venció hace 3 días)
            var prestamo = new Prestamo(usuario.Id, ejemplar.Id, DateTime.UtcNow.AddDays(-10), 7);
            typeof(Prestamo).GetProperty("Ejemplar")?.SetValue(prestamo, ejemplar);
            typeof(Prestamo).GetProperty("Usuario")?.SetValue(prestamo, usuario);

            _prestamoRepoMock.Setup(r => r.GetActiveLoanByCodigoBarrasAsync("9780134494166-C1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(prestamo);
            _reservaRepoMock.Setup(r => r.HasActiveReservationsForLibroAsync(ejemplar.LibroId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var command = new ProcesarDevolucionCommand("9780134494166-C1", EstadoEjemplar.Disponible, "Retrasado");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.DiasRetraso.Should().Be(3); // 10 días - 7 días = 3 días de retraso
            result.PenalizacionAplicada.Should().BeTrue();
            result.Sancion.Should().NotBeNull();
            result.Sancion!.DiasSancion.Should().Be(6); // 3 días * 2 = 6 días de sanción
            usuario.Estado.Should().Be(EstadoUsuario.BloqueoTemporal); // Usuario bloqueado por morosidad

            _sancionRepoMock.Verify(r => r.AddAsync(It.IsAny<Sancion>(), It.IsAny<CancellationToken>()), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Transition_To_Reservado_When_Reservations_Exist()
        {
            // Arrange
            var usuario = new Usuario("71234567", "Juan Pérez", "alumno@uni.edu.pe", "pwd", "123", TipoMiembro.Alumno, Rol.Lector);
            var ejemplar = new Ejemplar(1, "9780134494166-C1", "Shelf", "Obs");
            ejemplar.Prestar();

            var prestamo = new Prestamo(usuario.Id, ejemplar.Id, DateTime.UtcNow.AddDays(-2), 7);
            typeof(Prestamo).GetProperty("Ejemplar")?.SetValue(prestamo, ejemplar);
            typeof(Prestamo).GetProperty("Usuario")?.SetValue(prestamo, usuario);

            var reservaUsuario = new Usuario("77777777", "Lector Espera", "espera@uni.edu.pe", "pwd", "123", TipoMiembro.Alumno, Rol.Lector);
            var reserva = new Reserva(reservaUsuario.Id, ejemplar.LibroId, DateTime.UtcNow, 1);

            _prestamoRepoMock.Setup(r => r.GetActiveLoanByCodigoBarrasAsync("9780134494166-C1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(prestamo);
            _reservaRepoMock.Setup(r => r.HasActiveReservationsForLibroAsync(ejemplar.LibroId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _reservaRepoMock.Setup(r => r.GetFirstActiveReservationForLibroAsync(ejemplar.LibroId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(reserva);

            var command = new ProcesarDevolucionCommand("9780134494166-C1", EstadoEjemplar.Disponible, "Con reserva activa");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.NuevoEstadoEjemplar.Should().Be(EstadoEjemplar.Reservado); // Transiciona a Reservado por prioridad
            reserva.Estado.Should().Be(EstadoReserva.Procesada); // Primera reserva despachada

            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
