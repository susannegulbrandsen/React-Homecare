using HomeCareApp.Data;
using HomeCareApp.DTOs;
using HomeCareApp.Models;
using HomeCareApp.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HomeCareApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MedicationController : ControllerBase
    {
        private readonly IMedicationRepository _repo;
        private readonly AppDbContext _db;
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<MedicationController> _logger;

        public MedicationController(IMedicationRepository repo, AppDbContext db, INotificationRepository notificationRepository, ILogger<MedicationController> logger)
        {
            _repo = repo;
            _db = db;
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        // Get all medications (Public access like appointments)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MedicationDto>>> GetAll()
        {
            var items = await _repo.GetAllAsync();
            return Ok(items.Select(MedicationDto.FromEntity));
        }

        // Get medications for a specific patient
        [HttpGet("patient/{patientId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<MedicationDto>>> GetByPatientId(int patientId)
        {
            var items = await _repo.GetByPatientAsync(patientId);
            return Ok(items.Select(MedicationDto.FromEntity));
        }

        // Get a medication by name
        [HttpGet("{medicationName}")]
        public async Task<ActionResult<MedicationDto>> GetByName(string medicationName)
        {
            var med = await _db.Medications.FirstOrDefaultAsync(m => m.medicineName == medicationName);
            if (med == null) return NotFound();
            return Ok(MedicationDto.FromEntity(med));
        }

        // Create a new medication
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<MedicationDto>> Create(MedicationDto dto)
        {
            var entity = dto.ToEntity();
            _db.Medications.Add(entity);
            await _db.SaveChangesAsync();
            
            // Create notification for medication creation
            await CreateMedicationNotification(entity, "created");
            
            return CreatedAtAction(nameof(GetByName), new { medicationName = entity.medicineName }, MedicationDto.FromEntity(entity));
        }

        // Update medication
        [HttpPut("{medicationName}")]
        [Authorize]
        public async Task<IActionResult> Update(string medicationName, MedicationDto dto)
        {
            var med = await _db.Medications.FirstOrDefaultAsync(m => m.medicineName == medicationName);
            if (med == null) return NotFound();

            med.Dosage = dto.Dosage;
            med.StartDate = dto.StartDate;
            med.EndDate = dto.EndDate;
            med.PatientId = dto.PatientId;
            med.Indication = dto.Indication;

            await _db.SaveChangesAsync();
            
            // Create notification for medication update
            await CreateMedicationNotification(med, "updated");
            
            return NoContent();
        }

        // Delete medication
        [HttpDelete("{medicationName}")]
        [Authorize]
        public async Task<IActionResult> Delete(string medicationName)
        {
            var med = await _db.Medications.FirstOrDefaultAsync(m => m.medicineName == medicationName);
            if (med == null) return NotFound();

            // Create notification before deletion (need medication data)
            await CreateMedicationNotification(med, "deleted");

            _db.Medications.Remove(med);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private async Task CreateMedicationNotification(Medication medication, string action)
        {
            try
            {
                // Get patient information including UserId
                var patient = await _db.Patients.FirstOrDefaultAsync(p => p.PatientId == medication.PatientId);
                if (patient?.UserId == null)
                {
                    _logger.LogWarning("[MedicationController] Missing patient UserId for medication {MedicineName}", medication.medicineName);
                    return;
                }

                string title = action switch
                {
                    "created" => "New Medication Added",
                    "updated" => "Medication Updated", 
                    "deleted" => "Medication Removed",
                    _ => "Medication Changed"
                };

                string message = action switch
                {
                    "created" => $"A new medication '{medication.medicineName}' has been added to your treatment plan. Dosage: {medication.Dosage}.",
                    "updated" => $"Your medication '{medication.medicineName}' has been updated. New dosage: {medication.Dosage}.",
                    "deleted" => $"The medication '{medication.medicineName}' has been removed from your treatment plan.",
                    _ => $"Your medication '{medication.medicineName}' has been changed."
                };

                var notification = new Notification
                {
                    UserId = patient.UserId,
                    Title = title,
                    Message = message,
                    Type = "medication",
                    RelatedId = null, // Medications use string key, not int
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                await _notificationRepository.CreateAsync(notification);
                _logger.LogInformation("[MedicationController] Created {Action} notification for medication {MedicineName}", action, medication.medicineName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MedicationController] Failed to create {Action} notification for medication {MedicineName}", action, medication.medicineName);
            }
        }
    }
}


