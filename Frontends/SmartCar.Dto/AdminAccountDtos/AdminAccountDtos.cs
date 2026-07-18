namespace SmartCar.Dto.AdminAccountDtos
{
    public class AdminAccountListDto
    {
        public string SelectedType { get; set; } = "Khách hàng";
        public int CustomerCount { get; set; }
        public int PartnerCount { get; set; }
        public int StaffCount { get; set; }
        public int AdminCount { get; set; }
        public List<AdminAccountItemDto> Accounts { get; set; } = new();
    }

    public class AdminAccountItemDto
    {
        public int AppUserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string AccountType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public string? LockType { get; set; }
        public string? LockReason { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string VerificationStatus { get; set; } = "Không áp dụng";
        public int ViolationCount { get; set; }
        public int OpenReservationCount { get; set; }
        public string? Province { get; set; }
        public string? Ward { get; set; }
        public string? Gender { get; set; }
        public int? Age { get; set; }
    }

    public class AdminAccountStatusDto
    {
        public bool IsActive { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string LockMode { get; set; } = "Permanent"; // Temporary / Permanent / Unlock
        public int? LockDays { get; set; }
    }

    public class AdminRevokeSessionDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class AdminRequestReverificationDto
    {
        public string Reason { get; set; } = string.Empty;
        public bool RestrictBooking { get; set; } = true;
        public string Severity { get; set; } = "Trung bình";
    }

    public class AdminAccountDetailDto
    {
        public AdminAccountItemDto Account { get; set; } = new();
        public AdminCustomerVerificationDetailDto? Verification { get; set; }
        public List<AdminReservationSummaryDto> Reservations { get; set; } = new();
        public List<AdminAuditLogDto> AuditLogs { get; set; } = new();
        public List<AdminStaffIssueDto> StaffIssues { get; set; } = new();
    }

    public class AdminCustomerVerificationDetailDto
    {
        public int UserVerificationID { get; set; }
        public string Status { get; set; } = "Chưa xác minh";
        public string? LegalFullName { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? Age { get; set; }
        public string? CitizenIdMasked { get; set; }
        public DateTime? CitizenIdIssuedDate { get; set; }
        public DateTime? CitizenIdExpiryDate { get; set; }
        public string? CitizenIdAddress { get; set; }
        public string? PermanentProvince { get; set; }
        public string? PermanentWard { get; set; }
        public string? PermanentDetail { get; set; }
        public string? PermanentAddress { get; set; }
        public bool CurrentAddressSameAsPermanent { get; set; }
        public string? CurrentProvince { get; set; }
        public string? CurrentWard { get; set; }
        public string? CurrentDetail { get; set; }
        public string? CurrentAddress { get; set; }
        public string? DriverLicenseNumber { get; set; }
        public string? DriverLicenseClass { get; set; }
        public DateTime? DriverLicenseIssuedDate { get; set; }
        public DateTime? DriverLicenseExpiry { get; set; }
        public string? CitizenIdFrontUrl { get; set; }
        public string? CitizenIdBackUrl { get; set; }
        public string? DriverLicenseUrl { get; set; }
        public string? PortraitUrl { get; set; }
        public int? ReviewedByAppUserID { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ReviewedDate { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class AdminReservationSummaryDto
    {
        public int ReservationID { get; set; }
        public string CarName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime PickUpDate { get; set; }
        public DateTime DropOffDate { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class AdminAuditLogDto
    {
        public long AuditLogID { get; set; }
        public string ActorName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? EntityID { get; set; }
        public string? Note { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class AdminStaffIssueDto
    {
        public int StaffOperationalIssueID { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string IssueType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}
