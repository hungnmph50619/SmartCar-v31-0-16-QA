using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class SystemSetting
    {
        public int SystemSettingID { get; set; }
        [MaxLength(100)] public string SettingKey { get; set; } = string.Empty;
        [MaxLength(1000)] public string SettingValue { get; set; } = string.Empty;
        [MaxLength(30)] public string ValueType { get; set; } = "Integer";
        [MaxLength(500)] public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public int? UpdatedByAppUserID { get; set; }
    }
}
