using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class VehicleAvailabilityBlock
    {
        public int VehicleAvailabilityBlockID { get; set; }
        public int PartnerVehicleID { get; set; }
        public PartnerVehicle PartnerVehicle { get; set; } = null!;
        public DateTime StartUtc { get; set; }
        public DateTime EndUtc { get; set; }
        [MaxLength(30)] public string BlockType { get; set; } = "OwnerPaused";
        [MaxLength(500)] public string Reason { get; set; } = string.Empty;
        public int CreatedByAppUserID { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CancelledAt { get; set; }
        [Timestamp] public byte[]? RowVersion { get; set; }
    }
}
