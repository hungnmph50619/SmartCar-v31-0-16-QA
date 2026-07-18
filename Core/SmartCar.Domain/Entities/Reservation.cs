using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCar.Domain.Entities
{
    public class Reservation
    {
        public int ReservationID { get; set; }

        public int CustomerAppUserID { get; set; }
        public AppUser CustomerAppUser { get; set; } = null!;

        public int PartnerVehicleID { get; set; }
        public PartnerVehicle PartnerVehicle { get; set; } = null!;

        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        public int? PickUpLocationID { get; set; }
        public int? DropOffLocationID { get; set; }

        public int CarID { get; set; }
        public Car Car { get; set; } = null!;

        public int Age { get; set; }
        public int DriverLicenseYear { get; set; }

        [MaxLength(30)]
        public string RentalMode { get; set; } = "Tự lái";

        [MaxLength(40)]
        public string DeliveryMethod { get; set; } = "Nhận tại điểm giao xe";

        
        public int? VehiclePricingPlanID { get; set; }
        public VehiclePricingPlan? VehiclePricingPlan { get; set; }
        [MaxLength(500)] public string? PickUpAddressText { get; set; }
        [MaxLength(500)] public string? DropOffAddressText { get; set; }
        public int PassengerCount { get; set; }
        [MaxLength(2000)] public string? Itinerary { get; set; }
        [MaxLength(1000)] public string? SpecialLuggage { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal? EstimatedDistanceKm { get; set; }

        public string? Description { get; set; }
        public Location? PickUpLocation { get; set; }
        public Location? DropOffLocation { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Chờ chủ xe xác nhận";

        [MaxLength(1000)]
        public string? OwnerNote { get; set; }

        public DateTime? OwnerResponseDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime PickUpDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime DropOffDate { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan PickUpTime { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan DropOffTime { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal CommissionRateSnapshot { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PlatformFeeAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PartnerReceivableAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DepositAmount { get; set; }

                [Column(TypeName = "decimal(18,2)")] public decimal ReservationDepositAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal SecurityDepositAmount { get; set; }

[MaxLength(40)]
        public string DepositStatus { get; set; } = "Chưa đặt cọc";

        [MaxLength(50)] public string CancellationPolicyVersion { get; set; } = "cancel-v31.0";
        [MaxLength(50)] public string TermsVersion { get; set; } = "v1";
        [MaxLength(4000)] public string? PriceSnapshotJson { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal CancellationFeeAmount { get; set; }
        [MaxLength(500)] public string? CancellationReason { get; set; }
        public int? CancelledByAppUserID { get; set; }
        public DateTime? CancelledDate { get; set; }
        public DateTime? HoldExpiresAt { get; set; }
        public DateTime? PartnerResponseExpiresAt { get; set; }
        public DateTime? PaymentExpiresAt { get; set; }
        public DateTime? SurchargeProposalExpiresAt { get; set; }
        public DateTime? SurchargeResponseExpiresAt { get; set; }
        public DateTime? ReviewExpiresAt { get; set; }
        public int BufferMinutesSnapshot { get; set; }
        public int StateVersion { get; set; }
        [Timestamp] public byte[]? RowVersion { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
    }
}
