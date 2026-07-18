using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class BankAccountChangeRequest
    {
        public int BankAccountChangeRequestID { get; set; }
        public int VehiclePartnerProfileID { get; set; }
        public VehiclePartnerProfile VehiclePartnerProfile { get; set; } = null!;
        [MaxLength(100)] public string OldBankName { get; set; } = string.Empty;
        [MaxLength(50)] public string OldAccountNumber { get; set; } = string.Empty;
        [MaxLength(200)] public string OldAccountHolder { get; set; } = string.Empty;
        [MaxLength(100)] public string NewBankName { get; set; } = string.Empty;
        [MaxLength(50)] public string NewAccountNumber { get; set; } = string.Empty;
        [MaxLength(200)] public string NewAccountHolder { get; set; } = string.Empty;
        [MaxLength(100)] public string? NewBankBranch { get; set; }
        [MaxLength(30)] public string Status { get; set; } = "Chờ duyệt";
        [MaxLength(1000)] public string? ReviewReason { get; set; }
        public int RequestedByAppUserID { get; set; }
        public int? ReviewedByAppUserID { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
        [Timestamp] public byte[]? RowVersion { get; set; }
    }
}
