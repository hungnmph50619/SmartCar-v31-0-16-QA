using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCar.Domain.Entities
{
    public class CommissionTransaction
    {
        public int CommissionTransactionID { get; set; }

        public int ReservationID { get; set; }
        public Reservation Reservation { get; set; } = null!;

        public int SettlementID { get; set; }
        public Settlement Settlement { get; set; } = null!;

        public int PartnerVehicleID { get; set; }
        public PartnerVehicle PartnerVehicle { get; set; } = null!;

        public int PartnerAppUserID { get; set; }
        public AppUser PartnerAppUser { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrossAmount { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal CommissionRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PartnerNetAmount { get; set; }

        [MaxLength(40)]
        public string Status { get; set; } = "Chờ đối soát";

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ReconciledDate { get; set; }
        public DateTime? PaidDate { get; set; }

        [MaxLength(100)]
        public string? BankReference { get; set; }

        [MaxLength(1000)]
        public string? Note { get; set; }
    }
}
