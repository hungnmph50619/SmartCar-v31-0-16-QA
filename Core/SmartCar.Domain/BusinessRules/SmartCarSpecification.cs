namespace SmartCar.Domain.BusinessRules
{
    public static class AccountTypes
    {
        public const string Customer = "Customer";
        public const string Partner = "Partner";
        public const string Staff = "Staff";
        public const string Admin = "Admin";
    }

    public static class ServiceTypes
    {
        public const string SelfDrive = "Tự lái";
        public const string WithDriver = "Có tài xế";
    }

    public static class VerificationStatuses
    {
        public const string Draft = "Bản nháp";
        public const string PendingReview = "Chờ duyệt";
        public const string SupplementRequired = "Yêu cầu bổ sung";
        public const string Verified = "Đã xác minh";
        public const string Rejected = "Bị từ chối";
        public const string ReverificationPending = "Chờ xác minh lại";
        public const string Escalated = "Chuyển Admin kiểm tra";
    }

    public static class VehicleApprovalStatuses
    {
        public const string Draft = "Bản nháp";
        public const string PendingReview = "Chờ duyệt";
        public const string NotApproved = "Chưa đạt";
        public const string Approved = "Đã duyệt";
    }

    public static class VehicleOperationStatuses
    {
        public const string Active = "Đang hoạt động";
        public const string Inactive = "Ngừng hoạt động";
        public const string Locked = "Bị khóa";
    }

    public static class ReservationStatuses
    {
        public const string OwnerPending = "Chờ chủ xe xác nhận";
        public const string PaymentPending = "Chờ thanh toán";
        public const string Confirmed = "Đã xác nhận";
        public const string HandoverPending = "Chờ giao xe";
        public const string InProgress = "Đang thuê";
        public const string ReturnPending = "Chờ trả xe";
        public const string SurchargeProposalPending = "Chờ đề xuất phụ phí";
        public const string SurchargeResponsePending = "Chờ khách phản hồi phụ phí";
        public const string SettlementPending = "Chờ đối soát";
        public const string Completed = "Hoàn thành";
        public const string Cancelled = "Đã hủy";
        public const string PaymentExpired = "Quá hạn thanh toán";
        public const string CustomerNoShow = "Khách không đến nhận";
        public const string PartnerNoShow = "Chủ xe không đến giao";
        public const string IncidentProcessing = "Đang xử lý sự cố";
        public const string Disputed = "Đang tranh chấp";
        public const string Refunding = "Đang hoàn tiền";
    }

    public static class PaymentTypes
    {
        public const string LegacyDeposit = "Tiền cọc";
        public const string ReservationDeposit = "Cọc giữ chỗ";
        public const string SecurityDeposit = "Cọc bảo đảm";
        public const string Rental = "Tiền thuê";
        public const string AdditionalCharge = "Phụ phí";
    }

    public static class AdditionalChargeStatuses
    {
        public const string Draft = "Draft";
        public const string Submitted = "Submitted";
        public const string CustomerAccepted = "CustomerAccepted";
        public const string CustomerRejected = "CustomerRejected";
        public const string StaffReview = "StaffReview";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Collected = "Collected";
    }

    public static class SettlementStatuses
    {
        public const string Draft = "Draft";
        public const string PartnerReview = "PartnerReview";
        public const string PartnerDisputed = "PartnerDisputed";
        public const string AwaitingApproval = "AwaitingApproval";
        public const string Paid = "Paid";
    }

    public static class SmartCarSettingKeys
    {
        public const string OtpExpiryMinutes = nameof(OtpExpiryMinutes);
        public const string OtpMaxAttempts = nameof(OtpMaxAttempts);
        public const string OtpResendCooldownSeconds = nameof(OtpResendCooldownSeconds);
        public const string OtpMaxPerHour = nameof(OtpMaxPerHour);
        public const string OtpMaxPerDay = nameof(OtpMaxPerDay);
        public const string BookingHoldMinutes = nameof(BookingHoldMinutes);
        public const string PartnerResponseMinutes = nameof(PartnerResponseMinutes);
        public const string PaymentWindowMinutes = nameof(PaymentWindowMinutes);
        public const string SelfDriveMinHours = nameof(SelfDriveMinHours);
        public const string DriverServiceMinHours = nameof(DriverServiceMinHours);
        public const string SelfDriveBufferMinutes = nameof(SelfDriveBufferMinutes);
        public const string DriverServiceBufferMinutes = nameof(DriverServiceBufferMinutes);
        public const string MaxAdvanceBookingDays = nameof(MaxAdvanceBookingDays);
        public const string DocumentWarningDays = nameof(DocumentWarningDays);
        public const string DocumentCriticalWarningDays = nameof(DocumentCriticalWarningDays);
        public const string SurchargeProposalHours = nameof(SurchargeProposalHours);
        public const string SurchargeResponseHours = nameof(SurchargeResponseHours);
        public const string CustomerNoShowMinutes = nameof(CustomerNoShowMinutes);
        public const string DisputeOpenDays = nameof(DisputeOpenDays);
        public const string DisputeAppealDays = nameof(DisputeAppealDays);
        public const string ReviewWindowDays = nameof(ReviewWindowDays);
        public const string TrafficFineClaimDays = nameof(TrafficFineClaimDays);
        public const string StaffSurchargeApprovalLimit = nameof(StaffSurchargeApprovalLimit);
    }
}
