using HomeCareApp.Data;
using HomeCareApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HomeCareApp.Repositories.Interfaces;
using HomeCareApp.DTOs;

namespace HomeCareApp.Controllers;


[ApiController]
[Route("api/[controller]")]
public class AppointmentController : ControllerBase
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<AppointmentController> _logger;

    public AppointmentController(IAppointmentRepository appointmentRepository, INotificationRepository notificationRepository, ILogger<AppointmentController> logger)
    {
        _appointmentRepository = appointmentRepository;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAppointments()
    {
        var appointments = await _appointmentRepository.GetAll();
        if (appointments == null)
        {
            _logger.LogError("[AppointmentController] Appointment list not found while executing _appointmentRepository.GetAll()");
            return NotFound("Appointment list not found");
        }
        var appointmentDtos = appointments.Select(appointment => new AppointmentDto
        {
            AppointmentId = appointment.AppointmentId,
            Subject = appointment.Subject,
            Description = appointment.Description,
            Date = appointment.Date,
            PatientId = appointment.PatientId ?? 0,
            EmployeeId = appointment.EmployeeId ?? 0,
            PatientName = appointment.Patient?.FullName ?? "Unknown Patient",
            EmployeeName = appointment.Employee?.FullName ?? "Unknown Employee"
        });
        return Ok(appointmentDtos);
    }

    // GET: api/appointment/patient/{patientId}
    [HttpGet("patient/{patientId}")]
    [Authorize]
    public async Task<IActionResult> GetAppointmentsByPatientId(int patientId)
    {
        var appointments = await _appointmentRepository.GetAll();
        var patientAppointments = appointments.Where(a => a.PatientId == patientId);
        
        var appointmentDtos = patientAppointments.Select(appointment => new AppointmentDto
        {
            AppointmentId = appointment.AppointmentId,
            Subject = appointment.Subject,
            Description = appointment.Description,
            Date = appointment.Date,
            PatientId = appointment.PatientId ?? 0,
            EmployeeId = appointment.EmployeeId ?? 0,
            PatientName = appointment.Patient?.FullName ?? "Unknown Patient",
            EmployeeName = appointment.Employee?.FullName ?? "Unknown Employee"
        });
        
        return Ok(appointmentDtos);
    }
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AppointmentDto appointmentDto)
    {
        if (appointmentDto == null)
        {
            return BadRequest("Appointment cannot be null");
        }
        var newAppointment = new Appointment
        {
            Subject = appointmentDto.Subject,
            Description = appointmentDto.Description,
            Date = appointmentDto.Date,
            PatientId = appointmentDto.PatientId,
            EmployeeId = appointmentDto.EmployeeId,
            Patient = null!, // Will be set by EF Core
            Employee = null! // Will be set by EF Core
        };
        bool returnOk = await _appointmentRepository.Create(newAppointment);
        if (returnOk)
        {
            // Create notifications for patient and employee
            await CreateAppointmentNotifications(newAppointment);
            
            return CreatedAtAction(nameof(GetAppointments), new { id = newAppointment.AppointmentId }, newAppointment);
        }

        _logger.LogWarning("[AppointmentController] Appointment creation failed {@appointment}", newAppointment);
        return StatusCode(500, "Internal server error");
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAppointment(int id)
    {
        var appointment = await _appointmentRepository.GetAppointmentById(id);
        if (appointment == null)
        {
            _logger.LogError("[AppointmentController] Appointment not found for the AppointmentId {AppointmentId:0000}", id);
            return NotFound("Appointment not found for the AppointmentId");
        }

        var appointmentDto = new AppointmentDto
        {
            AppointmentId = appointment.AppointmentId,
            Subject = appointment.Subject,
            Description = appointment.Description,
            Date = appointment.Date,
            PatientId = appointment.PatientId ?? 0,
            EmployeeId = appointment.EmployeeId ?? 0,
            PatientName = appointment.Patient?.FullName ?? "Unknown Patient",
            EmployeeName = appointment.Employee?.FullName ?? "Unknown Employee"
        };

        return Ok(appointmentDto);
    }
    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] AppointmentDto appointmentDto)
    {
        if (appointmentDto == null)
        {
            return BadRequest("Appointment data cannot be null");
        }
        // Find the appointment in the database
        var existingAppointment = await _appointmentRepository.GetAppointmentById(id);
        if (existingAppointment == null)
        {
            return NotFound("Appointment not found");
        }
        // Update the appointment properties
        existingAppointment.Subject = appointmentDto.Subject;
        existingAppointment.Description = appointmentDto.Description;
        existingAppointment.Date = appointmentDto.Date;
        existingAppointment.PatientId = appointmentDto.PatientId;
        existingAppointment.EmployeeId = appointmentDto.EmployeeId;
        // Save the changes
        bool updateSuccessful = await _appointmentRepository.Update(existingAppointment);
        if (updateSuccessful)
        {
            // Create notifications for appointment update
            await CreateAppointmentUpdateNotifications(existingAppointment);
            return Ok(existingAppointment); // Return the updated appointment
        }

        _logger.LogWarning("[AppointmentController] Appointment update failed {@appointment}", existingAppointment);
        return StatusCode(500, "Internal server error");
    }
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        // Get appointment details before deletion for notifications
        var appointmentToDelete = await _appointmentRepository.GetAppointmentById(id);
        if (appointmentToDelete == null)
        {
            return NotFound("Appointment not found");
        }

        bool returnOk = await _appointmentRepository.Delete(id);
        if (!returnOk)
        {
            _logger.LogError("[AppointmentController] Appointment deletion failed for the AppointmentId {AppointmentId:0000}", id);
            return BadRequest("Appointment deletion failed");
        }

        // Create notifications for appointment deletion
        await CreateAppointmentDeleteNotifications(appointmentToDelete);
        
        return NoContent();
    }

    private async Task CreateAppointmentNotifications(Appointment appointment)
    {
        try
        {
            // Get patient and employee information first
            var fullAppointment = await _appointmentRepository.GetAppointmentById(appointment.AppointmentId);
            if (fullAppointment?.Patient?.UserId == null || fullAppointment?.Employee?.UserId == null)
            {
                _logger.LogWarning("[AppointmentController] Missing user IDs for appointment {AppointmentId}", appointment.AppointmentId);
                return;
            }

            // Create notification for patient
            var patientNotification = new Notification
            {
                UserId = fullAppointment.Patient.UserId,
                Title = "Appointment Successfully Booked",
                Message = $"Your appointment '{fullAppointment.Subject}' has been successfully booked for {fullAppointment.Date:MMM dd, yyyy} with {fullAppointment.Employee?.FullName ?? "healthcare provider"}.",
                Type = "appointment",
                RelatedId = fullAppointment.AppointmentId,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            // Create notification for employee  
            var employeeNotification = new Notification
            {
                UserId = fullAppointment.Employee!.UserId,
                Title = "New Appointment Scheduled",
                Message = $"A new appointment '{fullAppointment.Subject}' has been booked by {fullAppointment.Patient?.FullName ?? "patient"} for {fullAppointment.Date:MMM dd, yyyy}.",
                Type = "appointment", 
                RelatedId = fullAppointment.AppointmentId,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            // Save both notifications
            await _notificationRepository.CreateAsync(patientNotification);
            await _notificationRepository.CreateAsync(employeeNotification);

            _logger.LogInformation("[AppointmentController] Created notifications for appointment {AppointmentId}", appointment.AppointmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AppointmentController] Failed to create notifications for appointment {AppointmentId}", appointment.AppointmentId);
        }
    }

    private async Task CreateAppointmentUpdateNotifications(Appointment appointment)
    {
        try
        {
            if (appointment?.Patient?.UserId == null || appointment?.Employee?.UserId == null)
            {
                _logger.LogWarning("[AppointmentController] Missing user IDs for appointment update {AppointmentId}", appointment?.AppointmentId ?? 0);
                return;
            }

            // Create notification for patient
            var patientNotification = new Notification
            {
                UserId = appointment.Patient.UserId,
                Title = "Appointment Updated",
                Message = $"Your appointment '{appointment.Subject}' has been updated. New date: {appointment.Date:MMM dd, yyyy} with {appointment.Employee?.FullName ?? "healthcare provider"}.",
                Type = "appointment",
                RelatedId = appointment.AppointmentId,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            // Create notification for employee  
            var employeeNotification = new Notification
            {
                UserId = appointment.Employee!.UserId,
                Title = "Appointment Updated",
                Message = $"Appointment '{appointment.Subject}' with {appointment.Patient?.FullName ?? "patient"} has been updated. New date: {appointment.Date:MMM dd, yyyy}.",
                Type = "appointment",
                RelatedId = appointment.AppointmentId,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            // Save both notifications
            await _notificationRepository.CreateAsync(patientNotification);
            await _notificationRepository.CreateAsync(employeeNotification);

            _logger.LogInformation("[AppointmentController] Created update notifications for appointment {AppointmentId}", appointment.AppointmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AppointmentController] Failed to create update notifications for appointment {AppointmentId}", appointment.AppointmentId);
        }
    }

    private async Task CreateAppointmentDeleteNotifications(Appointment appointment)
    {
        try
        {
            if (appointment?.Patient?.UserId == null || appointment?.Employee?.UserId == null)
            {
                _logger.LogWarning("[AppointmentController] Missing user IDs for appointment deletion {AppointmentId}", appointment?.AppointmentId ?? 0);
                return;
            }

            // Create notification for patient
            var patientNotification = new Notification
            {
                UserId = appointment.Patient.UserId,
                Title = "Appointment Cancelled",
                Message = $"Your appointment '{appointment.Subject}' scheduled for {appointment.Date:MMM dd, yyyy} has been cancelled.",
                Type = "appointment",
                RelatedId = appointment.AppointmentId,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            // Create notification for employee  
            var employeeNotification = new Notification
            {
                UserId = appointment.Employee!.UserId,
                Title = "Appointment Cancelled",
                Message = $"Appointment '{appointment.Subject}' with {appointment.Patient?.FullName ?? "patient"} scheduled for {appointment.Date:MMM dd, yyyy} has been cancelled.",
                Type = "appointment",
                RelatedId = appointment.AppointmentId,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            // Save both notifications
            await _notificationRepository.CreateAsync(patientNotification);
            await _notificationRepository.CreateAsync(employeeNotification);

            _logger.LogInformation("[AppointmentController] Created deletion notifications for appointment {AppointmentId}", appointment.AppointmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AppointmentController] Failed to create deletion notifications for appointment {AppointmentId}", appointment.AppointmentId);
        }
    }
}
   