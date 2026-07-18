using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCar.Domain.Entities
{
    public class AdditionalCharge
    {
        public int AdditionalChargeID { get; set; }
        public int ReservationID { get; set; }
        public Reservation Reservation { get; set; } = null!;
        [MaxLength(50)] public string ChargeType { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
        [MaxLength(1000)] public string Reason { get; set; } = string.Empty;
        [MaxLength(4000)] public string? EvidenceUrls { get; set; }
        [MaxLength(40)] public string Status { get; set; } = "Chờ khách xác nhận";
        public int CreatedByAppUserID { get; set; }
        public int? PaymentID { get; set; }
        public Payment? Payment { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedDate { get; set; }
        public DateTime? CustomerResponseDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public int? ResolvedByAppUserID { get; set; }
    }
}
