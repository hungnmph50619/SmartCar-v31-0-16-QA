namespace SmartCar.Dto.AdminDashboardDtos
{
    public class AdminRiskCenterDto
    {
        public int NewFraudFlags { get; set; }
        public int OpenDisputes { get; set; }
        public int DeletedCars { get; set; }
        public int DeletedUsers { get; set; }
        public int AuditEventsToday { get; set; }
        public List<AdminFraudFlagDto> FraudFlags { get; set; } = new();
        public List<AdminAuditLogDto> AuditLogs { get; set; } = new();
        public List<AdminTrashItemDto> TrashItems { get; set; } = new();
    }

    public class AdminFraudFlagDto
    {
        public int FraudFlagID { get; set; }
        public int? AppUserID { get; set; }
        public int? ReservationID { get; set; }
        public string RuleCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int RiskScore { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class AdminAuditLogDto
    {
        public long AuditLogID { get; set; }
        public int? AppUserID { get; set; }
        public string ActorName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? EntityID { get; set; }
        public string? Note { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class UpdateFraudFlagDto
    {
        public string Status { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }

    public class AdminTrashItemDto
    {
        public string EntityType { get; set; } = string.Empty;
        public int EntityID { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public DateTime? DeletedAt { get; set; }
        public string? DeleteReason { get; set; }
        public bool CanRestore { get; set; }
    }
}
