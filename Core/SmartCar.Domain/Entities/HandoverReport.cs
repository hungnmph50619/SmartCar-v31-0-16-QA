using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class HandoverReport
    {
        public int HandoverReportID { get; set; }
        public int ReservationID { get; set; }
        public Reservation Reservation { get; set; } = null!;
        [MaxLength(20)] public string ReportType { get; set; } = "Giao xe";
        public int OdometerKm { get; set; }
        public int FuelPercent { get; set; }
        [MaxLength(2000)] public string? ExistingDamage { get; set; }
        [MaxLength(1000)] public string? Accessories { get; set; }
        [MaxLength(500)] public string? LocationText { get; set; }
        [MaxLength(4000)] public string? PhotoUrls { get; set; }
        [MaxLength(100)] public string? OtpHash { get; set; }
        [MaxLength(128)] public string? CustomerOtpHash { get; set; }
        [MaxLength(128)] public string? PartnerOtpHash { get; set; }
        public DateTime? OtpExpiresAt { get; set; }
        public DateTime? CustomerConfirmedDate { get; set; }
        public DateTime? PartnerConfirmedDate { get; set; }
        public DateTime? CustomerOtpLastSentAt { get; set; }
        public DateTime? PartnerOtpLastSentAt { get; set; }
        public int CustomerOtpFailedAttempts { get; set; }
        public int PartnerOtpFailedAttempts { get; set; }
        public DateTime? CustomerOtpLockedUntil { get; set; }
        public DateTime? PartnerOtpLockedUntil { get; set; }
        public DateTime? ConfirmedDate { get; set; }
        public int OtpFailedAttempts { get; set; }
        public DateTime? OtpLastSentAt { get; set; }
        public DateTime? OtpLockedUntil { get; set; }
        public int CreatedByAppUserID { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public bool IsLocked { get; set; }
        public bool IsSuperseded { get; set; }
        public int? ReplacedByReportId { get; set; }
        [MaxLength(1000)] public string? CorrectionReason { get; set; }
    }
}
