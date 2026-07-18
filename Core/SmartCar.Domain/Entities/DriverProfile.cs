using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class DriverProfile
    {
        public int DriverProfileID { get; set; }
        public int PartnerAppUserID { get; set; }
        public AppUser PartnerAppUser { get; set; } = null!;
        [MaxLength(150)] public string FullName { get; set; } = string.Empty;
        [MaxLength(20)] public string Phone { get; set; } = string.Empty;
        [MaxLength(20)] public string CitizenIdentityNumber { get; set; } = string.Empty;
        [MaxLength(64)] public string? CitizenIdFingerprint { get; set; }
        [MaxLength(50)] public string DriverLicenseNumber { get; set; } = string.Empty;
        [MaxLength(20)] public string DriverLicenseClass { get; set; } = string.Empty;
        public DateTime DriverLicenseIssuedDate { get; set; }
        public DateTime DriverLicenseExpiryDate { get; set; }
        [MaxLength(50)] public string RelationshipType { get; set; } = "Nhân viên";
        public Guid? CitizenIdFrontFileID { get; set; }
        public Guid? CitizenIdBackFileID { get; set; }
        public Guid? PortraitFileID { get; set; }
        public Guid? DriverLicenseFileID { get; set; }
        [MaxLength(40)] public string Status { get; set; } = "Bản nháp";
        [MaxLength(1000)] public string? ReviewReason { get; set; }
        public bool CanResubmit { get; set; } = true;
        public int? ReviewedByAppUserID { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        [Timestamp] public byte[]? RowVersion { get; set; }
    }
}
