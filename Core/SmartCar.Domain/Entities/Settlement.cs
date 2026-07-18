using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCar.Domain.Entities
{
    public class Settlement
    {
        public int SettlementID { get; set; }
        public int ReservationID { get; set; }
        public Reservation Reservation { get; set; } = null!;
        [Column(TypeName = "decimal(18,2)")] public decimal GrossRental { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal PlatformFee { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal PaymentGatewayFee { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal RefundAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal CompensationAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal OwnerPayout { get; set; }
        [MaxLength(40)] public string Status { get; set; } = "Draft";
        [MaxLength(100)] public string? CreationIdempotencyKey { get; set; }
        [MaxLength(100)] public string? PayoutIdempotencyKey { get; set; }
        [MaxLength(100)] public string? PayoutTransactionCode { get; set; }
        public int? CreatedByAppUserID { get; set; }
        public int? ApprovedByAppUserID { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? PartnerReviewDueDate { get; set; }
        public DateTime? PartnerConfirmedDate { get; set; }
        [MaxLength(1000)] public string? PartnerDisputeReason { get; set; }
        public DateTime? PaidDate { get; set; }
        [Timestamp] public byte[]? RowVersion { get; set; }
    }
}
