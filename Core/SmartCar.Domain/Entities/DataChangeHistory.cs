using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class DataChangeHistory
    {
        public long DataChangeHistoryID { get; set; }
        [MaxLength(100)] public string EntityName { get; set; } = string.Empty;
        [MaxLength(100)] public string EntityID { get; set; } = string.Empty;
        [MaxLength(50)] public string Action { get; set; } = string.Empty;
        public string? OldDataJson { get; set; }
        public string? NewDataJson { get; set; }
        [MaxLength(1000)] public string? Reason { get; set; }
        public int ChangedByAppUserID { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
