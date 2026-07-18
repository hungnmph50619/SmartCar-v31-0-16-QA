using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCar.Domain.Entities
{
    public class Payment
    {
        public int PaymentID { get; set; }
        public int ReservationID { get; set; }
        public Reservation Reservation { get; set; } = null!;
        [MaxLength(30)] public string PaymentType { get; set; } = "Tiền thuê";
        [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal ProviderFeeAmount { get; set; }
        public bool ProviderFeeVerified { get; set; }
        [MaxLength(30)] public string Status { get; set; } = "Chờ thanh toán";
        [MaxLength(100)] public string? TransactionCode { get; set; }
        [MaxLength(100)] public string? IdempotencyKey { get; set; }
        [MaxLength(50)] public string? Provider { get; set; }
        [MaxLength(50)] public string? TransferContent { get; set; }
        [MaxLength(50)] public string? RelatedEntityType { get; set; }
        public int? RelatedEntityID { get; set; }
        public DateTime? CustomerReportedDate { get; set; }
        public bool IsSimulated { get; set; }
        [MaxLength(500)] public string? VerificationNote { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ConfirmedDate { get; set; }
        public DateTime? RefundedDate { get; set; }
        [Timestamp] public byte[]? RowVersion { get; set; }
    }
}
