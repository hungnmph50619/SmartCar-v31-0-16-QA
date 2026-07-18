using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCar.Domain.Entities
{
    public class VehiclePricingPlan
    {
        public int VehiclePricingPlanID { get; set; }
        public int PartnerVehicleID { get; set; }
        public PartnerVehicle PartnerVehicle { get; set; } = null!;
        [MaxLength(30)] public string ServiceType { get; set; } = "Tự lái";
        [Column(TypeName = "decimal(18,2)")] public decimal? HourlyRate { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal? DailyRate { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal? TripRate { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal? PerKilometerRate { get; set; }
        public int MinimumHours { get; set; }
        public int MinimumDays { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal ReservationDepositAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal SecurityDepositAmount { get; set; }
        public int KilometerLimitPerDay { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal ExtraKilometerFee { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal LateReturnFeePerHour { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal DeliveryFee { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal DriverFee { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal WaitingFeePerHour { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal OvertimeFeePerHour { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal OutOfProvinceFee { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal OvernightFee { get; set; }
        [Column(TypeName = "decimal(8,4)")] public decimal WeekendMultiplier { get; set; } = 1m;
        [Column(TypeName = "decimal(8,4)")] public decimal HolidayMultiplier { get; set; } = 1m;
        public bool TollIncluded { get; set; }
        public bool ParkingIncluded { get; set; }
        [MaxLength(50)] public string CancellationPolicyVersion { get; set; } = "cancel-v31.0";
        [MaxLength(1000)] public string? FuelPolicy { get; set; }
        [MaxLength(1000)] public string? CleaningPolicy { get; set; }
        [MaxLength(2000)] public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime EffectiveFromUtc { get; set; } = DateTime.UtcNow;
        public DateTime? EffectiveToUtc { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Timestamp] public byte[]? RowVersion { get; set; }
    }
}
