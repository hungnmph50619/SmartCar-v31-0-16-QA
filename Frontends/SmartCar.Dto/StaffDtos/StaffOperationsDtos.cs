namespace SmartCar.Dto.StaffDtos
{
    public class StaffWorkQueueDto
    {
        public int PendingCustomerVerifications { get; set; }
        public int PendingVehicleApplications { get; set; }
        public int PendingVehicleDocuments { get; set; }
        public int PendingPayments { get; set; }
        public int OpenIncidents { get; set; }
        public int OpenDisputes { get; set; }
        public int OverdueTrafficFines { get; set; }
        public int PendingSettlements { get; set; }
        public int NeedActionCount { get; set; }
        public int InProgressCount { get; set; }
        public int ErrorCount { get; set; }
        public int CompletedCount { get; set; }
        public int CurrentStaffAppUserID { get; set; }
        public List<StaffQueueItemDto> Items { get; set; } = new();
    }

    public class StaffQueueItemDto
    {
        public string QueueType { get; set; } = string.Empty;
        public int EntityID { get; set; }
        public int? ReservationID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Bucket { get; set; } = "Cần xử lý";
        public string? IssueReason { get; set; }
        public bool IsOverdue { get; set; }
        public bool IsActionable { get; set; } = true;
        public string Priority { get; set; } = "Trung bình";
        public DateTime CreatedDate { get; set; }
        public int WaitingMinutes { get; set; }
        public int? WorkItemClaimID { get; set; }
        public int? AssignedStaffAppUserID { get; set; }
        public string? AssignedStaffName { get; set; }
        public bool CanClaim { get; set; }
        public DateTime? DueAt { get; set; }
        public string ActionUrl { get; set; } = string.Empty;
    }
}


namespace SmartCar.Dto.StaffDtos
{
    public class StaffVerificationReviewDto
    {
        public int UserVerificationID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string LegalFullName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string CitizenIdAddress { get; set; } = string.Empty;
        public string PermanentProvince { get; set; } = string.Empty;
        public string PermanentWard { get; set; } = string.Empty;
        public string PermanentDetail { get; set; } = string.Empty;
        public string PermanentAddress { get; set; } = string.Empty;
        public bool CurrentAddressSameAsPermanent { get; set; }
        public string CurrentProvince { get; set; } = string.Empty;
        public string CurrentWard { get; set; } = string.Empty;
        public string CurrentDetail { get; set; } = string.Empty;
        public string CurrentAddress { get; set; } = string.Empty;
        public string DriverLicenseNumber { get; set; } = string.Empty;
        public string DriverLicenseClass { get; set; } = string.Empty;
        public string CitizenIdMasked { get; set; } = string.Empty;
        public DateTime? CitizenIdIssuedDate { get; set; }
        public DateTime? CitizenIdExpiryDate { get; set; }
        public string CitizenIdFrontUrl { get; set; } = string.Empty;
        public string CitizenIdBackUrl { get; set; } = string.Empty;
        public string DriverLicenseUrl { get; set; } = string.Empty;
        public string PortraitUrl { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public DateTime? DriverLicenseIssuedDate { get; set; }
        public DateTime? DriverLicenseExpiry { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}

namespace SmartCar.Dto.StaffDtos
{
    public class StaffVehicleDocumentReviewDto
    {
        public int VehicleDocumentID { get; set; }
        public int PartnerVehicleID { get; set; }
        public string CarName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public DateTime? IssuedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
