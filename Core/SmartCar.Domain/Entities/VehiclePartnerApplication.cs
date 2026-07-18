using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCar.Domain.Entities
{
    public class VehiclePartnerApplication
    {
        public int VehiclePartnerApplicationID { get; set; }
        public int AppUserID { get; set; }
        public AppUser AppUser { get; set; } = null!;

        [MaxLength(120)] public string OwnerFullName { get; set; } = string.Empty;
        [MaxLength(150)] public string Email { get; set; } = string.Empty;
        [MaxLength(20)] public string Phone { get; set; } = string.Empty;
        [MaxLength(300)] public string Address { get; set; } = string.Empty;
        [MaxLength(20)] public string CitizenIdentityNumber { get; set; } = string.Empty;

        [MaxLength(120)] public string BankName { get; set; } = string.Empty;
        [MaxLength(50)] public string BankAccountNumber { get; set; } = string.Empty;
        [MaxLength(120)] public string BankAccountHolder { get; set; } = string.Empty;

        [MaxLength(100)] public string BrandName { get; set; } = string.Empty;
        [MaxLength(100)] public string Model { get; set; } = string.Empty;
        [MaxLength(100)] public string? VehicleVersion { get; set; }
        [MaxLength(50)] public string ChassisNumber { get; set; } = string.Empty;
        [MaxLength(50)] public string EngineNumber { get; set; } = string.Empty;
        public int ManufactureYear { get; set; }
        [MaxLength(20)] public string LicensePlate { get; set; } = string.Empty;
        [MaxLength(50)] public string Color { get; set; } = string.Empty;
        [MaxLength(50)] public string Transmission { get; set; } = string.Empty;
        [MaxLength(50)] public string Fuel { get; set; } = string.Empty;
        public byte Seat { get; set; }
        public int Km { get; set; }

        public int LocationID { get; set; }
        public Location Location { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ProposedDailyPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ProposedDepositAmount { get; set; }

        [MaxLength(30)] public string RentalMode { get; set; } = "Tự lái";

        // Hồ sơ tài xế chỉ bắt buộc khi xe cung cấp dịch vụ có tài xế.
        [MaxLength(120)] public string? DriverFullName { get; set; }
        [MaxLength(20)] public string? DriverPhone { get; set; }
        [MaxLength(20)] public string? DriverCitizenIdentityNumber { get; set; }
        [MaxLength(50)] public string? DriverLicenseNumber { get; set; }
        [MaxLength(20)] public string? DriverLicenseClass { get; set; }
        public DateTime? DriverLicenseExpiryDate { get; set; }
        [MaxLength(500)] public string? DriverLicenseImageUrl { get; set; }

        [MaxLength(40)] public string DeliveryMethod { get; set; } = "Nhận tại điểm giao xe";
        [MaxLength(300)] public string? DeliveryAddress { get; set; }
        public int KmLimitPerDay { get; set; } = 300;
        [Column(TypeName = "decimal(18,2)")] public decimal ExtraKmFee { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal DeliveryFee { get; set; }
        [MaxLength(1500)] public string? Amenities { get; set; }
        [MaxLength(1500)] public string? Accessories { get; set; }
        [MaxLength(1500)] public string? RentalConditions { get; set; }
        [MaxLength(1500)] public string? CancellationPolicy { get; set; }

        [MaxLength(500)] public string VehicleImageUrl { get; set; } = string.Empty;
        [MaxLength(500)] public string FrontImageUrl { get; set; } = string.Empty;
        [MaxLength(500)] public string RearImageUrl { get; set; } = string.Empty;
        [MaxLength(500)] public string LeftImageUrl { get; set; } = string.Empty;
        [MaxLength(500)] public string RightImageUrl { get; set; } = string.Empty;
        [MaxLength(500)] public string InteriorImageUrl { get; set; } = string.Empty;
        [MaxLength(500)] public string DashboardImageUrl { get; set; } = string.Empty;
        [MaxLength(500)] public string RegistrationImageUrl { get; set; } = string.Empty;
        [MaxLength(500)] public string InspectionImageUrl { get; set; } = string.Empty;
        [MaxLength(500)] public string InsuranceImageUrl { get; set; } = string.Empty;

        [MaxLength(40)] public string Status { get; set; } = "Chờ duyệt";
        [MaxLength(1000)] public string? AdminNote { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedDate { get; set; }
        public int? ApprovedCarID { get; set; }
    }
}
