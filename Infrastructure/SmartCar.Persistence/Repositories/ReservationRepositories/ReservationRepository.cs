using Microsoft.EntityFrameworkCore;
using SmartCar.Application.Interfaces.ReservationInterfaces;
using SmartCar.Domain.BusinessRules;
using SmartCar.Domain.Entities;
using SmartCar.Persistence.Context;

namespace SmartCar.Persistence.Repositories.ReservationRepositories
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly CarBookContext _context;

        public ReservationRepository(CarBookContext context)
        {
            _context = context;
        }

        public async Task<List<Reservation>> GetAllWithDetailsAsync()
        {
            return await _context.Reservations
                .AsNoTracking()
                .Include(x => x.Car)
                    .ThenInclude(x => x.Brand)
                .Include(x => x.PickUpLocation)
                .Include(x => x.DropOffLocation)
                .Include(x => x.PartnerVehicle).ThenInclude(x => x.OwnerAppUser)
                .Include(x => x.PartnerVehicle).ThenInclude(x => x.VehiclePartnerApplication)
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.ReservationID)
                .ToListAsync();
        }

        public async Task<Reservation?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Reservations
                .AsNoTracking()
                .Include(x => x.Car)
                    .ThenInclude(x => x.Brand)
                .Include(x => x.PickUpLocation)
                .Include(x => x.DropOffLocation)
                .Include(x => x.PartnerVehicle).ThenInclude(x => x.OwnerAppUser)
                .Include(x => x.PartnerVehicle).ThenInclude(x => x.VehiclePartnerApplication)
                .FirstOrDefaultAsync(x => x.ReservationID == id);
        }

        public async Task<Reservation?> GetForTrackingAsync(int id, string contact)
        {
            var normalizedContact = contact.Trim();

            return await _context.Reservations
                .AsNoTracking()
                .Include(x => x.Car)
                    .ThenInclude(x => x.Brand)
                .Include(x => x.PickUpLocation)
                .Include(x => x.DropOffLocation)
                .Include(x => x.PartnerVehicle).ThenInclude(x => x.OwnerAppUser)
                .Include(x => x.PartnerVehicle).ThenInclude(x => x.VehiclePartnerApplication)
                .FirstOrDefaultAsync(x => x.ReservationID == id &&
                    (x.Email == normalizedContact || x.Phone == normalizedContact));
        }

        public async Task<List<Reservation>> GetByContactAsync(string contact)
        {
            var normalizedContact = contact.Trim();
            var normalizedEmail = normalizedContact.ToLower();

            return await _context.Reservations
                .AsNoTracking()
                .Include(x => x.Car)
                    .ThenInclude(x => x.Brand)
                .Include(x => x.PickUpLocation)
                .Include(x => x.DropOffLocation)
                .Include(x => x.PartnerVehicle).ThenInclude(x => x.OwnerAppUser)
                .Include(x => x.PartnerVehicle).ThenInclude(x => x.VehiclePartnerApplication)
                .Where(x => x.Phone == normalizedContact || x.Email.ToLower() == normalizedEmail)
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.ReservationID)
                .ToListAsync();
        }

        public async Task<bool> IsCarAvailableAsync(
            int carId,
            DateTime pickUpDateTime,
            DateTime dropOffDateTime,
            int? excludeReservationId = null,
            string? serviceType = null)
        {
            if (dropOffDateTime <= pickUpDateTime)
            {
                return false;
            }

            var now = DateTime.UtcNow;
            var reservations = await _context.Reservations
                .AsNoTracking()
                .Where(x => x.CarID == carId &&
                            (!excludeReservationId.HasValue || x.ReservationID != excludeReservationId.Value))
                .Select(x => new
                {
                    x.Status,
                    x.HoldExpiresAt,
                    x.PartnerResponseExpiresAt,
                    x.PaymentExpiresAt,
                    x.RentalMode,
                    x.BufferMinutesSnapshot,
                    x.PickUpDate,
                    x.DropOffDate,
                    x.PickUpTime,
                    x.DropOffTime
                })
                .ToListAsync();

            var reservationsAvailable = reservations
                .Where(x => ReservationAvailabilityRules.IsBlocking(x.Status, x.HoldExpiresAt, x.PartnerResponseExpiresAt, x.PaymentExpiresAt, now))
                .All(x =>
                {
                    var existingStart = x.PickUpDate.Date.Add(x.PickUpTime);
                    var existingEnd = x.DropOffDate.Date.Add(x.DropOffTime);
                    var buffer = x.BufferMinutesSnapshot > 0
                        ? x.BufferMinutesSnapshot
                        : Math.Max(ReservationAvailabilityRules.GetBufferMinutes(x.RentalMode), ReservationAvailabilityRules.GetBufferMinutes(serviceType));
                    return !ReservationAvailabilityRules.OverlapsWithTurnaroundBuffer(
                        existingStart, existingEnd, pickUpDateTime, dropOffDateTime, buffer);
                });
            if (!reservationsAvailable) return false;

            var partnerVehicleId = await _context.PartnerVehicles.AsNoTracking()
                .Where(x => x.CarID == carId)
                .Select(x => (int?)x.PartnerVehicleID)
                .FirstOrDefaultAsync();
            if (!partnerVehicleId.HasValue) return false;

            return !await _context.VehicleAvailabilityBlocks.AsNoTracking().AnyAsync(x =>
                x.PartnerVehicleID == partnerVehicleId.Value &&
                x.IsActive &&
                x.StartUtc < dropOffDateTime &&
                x.EndUtc > pickUpDateTime);
        }

        public async Task<decimal> GetDailyPriceAsync(int carId)
        {
            return await _context.CarPricings
                .AsNoTracking()
                .Where(x => x.CarID == carId && x.Pricing.Name == "Theo ngày")
                .Select(x => x.Amount)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> CanRentAtLocationAsync(
            int carId,
            int pickUpLocationId,
            int dropOffLocationId)
        {
            var locationsExist = await _context.Locations
                .AsNoTracking()
                .CountAsync(x => x.LocationID == pickUpLocationId || x.LocationID == dropOffLocationId);

            var expectedLocationCount = pickUpLocationId == dropOffLocationId ? 1 : 2;
            if (locationsExist != expectedLocationCount)
            {
                return false;
            }

            return await _context.RentACars
                .AsNoTracking()
                .AnyAsync(x => x.CarID == carId &&
                               x.LocationID == pickUpLocationId &&
                               x.Available &&
                               _context.PartnerVehicles.Any(p => p.CarID == carId && p.VehiclePartnerApplication.Status == "Đã duyệt" && p.IsActive));
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation is null)
            {
                return false;
            }

            reservation.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
