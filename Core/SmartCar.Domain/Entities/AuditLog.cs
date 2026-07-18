using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class AuditLog
    {
        public long AuditLogID { get; set; }
        public int? AppUserID { get; set; }
        [MaxLength(100)] public string Action { get; set; } = string.Empty;
        [MaxLength(100)] public string EntityName { get; set; } = string.Empty;
        [MaxLength(100)] public string? EntityID { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        [MaxLength(1000)] public string? Note { get; set; }
        [MaxLength(64)] public string? IpAddress { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
