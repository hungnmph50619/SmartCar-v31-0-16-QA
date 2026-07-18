using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class ArchivedRecord
    {
        public long ArchivedRecordID { get; set; }
        [MaxLength(100)] public string EntityName { get; set; } = string.Empty;
        [MaxLength(100)] public string EntityID { get; set; } = string.Empty;
        public string DataJson { get; set; } = string.Empty;
        public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;
        public int? ArchivedByAppUserID { get; set; }
    }
}
