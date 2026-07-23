using MediatR;
using SmartCar.Application.Features.Mediator.Commands.ReservationCommands;
using SmartCar.Application.Interfaces;
using SmartCar.Domain.BusinessRules;
using SmartCar.Domain.Entities;

namespace SmartCar.Application.Features.Mediator.Handlers.ReservationHandlers
{
    public class CreateReservationCommandHandler : IRequestHandler<CreateReservationCommand, int>
    {
        private readonly IRepository<Reservation> _repository;
        private readonly IRepository<PartnerVehicle> _partnerVehicleRepository;
        private readonly IRepository<Notification> _notificationRepository;

        public CreateReservationCommandHandler(
            IRepository<Reservation> repository,
            IRepository<PartnerVehicle> partnerVehicleRepository,
            IRepository<Notification> notificationRepository)
        {
            _repository = repository;
            _partnerVehicleRepository = partnerVehicleRepository;
            _notificationRepository = notificationRepository;
        }

        public async Task<int> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
        {
            var reservation = new Reservation
            {
                CustomerAppUserID = request.CustomerAppUserID,
                PartnerVehicleID = request.PartnerVehicleID,
                VehiclePricingPlanID = request.VehiclePricingPlanID,
                Age = request.Age,
                CarID = request.CarID,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                DriverLicenseYear = request.DriverLicenseYear,
                RentalMode = request.RentalMode,
                DeliveryMethod = request.DeliveryMethod,
                PickUpAddressText = string.IsNullOrWhiteSpace(request.PickUpAddressText) ? null : request.PickUpAddressText.Trim(),
                DropOffAddressText = string.IsNullOrWhiteSpace(request.DropOffAddressText) ? null : request.DropOffAddressText.Trim(),
                PassengerCount = request.PassengerCount,
                Itinerary = string.IsNullOrWhiteSpace(request.Itinerary) ? null : request.Itinerary.Trim(),
                SpecialLuggage = string.IsNullOrWhiteSpace(request.SpecialLuggage) ? null : request.SpecialLuggage.Trim(),
                EstimatedDistanceKm = request.EstimatedDistanceKm,
                DropOffLocationID = request.DropOffLocationID,
                Email = (request.Email ?? string.Empty).Trim().ToLowerInvariant(),
                Name = (request.Name ?? string.Empty).Trim(),
                Phone = (request.Phone ?? string.Empty).Trim(),
                PickUpLocationID = request.PickUpLocationID,
                Surname = (request.Surname ?? string.Empty).Trim(),
                PickUpDate = request.PickUpDate.Date,
                DropOffDate = request.DropOffDate.Date,
                PickUpTime = request.PickUpTime,
                DropOffTime = request.DropOffTime,
                TotalPrice = request.TotalPrice,
                CommissionRateSnapshot = request.CommissionRateSnapshot,
                PlatformFeeAmount = request.PlatformFeeAmount,
                PartnerReceivableAmount = request.PartnerReceivableAmount,
                DepositAmount = request.DepositAmount,
                ReservationDepositAmount = request.ReservationDepositAmount,
                SecurityDepositAmount = request.SecurityDepositAmount,
                DepositStatus = "Chưa đặt cọc",
                BufferMinutesSnapshot = request.BufferMinutesSnapshot,
                HoldExpiresAt = request.HoldExpiresAt,
                PartnerResponseExpiresAt = request.PartnerResponseExpiresAt,
                PaymentExpiresAt = request.PaymentExpiresAt,
                CreatedDate = DateTime.UtcNow,
                Status = ReservationStatuses.OwnerPending
            };

            await _repository.CreateAsync(reservation);

            var partnerVehicle = await _partnerVehicleRepository.GetByFilterAsync(
                x => x.PartnerVehicleID == request.PartnerVehicleID);
            if (partnerVehicle?.OwnerAppUserID > 0)
            {
                await _notificationRepository.CreateAsync(new Notification
                {
                    AppUserID = partnerVehicle.OwnerAppUserID,
                    Title = "Có yêu cầu thuê xe mới",
                    Message = $"Đơn #{reservation.ReservationID} đang chờ bạn phản hồi. Thời gian nhận xe: {request.PickUpDate:dd/MM/yyyy} {request.PickUpTime:hh\\:mm}.",
                    Type = "Reservation",
                    Link = $"/ReservationLookup/Details/{reservation.ReservationID}"
                });
            }

            return reservation.ReservationID;
        }
    }
}
