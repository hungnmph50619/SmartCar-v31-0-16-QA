using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCar.Domain.Entities
{
    public class PlatformFeeSetting
    {
        public int PlatformFeeSettingID { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal VehiclePartnerCommissionPercent { get; set; } = 20.00m;

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
        public int? UpdatedByAppUserID { get; set; }
        public AppUser? UpdatedByAppUser { get; set; }
    }
}
