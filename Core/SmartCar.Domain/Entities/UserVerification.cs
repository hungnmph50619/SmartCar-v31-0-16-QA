using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class UserVerification
    {
        public int UserVerificationID { get; set; }
        public int AppUserID { get; set; }
        public AppUser AppUser { get; set; } = null!;
        [MaxLength(30)] public string VerificationType { get; set; } = "Khách thuê";
        [MaxLength(30)] public string Status { get; set; } = "Chưa xác minh";
        [MaxLength(120)] public string? LegalFullName { get; set; }
        [MaxLength(20)] public string? Gender { get; set; }
        [MaxLength(50)] public string? CitizenIdMasked { get; set; }
        [MaxLength(64)] public string? CitizenIdFingerprint { get; set; }
        public DateTime? CitizenIdIssuedDate { get; set; }
        public DateTime? CitizenIdExpiryDate { get; set; }
        [MaxLength(500)] public string? CitizenIdAddress { get; set; }
        [MaxLength(2)] public string? PermanentProvinceCode { get; set; }
        [MaxLength(5)] public string? PermanentWardCode { get; set; }
        [MaxLength(100)] public string? PermanentProvince { get; set; }
        [MaxLength(150)] public string? PermanentWard { get; set; }
        [MaxLength(300)] public string? PermanentDetail { get; set; }
        [MaxLength(500)] public string? PermanentAddress { get; set; }
        public bool CurrentAddressSameAsPermanent { get; set; }
        [MaxLength(2)] public string? CurrentProvinceCode { get; set; }
        [MaxLength(5)] public string? CurrentWardCode { get; set; }
        [MaxLength(100)] public string? CurrentProvince { get; set; }
        [MaxLength(150)] public string? CurrentWard { get; set; }
        [MaxLength(300)] public string? CurrentDetail { get; set; }
        [MaxLength(500)] public string? CurrentAddress { get; set; }
        [MaxLength(50)] public string? DriverLicenseNumber { get; set; }
        [MaxLength(20)] public string? DriverLicenseClass { get; set; }
        public Guid? CitizenIdFrontFileID { get; set; }
        public Guid? CitizenIdBackFileID { get; set; }
        public Guid? DriverLicenseFileID { get; set; }
        public Guid? PortraitFileID { get; set; }
        // Cột URL cũ được giữ tạm để đọc dữ liệu của các bản trước; hồ sơ mới chỉ dùng FileID.
        [MaxLength(500)] public string? CitizenIdFrontUrl { get; set; }
        [MaxLength(500)] public string? CitizenIdBackUrl { get; set; }
        [MaxLength(500)] public string? DriverLicenseUrl { get; set; }
        [MaxLength(500)] public string? PortraitUrl { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? DriverLicenseIssuedDate { get; set; }
        public DateTime? DriverLicenseExpiry { get; set; }
        public int? ReviewedByAppUserID { get; set; }
        [MaxLength(1000)] public string? RejectionReason { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedDate { get; set; }
    }
}
