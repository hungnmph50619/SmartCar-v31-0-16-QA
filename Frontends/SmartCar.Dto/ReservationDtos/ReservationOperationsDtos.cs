using System.ComponentModel.DataAnnotations;

namespace SmartCar.Dto.ReservationDtos
{
    public class ReservationDetailDto
    {
        public ResultReservationDto Reservation { get; set; } = new();
        public ReservationPriceBreakdownDto Price { get; set; } = new();
        public string VerificationStatus { get; set; } = "Chưa xác minh";
        public DateTime? DriverLicenseExpiry { get; set; }
        public DateTime? HoldExpiresAt { get; set; }
        public bool CanCancel { get; set; }
        public bool CanSubmitDepositProof { get; set; }
        public bool CanSubmitRentalPaymentProof { get; set; }
        public decimal RentalPaymentDue { get; set; }
        public bool CanSubmitSecurityDepositProof { get; set; }
        public decimal SecurityDepositDue { get; set; }
        public bool CanCreateDeliveryReport { get; set; }
        public bool CanCreateReturnReport { get; set; }
        public bool CanReportIncident { get; set; }
        public bool CanOpenDispute { get; set; }
        public bool CanOwnerDecide { get; set; }
        public List<ReservationTimelineItemDto> Timeline { get; set; } = new();
        public List<ReservationPaymentDto> Payments { get; set; } = new();
        public List<ReservationHandoverDto> Handovers { get; set; } = new();
        public List<ReservationAdditionalChargeDto> AdditionalCharges { get; set; } = new();
        public List<ReservationIncidentDto> Incidents { get; set; } = new();
        public List<ReservationDisputeDto> Disputes { get; set; } = new();
        public List<ReservationTrafficFineDto> TrafficFines { get; set; } = new();
        public ReservationSettlementDto? Settlement { get; set; }
    }

    public class CancellationPreviewDto
    {
        public bool CanCancel { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal PaidAmount { get; set; }
        public decimal FeeRate { get; set; }
        public decimal CancellationFee { get; set; }
        public decimal RefundAmount { get; set; }
        public string PolicyVersion { get; set; } = string.Empty;
    }

