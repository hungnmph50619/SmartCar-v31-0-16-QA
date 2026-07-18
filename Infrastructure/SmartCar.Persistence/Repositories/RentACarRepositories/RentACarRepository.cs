using Microsoft.EntityFrameworkCore;
using SmartCar.Application.Interfaces.RentACarInterfaces;
using SmartCar.Domain.BusinessRules;
using SmartCar.Domain.Entities;
using SmartCar.Persistence.Context;

namespace SmartCar.Persistence.Repositories.RentACarRepositories
{
    public class RentACarRepository : IRentACarRepository
    {
        private readonly CarBookContext _context;

        public RentACarRepository(CarBookContext context)
        {
            _context = context;
        }

        public async Task<List<RentACar>> GetAvailableCarsAsync(
            int locationId,
            bool available,
            DateTime? pickUpDateTime = null,
            DateTime? dropOffDateTime = null)
        {
            var carsAtLocation = await _context.RentACars
                .AsNoTracking()
                .Where(x => x.LocationID == locationId && x.Available == available &&
                            _context.PartnerVehicles.Any(p => p.CarID == x.CarID &&
                                p.VehiclePartnerApplication.Status == "Đã duyệt" && p.IsActive))
                .Include(x => x.Car).ThenInclude(y => y.Brand)
                .Include(x => x.Car).ThenInclude(y => y.CarPricings).ThenInclude(z => z.Pricing)
                .ToListAsync();

            if (!pickUpDateTime.HasValue || !dropOffDateTime.HasValue ||
                dropOffDateTime.Value <= pickUpDateTime.Value)
                return carsAtLocation;

            var carIds = carsAtLocation.Select(x => x.CarID).Distinct().ToList();
            var now = DateTime.UtcNow;
            var reservations = await _context.Reservations
                .AsNoTracking()
                .Where(x => carIds.Contains(x.CarID))
                .Select(x => new
                {
                    x.CarID, x.Status, x.HoldExpiresAt,
                    x.PickUpDate, x.DropOffDate, x.PickUpTime, x.DropOffTime
                })
                .ToListAsync();

            var blockedCarIds = reservations
                .Where(x => ReservationAvailabilityRules.IsBlocking(x.Status, x.HoldExpiresAt, now))
                .Where(x => ReservationAvailabilityRules.OverlapsWithTurnaroundBuffer(
                    x.PickUpDate.Date.Add(x.PickUpTime),
                    x.DropOffDate.Date.Add(x.DropOffTime),
                    pickUpDateTime.Value,
                    dropOffDateTime.Value))
                .Select(x => x.CarID)
                .ToHashSet();

            return carsAtLocation.Where(x => !blockedCarIds.Contains(x.CarID)).ToList();
        }
    }
}
