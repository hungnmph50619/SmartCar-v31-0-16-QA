using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCar.Domain.Entities
{
    public class RefundTransaction
    {
        public int RefundTransactionID { get; set; }
        public int ReservationID { get; set; }
        public Reservation Reservation { get; set; } = null!;
        public int? OriginalPaymentID { get; set; }
        public Payment? OriginalPayment { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
        [MaxLength(500)] public string Reason { get; set; } = string.Empty;
        [MaxLength(30)] public string Status { get; set; } = "Proposed";
        [MaxLength(100)] public string? IdempotencyKey { get; set; }
        [MaxLength(100)] public string? BankReference { get; set; }
        public int ProposedByAppUserID { get; set; }
        public int? ApprovedByAppUserID { get; set; }
        public DateTime ProposedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        [Timestamp] public byte[]? RowVersion { get; set; }
    }
}
