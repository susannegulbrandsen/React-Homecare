using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HomeCareApp.Controllers;
using HomeCareApp.DTOs;
using HomeCareApp.Models;
using HomeCareApp.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Homecare.Api.Tests.Controllers
{
    public class AppointmentControllerTests
    {
        private readonly Mock<IAppointmentRepository> _appointmentRepoMock;
        private readonly Mock<INotificationRepository> _notificationRepoMock;
        private readonly Mock<ILogger<AppointmentController>> _loggerMock;
        private readonly AppointmentController _controller;

        public AppointmentControllerTests()
        {
            _appointmentRepoMock = new Mock<IAppointmentRepository>();
            _notificationRepoMock = new Mock<INotificationRepository>();
            _loggerMock = new Mock<ILogger<AppointmentController>>();

            _controller = new AppointmentController(
                _appointmentRepoMock.Object,
                _notificationRepoMock.Object,
                _loggerMock.Object
            );

            // Setup fake HttpContext with authenticated user
            var httpContext = new DefaultHttpContext();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "employee-user-id"),
                new Claim(ClaimTypes.Role, "Employee")
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Avoid null reference exceptions for notification creation
            _notificationRepoMock
                .Setup(r => r.CreateAsync(It.IsAny<Notification>()))
                .ReturnsAsync(true);
        }

        /// <summary>
        /// Helper to create a test appointment with default or specified values
        /// </summary>
        private Appointment CreateTestAppointment(
            int id = 1,
            string subject = "Test",
            int patientId = 10,
            int employeeId = 20,
            string patientUserId = "patient-user-id",
            string employeeUserId = "employee-user-id")
        {
            return new Appointment
            {
                AppointmentId = id,
                Subject = subject,
                Date = DateTime.Now.AddDays(1), // avoid failing due to past date validation
                PatientId = patientId,
                EmployeeId = employeeId,
                Patient = new Patient
                {
                    PatientId = patientId,
                    UserId = patientUserId,
                    FullName = "Test Patient",
                    User = null!,
                    Appointments = new List<Appointment>()
                },
                Employee = new Employee
                {
                    EmployeeId = employeeId,
                    UserId = employeeUserId,
                    FullName = "Test Employee",
                    User = null!,
                    Appointments = new List<Appointment>()
                },
                IsConfirmed = true
            };
        }

        // ------------------- GET ALL -------------------

        // Positive: returns Ok with a list when appointments exist
        [Fact]
        public async Task GetAppointments_ReturnsOk_WhenAppointmentsExist()
        {
            // Arrange
            var appointments = new List<Appointment>
            {
                CreateTestAppointment(1, "Checkup"),
                CreateTestAppointment(2, "Follow-up")
            };

            _appointmentRepoMock
                .Setup(r => r.GetAll())
                .ReturnsAsync(appointments);

            // Act
            var result = await _controller.GetAppointments();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var dtos = Assert.IsAssignableFrom<IEnumerable<AppointmentDto>>(okResult.Value);
            Assert.Equal(2, dtos.Count());
        }

        // Negative: returns NotFound when no appointments exist
        [Fact]
        public async Task GetAppointments_ReturnsNotFound_WhenNoAppointmentsExist()
        {
            // Arrange
            _appointmentRepoMock
                .Setup(r => r.GetAll())
                .ReturnsAsync(new List<Appointment>());

            // Act
            var result = await _controller.GetAppointments();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Appointment list not found", notFoundResult.Value);
        }

        // ------------------- GET BY ID -------------------

        // Positive: returns Ok with appointment when it exists
        [Fact]
        public async Task GetAppointmentById_ReturnsOk_WhenAppointmentExists()
        {
            // Arrange
            var appointment = CreateTestAppointment(1, "Checkup");

            _appointmentRepoMock
                .Setup(r => r.GetAppointmentById(1))
                .ReturnsAsync(appointment);

            // Act
            var result = await _controller.GetAppointmentById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<AppointmentDto>(okResult.Value);
            Assert.Equal(1, dto.AppointmentId);
            Assert.Equal("Checkup", dto.Subject);
        }

        // Negative: returns NotFound when appointment does not exist
        [Fact]
        public async Task GetAppointmentById_ReturnsNotFound_WhenAppointmentDoesNotExist()
        {
            // Arrange
            _appointmentRepoMock
                .Setup(r => r.GetAppointmentById(1))
                .ReturnsAsync((Appointment?)null);

            // Act
            var result = await _controller.GetAppointmentById(1);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Appointment with ID 1 not found", notFoundResult.Value);
        }

        // ------------------- GET BY PATIENT -------------------

        // Positive: returns Ok with appointments for given patient
        [Fact]
        public async Task GetAppointmentsByPatientId_ReturnsOnlyAppointmentsForThatPatient()
        {
            // Arrange
            var appointments = new List<Appointment>
            {
                CreateTestAppointment(1, "A", patientId: 10),
                CreateTestAppointment(2, "B", patientId: 10),
                CreateTestAppointment(3, "C", patientId: 99)
            };

            _appointmentRepoMock
                .Setup(r => r.GetAll())
                .ReturnsAsync(appointments);

            // Act
            var result = await _controller.GetAppointmentsByPatientId(10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var dtos = Assert.IsAssignableFrom<IEnumerable<AppointmentDto>>(okResult.Value);
            Assert.Equal(2, dtos.Count());
            Assert.All(dtos, d => Assert.Equal(10, d.PatientId));
        }

        // ------------------- CREATE -------------------

        // Negative: returns BadRequest when dto is null
        [Fact]
        public async Task Create_ReturnsBadRequest_WhenDtoIsNull()
        {
            // Act
            var result = await _controller.Create(null!);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Appointment cannot be null", badRequest.Value);
        }

        // Negative: returns BadRequest when date is in the past
        [Fact]
        public async Task Create_ReturnsBadRequest_WhenDateInPast()
        {
            // Arrange
            var dto = new AppointmentDto
            {
                AppointmentId = 1,
                Subject = "Past Appointment",
                Date = DateTime.Now.AddDays(-1) // use past date to trigger validation
            };

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Appointment date cannot be in the past", badRequest.Value);
        }

        // Positive: returns Created when repository creates successfully
        [Fact]
        public async Task Create_ReturnsCreated_WhenRepositoryReturnsTrue()
        {
            // Arrange
            var dto = new AppointmentDto
            {
                AppointmentId = 1,
                Subject = "New Appointment",
                Date = DateTime.Now.AddDays(1) // avoid failing due to past date validation
            };

            var createdAppointment = CreateTestAppointment(1, "New Appointment");

            _appointmentRepoMock
                .Setup(r => r.Create(It.IsAny<Appointment>()))
                .ReturnsAsync(true);

            _appointmentRepoMock
                .Setup(r => r.GetAppointmentById(It.IsAny<int>()))
                .ReturnsAsync(createdAppointment);

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(AppointmentController.GetAppointments), createdResult.ActionName);
            var returnedAppointment = Assert.IsType<Appointment>(createdResult.Value);
            Assert.Equal("New Appointment", returnedAppointment.Subject);
        }

        // Negative: returns InternalServerError when repository fails to create
        [Fact]
        public async Task Create_ReturnsInternalServerError_WhenRepositoryReturnsFalse()
        {
            // Arrange
            var dto = new AppointmentDto
            {
                AppointmentId = 1,
                Subject = "New Appointment",
                Date = DateTime.Now.AddDays(1) // avoid failing due to past date validation
            };

            _appointmentRepoMock
                .Setup(r => r.Create(It.IsAny<Appointment>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            Assert.Equal("Internal server error", objectResult.Value);
        }

        // ------------------- UPDATE -------------------

        // Negative: returns BadRequest when dto is null
        [Fact]
        public async Task Update_ReturnsBadRequest_WhenDtoIsNull()
        {
            // Act
            var result = await _controller.Update(1, null!);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Appointment data cannot be null", badRequest.Value);
        }

        // Negative: returns BadRequest when date is in the past
        [Fact]
        public async Task Update_ReturnsBadRequest_WhenDateInPast()
        {
            // Arrange
            var existing = CreateTestAppointment(1, "Old");

            _appointmentRepoMock
                .Setup(r => r.GetAppointmentById(1))
                .ReturnsAsync(existing);

            var dto = new AppointmentDto
            {
                AppointmentId = 1,
                Subject = "Updated",
                Date = DateTime.Now.AddDays(-1) // past date to trigger validation
            };

            // Act
            var result = await _controller.Update(1, dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Appointment date cannot be in the past", badRequest.Value);
        }

        // Negative: returns NotFound when appointment does not exist
        [Fact]
        public async Task Update_ReturnsNotFound_WhenAppointmentDoesNotExist()
        {
            // Arrange
            _appointmentRepoMock
                .Setup(r => r.GetAppointmentById(1))
                .ReturnsAsync((Appointment?)null);

            var dto = new AppointmentDto
            {
                AppointmentId = 1,
                Subject = "Updated Subject",
                Date = DateTime.Now.AddDays(1) // avoid failing due to past date validation
            };

            // Act
            var result = await _controller.Update(1, dto);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Appointment not found", notFound.Value);
        }

        // Positive: returns Ok when update is successful
        [Fact]
        public async Task Update_ReturnsOk_WhenUpdateSuccessful()
        {
            // Arrange
            var existingAppointment = CreateTestAppointment(1, "Old Subject");

            var dto = new AppointmentDto
            {
                AppointmentId = 1,
                Subject = "Updated Subject",
                Date = DateTime.Now.AddDays(1) // avoid failing due to past date validation
            };

            _appointmentRepoMock
                .Setup(r => r.GetAppointmentById(1))
                .ReturnsAsync(existingAppointment);

            _appointmentRepoMock
                .Setup(r => r.Update(It.IsAny<Appointment>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Update(1, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedDto = Assert.IsType<AppointmentDto>(okResult.Value);
            Assert.Equal("Updated Subject", updatedDto.Subject);
        }

        // Negative: returns InternalServerError when update fails
        [Fact]
        public async Task Update_ReturnsInternalServerError_WhenUpdateFails()
        {
            // Arrange
            var existingAppointment = CreateTestAppointment(1, "Old Subject");

            var dto = new AppointmentDto
            {
                AppointmentId = 1,
                Subject = "Updated Subject",
                Date = DateTime.Now.AddDays(1) // avoid failing due to past date validation
            };

            _appointmentRepoMock
                .Setup(r => r.GetAppointmentById(1))
                .ReturnsAsync(existingAppointment);

            _appointmentRepoMock
                .Setup(r => r.Update(It.IsAny<Appointment>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Update(1, dto);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            Assert.Equal("Internal server error", objectResult.Value);
        }

        // ------------------- DELETE -------------------

        // Negative: returns NotFound when appointment does not exist
        [Fact]
        public async Task Delete_ReturnsNotFound_WhenAppointmentDoesNotExist()
        {
            // Arrange
            _appointmentRepoMock
                .Setup(r => r.GetAppointmentById(1))
                .ReturnsAsync((Appointment?)null);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Appointment not found", notFound.Value);
        }

        // Positive: returns NoContent when delete is successful
        [Fact]
        public async Task Delete_ReturnsNoContent_WhenDeleteSuccessful()
        {
            // Arrange
            var appointment = CreateTestAppointment(1, "To delete");

            _appointmentRepoMock
                .Setup(r => r.GetAppointmentById(1))
                .ReturnsAsync(appointment);

            _appointmentRepoMock
                .Setup(r => r.Delete(1))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        // Negative: returns BadRequest when delete fails
        [Fact]
        public async Task Delete_ReturnsBadRequest_WhenDeleteFails()
        {
            // Arrange
            var appointment = CreateTestAppointment(1, "To delete");

            _appointmentRepoMock
                .Setup(r => r.GetAppointmentById(1))
                .ReturnsAsync(appointment);

            _appointmentRepoMock
                .Setup(r => r.Delete(1))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Appointment deletion failed", badRequest.Value);
        }
    }
}
