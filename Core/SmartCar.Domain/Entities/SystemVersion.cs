using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class SystemVersion
    {
        public int SystemVersionID { get; set; }
        [MaxLength(30)] public string ApplicationVersion { get; set; } = string.Empty;
        [MaxLength(30)] public string DatabaseVersion { get; set; } = string.Empty;
        public DateTime ReleasedDate { get; set; } = DateTime.UtcNow;
        public bool IsCurrent { get; set; }
        [MaxLength(1000)] public string? Notes { get; set; }
    }
}
