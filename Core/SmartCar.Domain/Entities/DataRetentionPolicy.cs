using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class DataRetentionPolicy
    {
        public int DataRetentionPolicyID { get; set; }
        [MaxLength(100)] public string EntityName { get; set; } = string.Empty;
        public int RetentionDays { get; set; }
        public bool AllowHardDelete { get; set; }
        public bool RequireAnonymization { get; set; }
        public bool IsActive { get; set; } = true;
        [MaxLength(1000)] public string? LegalBasis { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public int? UpdatedByAppUserID { get; set; }
    }
}
