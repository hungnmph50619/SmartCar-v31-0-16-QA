using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCar.Domain.Entities
{
    public class PartnerVehicle
    {
        public int PartnerVehicleID { get; set; }
        public int CarID { get; set; }
        public Car Car { get; set; } = null!;
        public int OwnerAppUserID { get; set; }
        public AppUser OwnerAppUser { get; set; } = null!;
        public int VehiclePartnerApplicationID { get; set; }
        public VehiclePartnerApplication VehiclePartnerApplication { get; set; } = null!;

        [Column(TypeName = "decimal(5,2)")]
        public decimal? CommissionRateOverride { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DepositAmount { get; set; }
        public bool IsActive { get; set; } = true;
        [MaxLength(30)] public string ApprovalStatus { get; set; } = "Đã duyệt";
        [MaxLength(30)] public string OperationStatus { get; set; } = "Đang hoạt động";
        [MaxLength(50)] public string? InactiveReason { get; set; }
        [MaxLength(500)] public string? PauseReason { get; set; }
        public DateTime ListedDate { get; set; } = DateTime.UtcNow;
        public DateTime? OperationStatusChangedAt { get; set; }
        public int? OperationStatusChangedByAppUserID { get; set; }
        [Timestamp] public byte[]? RowVersion { get; set; }
    }
}
