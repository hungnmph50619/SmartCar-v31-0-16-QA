using System.ComponentModel.DataAnnotations;
using SmartCar.Dto.ReservationDtos;

namespace SmartCar.Dto.MarketplaceDtos
{
    public class PlatformFeeSettingDto
    {
        public int PlatformFeeSettingID { get; set; }
        public decimal VehiclePartnerCommissionPercent { get; set; }
        public string? Note { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    public class UpdatePlatformFeeSettingDto
    {
        [Range(0, 100, ErrorMessage = "Chiết khấu xe đối tác phải từ 0 đến 100%.")]
        public decimal VehiclePartnerCommissionPercent { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }
    }

    public class CreateVehiclePartnerApplicationDto
    {
        [Required, MaxLength(120)] public string OwnerFullName { get; set; } = string.Empty;
        [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = string.Empty;
        [Required, MaxLength(20)] public string Phone { get; set; } = string.Empty;
        [Required, MaxLength(300)] public string Address { get; set; } = string.Empty;
        [Required, MaxLength(20)] public string CitizenIdentityNumber { get; set; } = string.Empty;
        [Required, MaxLength(120)] public string BankName { get; set; } = string.Empty;
        [Required, MaxLength(50)] public string BankAccountNumber { get; set; } = string.Empty;
        [Required, MaxLength(120)] public string BankAccountHolder { get; set; } = string.Empty;
        [Required, MaxLength(100)] public string BrandName { get; set; } = string.Empty;
        [Required, MaxLength(100)] public string Model { get; set; } = string.Empty;
        [MaxLength(100)] public string? VehicleVersion { get; set; }
        [Required, MaxLength(50)] public string ChassisNumber { get; set; } = string.Empty;
        [Required, MaxLength(50)] public string EngineNumber { get; set; } = string.Empty;
        [Range(1990, 2100)] public int ManufactureYear { get; set; }
        [Required, MaxLength(20)] public string LicensePlate { get; set; } = string.Empty;
        [Required, MaxLength(50)] public string Color { get; set; } = string.Empty;
        [Required, MaxLength(50)] public string Transmission { get; set; } = string.Empty;
        [Required, MaxLength(50)] public string Fuel { get; set; } = string.Empty;
        [Range(2, 50)] public byte Seat { get; set; }
        [Range(0, 2000000)] public int Km { get; set; }
        [Range(1, int.MaxValue)] public int LocationID { get; set; }
        [Range(100000, 100000000)] public decimal ProposedDailyPrice { get; set; }
        [Range(0, 1000000000)] public decimal ProposedDepositAmount { get; set; }
        [Required, MaxLength(30)] public string RentalMode { get; set; } = "Tự lái";
        [MaxLength(120)] public string? DriverFullName { get; set; }
        [MaxLength(20)] public string? DriverPhone { get; set; }
        [MaxLength(20)] public string? DriverCitizenIdentityNumber { get; set; }
        [MaxLength(50)] public string? DriverLicenseNumber { get; set; }
        [MaxLength(20)] public string? DriverLicenseClass { get; set; }
        public DateTime? DriverLicenseExpiryDate { get; set; }
        public Guid? DriverLicenseFileId { get; set; }
        [Required, MaxLength(40)] public string DeliveryMethod { get; set; } = "Nhận tại điểm giao xe";
        [MaxLength(300)] public string? DeliveryAddress { get; set; }
        [Range(0, 5000)] public int KmLimitPerDay { get; set; } = 300;
        [Range(0, 10000000)] public decimal ExtraKmFee { get; set; }
        [Range(0, 100000000)] public decimal DeliveryFee { get; set; }
        [MaxLength(1500)] public string? Amenities { get; set; }
        [MaxLength(1500)] public string? Accessories { get; set; }
        [MaxLength(1500)] public string? RentalConditions { get; set; }
        [MaxLength(1500)] public string? CancellationPolicy { get; set; }
        [Required, MaxLength(500)] public string VehicleImageUrl { get; set; } = string.Empty;
        [Required, MaxLength(500)] public string FrontImageUrl { get; set; } = string.Empty;
        [Required, MaxLength(500)] public string RearImageUrl { get; set; } = string.Empty;
        [Required, MaxLength(500)] public string LeftImageUrl { get; set; } = string.Empty;
        [Required, MaxLength(500)] public string RightImageUrl { get; set; } = string.Empty;
        [Required, MaxLength(500)] public string InteriorImageUrl { get; set; } = string.Empty;
        [Required, MaxLength(500)] public string DashboardImageUrl { get; set; } = string.Empty;
        public Guid RegistrationFileId { get; set; }
        public Guid InspectionFileId { get; set; }
        public Guid InsuranceFileId { get; set; }
    }

    public class ReviewVehiclePartnerApplicationDto
    {
        [Required] public string Status { get; set; } = string.Empty;
        [MaxLength(1000)] public string? AdminNote { get; set; }
        [Range(100000, 100000000)] public decimal? ApprovedDailyPrice { get; set; }
        [Range(0, 1000000000)] public decimal? ApprovedDepositAmount { get; set; }
        [Range(0, 100)] public decimal? CommissionRateOverride { get; set; }
    }

    public class ResultVehiclePartnerApplicationDto
    {
        public int VehiclePartnerApplicationID { get; set; }
        public int AppUserID { get; set; }
        public string OwnerFullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string CitizenIdentityNumber { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string BankAccountNumber { get; set; } = string.Empty;
        public string BankAccountHolder { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string? VehicleVersion { get; set; }
        public string ChassisNumber { get; set; } = string.Empty;
        public string EngineNumber { get; set; } = string.Empty;
        public int ManufactureYear { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Transmission { get; set; } = string.Empty;
        public string Fuel { get; set; } = string.Empty;
        public byte Seat { get; set; }
        public int Km { get; set; }
        public int LocationID { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public decimal ProposedDailyPrice { get; set; }
        public decimal ProposedDepositAmount { get; set; }
        public string RentalMode { get; set; } = string.Empty;
        public string? DriverFullName { get; set; }
        public string? DriverPhone { get; set; }
        public string? DriverCitizenIdentityNumber { get; set; }
        public string? DriverLicenseNumber { get; set; }
        public string? DriverLicenseClass { get; set; }
        public DateTime? DriverLicenseExpiryDate { get; set; }
        public string? DriverLicenseImageUrl { get; set; }
        public string DeliveryMethod { get; set; } = string.Empty;
        public string? DeliveryAddress { get; set; }
        public int KmLimitPerDay { get; set; }
        public decimal ExtraKmFee { get; set; }
        public decimal DeliveryFee { get; set; }
        public string? Amenities { get; set; }
        public string? Accessories { get; set; }
        public string? RentalConditions { get; set; }
        public string? CancellationPolicy { get; set; }
        public string VehicleImageUrl { get; set; } = string.Empty;
        public string FrontImageUrl { get; set; } = string.Empty;
        public string RearImageUrl { get; set; } = string.Empty;
        public string LeftImageUrl { get; set; } = string.Empty;
        public string RightImageUrl { get; set; } = string.Empty;
        public string InteriorImageUrl { get; set; } = string.Empty;
        public string DashboardImageUrl { get; set; } = string.Empty;
        public string RegistrationImageUrl { get; set; } = string.Empty;
        public string InspectionImageUrl { get; set; } = string.Empty;
        public string InsuranceImageUrl { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? AdminNote { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ReviewedDate { get; set; }
        public int? ApprovedCarID { get; set; }
    }

    public class ResultPartnerVehicleDto
    {
        public int PartnerVehicleID { get; set; }
        public int CarID { get; set; }
        public string CarName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public string CoverImageUrl { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public decimal DailyPrice { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal AppliedCommissionRate { get; set; }
        public bool IsActive { get; set; }
        public string OperationalStatus { get; set; } = "Đang hoạt động";
        public string? WarningText { get; set; }
        public DateTime? NearestDocumentExpiry { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
        public DateTime ListedDate { get; set; }
        public int CompletedReservations { get; set; }
        public decimal GrossRevenue { get; set; }
        public decimal PlatformCommission { get; set; }
        public decimal OwnerNetRevenue { get; set; }
    }

    public class PartnerVehicleDashboardDto
    {
        public decimal GlobalCommissionRate { get; set; }
        public VehiclePartnerProfileDto? PartnerProfile { get; set; }
        public decimal TotalGrossRevenue { get; set; }
        public decimal TotalPlatformCommission { get; set; }
        public decimal TotalOwnerNetRevenue { get; set; }
        public decimal PendingPayout { get; set; }
        public int PendingOwnerConfirmations { get; set; }
        public int ActiveRentals { get; set; }
        public int UpcomingDeliveries { get; set; }
        public int UpcomingReturns { get; set; }
        public int ExpiringDocuments { get; set; }
        public int DueMaintenanceVehicles { get; set; }
        public int OpenIncidents { get; set; }
        public int OpenDisputes { get; set; }
        public List<ResultVehiclePartnerApplicationDto> Applications { get; set; } = new();
        public List<ResultPartnerVehicleDto> Vehicles { get; set; } = new();
        public List<ResultReservationDto> Reservations { get; set; } = new();
        public List<ResultCommissionTransactionDto> Transactions { get; set; } = new();
    }

    public class ResultCommissionTransactionDto
    {
        public int CommissionTransactionID { get; set; }
        public int ReservationID { get; set; }
        public int SettlementID { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public decimal GrossAmount { get; set; }
        public decimal CommissionRate { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal PartnerNetAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public string? BankReference { get; set; }
        public string? Note { get; set; }
    }

    public class UpdateCommissionTransactionStatusDto
    {
        [Required] public string Status { get; set; } = string.Empty;
        [MaxLength(100)] public string? BankReference { get; set; }
        [MaxLength(1000)] public string? Note { get; set; }
    }

    public class UpdatePartnerVehicleAvailabilityDto
    {
        public bool IsActive { get; set; }
        [MaxLength(500)] public string? Reason { get; set; }
    }

    public class OwnerReservationDecisionDto
    {
        [Required] public string Decision { get; set; } = string.Empty;
        [MaxLength(1000)] public string? Note { get; set; }
    }
}

namespace SmartCar.Dto.MarketplaceDtos
{
    public class PartnerVehicleOperationsDto
    {
        public ResultPartnerVehicleDto Vehicle { get; set; } = new();
        public string RentalMode { get; set; } = string.Empty;
        public string DeliveryMethod { get; set; } = string.Empty;
        public int KmLimitPerDay { get; set; }
        public decimal ExtraKmFee { get; set; }
        public string? PauseReason { get; set; }
        public int UpcomingReservationCount { get; set; }
        public int ActiveReservationCount { get; set; }
        public List<PartnerVehicleCalendarItemDto> Calendar { get; set; } = new();
        public List<PartnerVehicleDocumentDto> Documents { get; set; } = new();
        public List<PartnerVehicleMaintenanceDto> MaintenanceRecords { get; set; } = new();
    }

    public class PartnerVehicleCalendarItemDto
    {
        public int ReservationID { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal OwnerReceivableAmount { get; set; }
    }

    public class PartnerVehicleDocumentDto
    {
        public int VehicleDocumentID { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public DateTime? IssuedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }
    }

    public class PartnerVehicleMaintenanceDto
    {
        public int MaintenanceRecordID { get; set; }
        public DateTime MaintenanceDate { get; set; }
        public int OdometerKm { get; set; }
        public int? NextMaintenanceKm { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
        public string WorkPerformed { get; set; } = string.Empty;
        public string? Garage { get; set; }
        public decimal Cost { get; set; }
        public bool HasUnresolvedSafetyIssue { get; set; }
        public string? SafetyIssueNote { get; set; }
    }
}
