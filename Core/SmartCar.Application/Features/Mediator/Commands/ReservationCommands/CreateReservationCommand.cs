using MediatR;

namespace SmartCar.Application.Features.Mediator.Commands.ReservationCommands
{
    public class CreateReservationCommand : IRequest<int>
    {
        public int CustomerAppUserID { get; set; }
        public int PartnerVehicleID { get; set; }
        public int? VehiclePricingPlanID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int PickUpLocationID { get; set; }
        public int DropOffLocationID { get; set; }
        public string? PickUpAddressText { get; set; }
        public string? DropOffAddressText { get; set; }
        public int CarID { get; set; }
        public int Age { get; set; }
        public DateTime? BookingHolderDateOfBirth { get; set; }
        public int DriverLicenseYear { get; set; }
        public string RentalMode { get; set; } = "Tự lái";
        public string DeliveryMethod { get; set; } = "Nhận tại điểm giao xe";
        public int PassengerCount { get; set; }
        public string? Itinerary { get; set; }
        public string? SpecialLuggage { get; set; }
        public decimal? EstimatedDistanceKm { get; set; }
        public string? Description { get; set; }
        public DateTime PickUpDate { get; set; }
        public DateTime DropOffDate { get; set; }
        public TimeSpan PickUpTime { get; set; }
        public TimeSpan DropOffTime { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal CommissionRateSnapshot { get; set; }
        public decimal PlatformFeeAmount { get; set; }
        public decimal PartnerReceivableAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal ReservationDepositAmount { get; set; }
        public decimal SecurityDepositAmount { get; set; }
        public int BufferMinutesSnapshot { get; set; }
        public DateTime? HoldExpiresAt { get; set; }
        public DateTime? PartnerResponseExpiresAt { get; set; }
        public DateTime? PaymentExpiresAt { get; set; }
    }
}