    public class ReservationPriceBreakdownDto
    {
        public decimal DailyPrice { get; set; }
        public int RentalDays { get; set; }
        public decimal RentalAmount { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal InsuranceFee { get; set; }
        public decimal CustomerPlatformFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalRental { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal ReservationDepositAmount { get; set; }
        public decimal SecurityDepositAmount { get; set; }
        public decimal TotalDue { get; set; }
        public string RefundableExplanation { get; set; } = "Cọc giữ chỗ được cấn trừ vào tiền thuê; cọc bảo đảm được hoàn sau khi trả xe và hoàn tất nghĩa vụ, trừ các khoản phát sinh hợp lệ.";
    }

    public class ReservationTimelineItemDto
    {
        public string Status { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime? ChangedDate { get; set; }
        public string State { get; set; } = "upcoming";
    }

    public class ReservationPaymentDto
    {
        public int PaymentID { get; set; }
        public string PaymentType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal ProviderFeeAmount { get; set; }
        public bool ProviderFeeVerified { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? TransactionCode { get; set; }
        public string? Provider { get; set; }
        public string? TransferContent { get; set; }
        public DateTime? CustomerReportedDate { get; set; }
        public bool IsSimulated { get; set; }
        public string? VerificationNote { get; set; }
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityID { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ConfirmedDate { get; set; }
    }

    public class ReservationHandoverDto
    {
        public int HandoverReportID { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public int OdometerKm { get; set; }
        public int FuelPercent { get; set; }
        public string? ExistingDamage { get; set; }
        public string? Accessories { get; set; }
        public string? LocationText { get; set; }
        public string? PhotoUrls { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ConfirmedDate { get; set; }
        public bool IsLocked { get; set; }
        public bool IsSuperseded { get; set; }
        public DateTime? OtpExpiresAt { get; set; }
    }

    public class ReservationAdditionalChargeDto
    {
        public int AdditionalChargeID { get; set; }
        public string ChargeType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? EvidenceUrls { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? PaymentID { get; set; }
        public string? PaymentStatus { get; set; }
        public bool CanSubmitPaymentProof { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ReservationIncidentDto
    {
        public int IncidentID { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? LocationText { get; set; }
        public string? EvidenceUrls { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool VehicleImmobilized { get; set; }
        public DateTime OccurredAt { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ReservationDisputeDto
    {
        public int DisputeID { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? EvidenceUrls { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Resolution { get; set; }
        public decimal CompensationAmount { get; set; }
        public int? AssignedStaffAppUserID { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<ReservationDisputeMessageDto> Messages { get; set; } = new();
    }

    public class ReservationDisputeMessageDto
    {
        public int DisputeMessageID { get; set; }
        public int SenderAppUserID { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? EvidenceUrls { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ReservationTrafficFineDto
    {
        public int TrafficFineID { get; set; }
        public DateTime ViolationAt { get; set; }
        public string Violation { get; set; } = string.Empty;
        public string? LocationText { get; set; }
        public decimal Amount { get; set; }
        public string? NoticeNumber { get; set; }
        public string? EvidenceUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ReservationSettlementDto
    {
        public int SettlementID { get; set; }
        public decimal GrossRental { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal PaymentGatewayFee { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal CompensationAmount { get; set; }
        public decimal OwnerPayout { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? PayoutTransactionCode { get; set; }
        public int? CreatedByAppUserID { get; set; }
        public int? ApprovedByAppUserID { get; set; }
        public bool CanApprovePayment { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? PaidDate { get; set; }
    }

    public class ReservationQuoteDto
    {
        public int CarID { get; set; }
        public string CarName { get; set; } = string.Empty;
        public DateTime PickUpDateTime { get; set; }
        public DateTime DropOffDateTime { get; set; }
        public bool IsAvailable { get; set; }
        public string AvailabilityMessage { get; set; } = string.Empty;
        public string RentalMode { get; set; } = string.Empty;
        public string DeliveryMethod { get; set; } = string.Empty;
        public ReservationPriceBreakdownDto Price { get; set; } = new();
    }

    public class CustomerReadinessDto
    {
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? LegalFullName { get; set; }
        public string? Gender { get; set; }
        public string? CitizenIdAddress { get; set; }
        public string? PermanentProvinceCode { get; set; }
        public string? PermanentWardCode { get; set; }
        public string? PermanentProvince { get; set; }
        public string? PermanentWard { get; set; }
        public string? PermanentDetail { get; set; }
        public string? PermanentAddress { get; set; }
        public bool CurrentAddressSameAsPermanent { get; set; }
        public string? CurrentProvinceCode { get; set; }
        public string? CurrentWardCode { get; set; }
        public string? CurrentProvince { get; set; }
        public string? CurrentWard { get; set; }
        public string? CurrentDetail { get; set; }
        public string? CurrentAddress { get; set; }
        public string? DriverLicenseNumber { get; set; }
        public string? DriverLicenseClass { get; set; }
        public DateTime? CitizenIdIssuedDate { get; set; }
        public DateTime? CitizenIdExpiryDate { get; set; }
        public string VerificationStatus { get; set; } = "Chưa xác minh";
        public string? VerificationNote { get; set; }
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityID { get; set; }
        public string CitizenIdMasked { get; set; } = string.Empty;
        public bool HasCitizenIdFront { get; set; }
        public bool HasCitizenIdBack { get; set; }
        public bool HasDriverLicense { get; set; }
        public bool HasPortrait { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? DriverLicenseIssuedDate { get; set; }
        public DateTime? DriverLicenseExpiry { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool ContactReady { get; set; }
        public bool CanBook { get; set; }
        public bool CanBookWithDriver { get; set; }
        public DateTime? VerificationSubmittedDate { get; set; }
        public DateTime? VerificationReviewedDate { get; set; }
        public List<string> MissingItems { get; set; } = new();
    }

    public class SubmitPaymentProofDto
    {
        [MaxLength(50)] public string? Provider { get; set; }
        [MaxLength(30)] public string? PaymentType { get; set; }
        public int? AdditionalChargeID { get; set; }
    }

    public class SubmitVerificationDto
    {
        [Required, RegularExpression(@"^(0|\+84)[0-9]{9,10}$")] public string Phone { get; set; } = string.Empty;
        [Required, MaxLength(120)] public string LegalFullName { get; set; } = string.Empty;
        [Required, MaxLength(20)] public string Gender { get; set; } = string.Empty;
        [Required, MaxLength(500)] public string CitizenIdAddress { get; set; } = string.Empty;
        [Required, StringLength(2, MinimumLength = 2)] public string PermanentProvinceCode { get; set; } = string.Empty;
        [Required, StringLength(5, MinimumLength = 5)] public string PermanentWardCode { get; set; } = string.Empty;
        [MaxLength(100)] public string? PermanentProvince { get; set; }
        [MaxLength(150)] public string? PermanentWard { get; set; }
        [Required, MaxLength(300)] public string PermanentDetail { get; set; } = string.Empty;
        [MaxLength(500)] public string? PermanentAddress { get; set; }
        public bool CurrentAddressSameAsPermanent { get; set; }
        [MaxLength(2)] public string? CurrentProvinceCode { get; set; }
        [MaxLength(5)] public string? CurrentWardCode { get; set; }
        [MaxLength(100)] public string? CurrentProvince { get; set; }
        [MaxLength(150)] public string? CurrentWard { get; set; }
        [MaxLength(300)] public string? CurrentDetail { get; set; }
        [MaxLength(500)] public string? CurrentAddress { get; set; }
        [Required, MaxLength(50)] public string DriverLicenseNumber { get; set; } = string.Empty;
        [Required, MaxLength(20)] public string DriverLicenseClass { get; set; } = string.Empty;
        [Required] public DateTime CitizenIdIssuedDate { get; set; }
        [Required] public DateTime CitizenIdExpiryDate { get; set; }
        public Guid? CitizenIdFrontFileId { get; set; }
        public Guid? CitizenIdBackFileId { get; set; }
        public Guid? DriverLicenseFileId { get; set; }
        public Guid? PortraitFileId { get; set; }
        [Required] public DateTime DateOfBirth { get; set; }
        [Required] public DateTime DriverLicenseIssuedDate { get; set; }
        [Required] public DateTime DriverLicenseExpiry { get; set; }
        [Required, RegularExpression(@"^\d{12}$")] public string CitizenIdentityNumber { get; set; } = string.Empty;
    }
}
