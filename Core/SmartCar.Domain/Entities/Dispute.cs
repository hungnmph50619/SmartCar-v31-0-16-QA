using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCar.Domain.Entities
{
    public class Dispute
    {
        public int DisputeID { get; set; }
        public int ReservationID { get; set; }
        public Reservation Reservation { get; set; } = null!;
        public int CreatedByAppUserID { get; set; }
        public int? AssignedStaffAppUserID { get; set; }
        [MaxLength(50)] public string Type { get; set; } = "Khác";
        [MaxLength(30)] public string Status { get; set; } = "Mới tiếp nhận";
        [MaxLength(3000)] public string Description { get; set; } = string.Empty;
        [MaxLength(2000)] public string? EvidenceUrls { get; set; }
        [MaxLength(3000)] public string? Resolution { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal CompensationAmount { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedDate { get; set; }
        [Timestamp] public byte[]? RowVersion { get; set; }
    }
}
