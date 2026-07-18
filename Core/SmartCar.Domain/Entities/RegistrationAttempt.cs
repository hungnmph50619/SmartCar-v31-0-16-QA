using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class RegistrationAttempt
    {
        public Guid RegistrationAttemptID { get; set; } = Guid.NewGuid();
        [MaxLength(30)] public string Username { get; set; } = string.Empty;
        [MaxLength(500)] public string PasswordHash { get; set; } = string.Empty;
        [MaxLength(100)] public string Name { get; set; } = string.Empty;
        [MaxLength(100)] public string Surname { get; set; } = string.Empty;
        [MaxLength(256)] public string Email { get; set; } = string.Empty;
        [MaxLength(20)] public string Phone { get; set; } = string.Empty;
        [MaxLength(20)] public string AccountType { get; set; } = "Customer";
        [MaxLength(50)] public string? PartnerType { get; set; }
        [MaxLength(50)] public string TermsVersion { get; set; } = "Terms-v1.0";
        [MaxLength(50)] public string PrivacyVersion { get; set; } = "Privacy-v1.0";
        public DateTime TermsAcceptedAt { get; set; } = DateTime.UtcNow;
        public DateTime PrivacyAcceptedAt { get; set; } = DateTime.UtcNow;
        [MaxLength(20)] public string Status { get; set; } = "Pending";
        [MaxLength(128)] public string OtpHash { get; set; } = string.Empty;
        public int FailedAttempts { get; set; }
        public int SendCountHour { get; set; }
        public DateTime HourWindowStartedAt { get; set; } = DateTime.UtcNow;
        public int SendCountDay { get; set; }
        public DateTime DayWindowStartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastSentAt { get; set; }
        public DateTime OtpExpiresAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? VerifiedAt { get; set; }
        public int? CreatedAppUserID { get; set; }
        [Timestamp] public byte[]? RowVersion { get; set; }
    }
}
