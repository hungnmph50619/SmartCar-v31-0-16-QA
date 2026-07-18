using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class EmailVerificationOtp
    {
        public int EmailVerificationOtpID { get; set; }
        public int AppUserID { get; set; }
        public AppUser AppUser { get; set; } = null!;

        [MaxLength(128)]
        public string OtpHash { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Purpose { get; set; } = "Register";

        [MaxLength(256)]
        public string? TargetEmail { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresDate { get; set; }
        public DateTime? UsedDate { get; set; }
        public DateTime? LastSentAt { get; set; }
        public int FailedAttempts { get; set; }
        public DateTime? LockedUntil { get; set; }
    }
}
