using System.ComponentModel.DataAnnotations;

namespace HomeCareApp.DTOs
{
    public class NotificationDto
    {
        public int? NotificationId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        public string? Type { get; set; }

        public int? RelatedId { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; }
    }
}