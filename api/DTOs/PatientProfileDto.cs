namespace HomeCareApp.DTOs
{
    public class PatientProfileDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string HealthRelatedInfo { get; set; } = string.Empty;
    }
}