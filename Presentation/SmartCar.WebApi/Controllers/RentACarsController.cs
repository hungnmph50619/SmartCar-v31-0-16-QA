using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.BusinessRules;
using SmartCar.Domain.Entities;
using SmartCar.Domain.Time;
using SmartCar.Dto.RentACarDtos;
using SmartCar.Persistence.Context;
using System.Globalization;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RentACarsController : ControllerBase
    {
        private readonly CarBookContext _context;
        public RentACarsController(CarBookContext context) => _context = context;

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetRentACarListByLocation(
            int locationID,
            bool available = true,
            string? pickUpDate = null,
            string? dropOffDate = null,
            string? pickUpTime = null,
            string? dropOffTime = null,
            int? seats = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? transmission = null,
            string? fuel = null,
            string? vehicleType = null,
            int? radiusKm = null,
            double? searchLatitude = null,
            double? searchLongitude = null)
        {
            if (locationID <= 0) return BadRequest("Địa điểm nhận xe không hợp lệ.");

            var selectedLocation = await _context.Locations.AsNoTracking()
                .FirstOrDefaultAsync(x => x.LocationID == locationID && x.IsActive);
            if (selectedLocation is null) return BadRequest("Địa điểm nhận xe không tồn tại hoặc đang tạm ngừng.");

            if (searchLatitude.HasValue != searchLongitude.HasValue)
                return BadRequest("Tọa độ điểm nhận xe phải gồm cả vĩ độ và kinh độ.");
            if ((searchLatitude.HasValue && (searchLatitude.Value < -90 || searchLatitude.Value > 90)) ||
                (searchLongitude.HasValue && (searchLongitude.Value < -180 || searchLongitude.Value > 180)))
                return BadRequest("Tọa độ điểm nhận xe không hợp lệ.");

            var hasExactSearchPoint = searchLatitude.HasValue && searchLongitude.HasValue;
            var effectiveRadius = Math.Clamp(radiusKm ?? selectedLocation.SearchRadiusKm, 1, 100);
            var activeLocations = await _context.Locations.AsNoTracking()
                .Where(x => x.IsActive)
                .ToListAsync();

            var distanceByLocation = activeLocations.ToDictionary(
                x => x.LocationID,
                x => hasExactSearchPoint
                    ? CalculateDistanceKm(searchLatitude.Value, searchLongitude.Value, x)
                    : CalculateDistanceKm(selectedLocation, x));

            var nearbyLocationIds = distanceByLocation
                .Where(x => (!hasExactSearchPoint && x.Key == locationID) || x.Value <= effectiveRadius)
                .Select(x => x.Key)
                .ToList();

            if (!hasExactSearchPoint && !nearbyLocationIds.Contains(locationID)) nearbyLocationIds.Add(locationID);

            DateTime? pickUpDateTime = null;
            DateTime? dropOffDateTime = null;
            var hasAnyRentalTime = !string.IsNullOrWhiteSpace(pickUpDate) ||
                                   !string.IsNullOrWhiteSpace(dropOffDate) ||
                                   !string.IsNullOrWhiteSpace(pickUpTime) ||
                                   !string.IsNullOrWhiteSpace(dropOffTime);
            if (hasAnyRentalTime)
            {
                if (!TryParseDate(pickUpDate, out var pickDate)) return BadRequest("Ngày nhận xe không đúng định dạng.");
                if (!TryParseDate(dropOffDate, out var dropDate)) return BadRequest("Ngày trả xe không đúng định dạng.");
                if (!TryParseTime(pickUpTime, out var pickTime)) return BadRequest("Giờ nhận xe không đúng định dạng.");
                if (!TryParseTime(dropOffTime, out var dropTime)) return BadRequest("Giờ trả xe không đúng định dạng.");
                pickUpDateTime = VietnamTime.ComposeLocal(pickDate, pickTime);
                dropOffDateTime = VietnamTime.ComposeLocal(dropDate, dropTime);
                if (VietnamTime.LocalToUtc(pickUpDateTime.Value) < DateTime.UtcNow.AddMinutes(-1))
                    return BadRequest("Thời gian nhận xe không được ở trong quá khứ.");
                if (dropOffDateTime <= pickUpDateTime) return BadRequest("Thời gian trả xe phải sau thời gian nhận xe.");
            }

            var baseQuery = _context.RentACars.AsNoTracking()
                .Where(x => nearbyLocationIds.Contains(x.LocationID) && x.Available == available)
                .Where(x => _context.PartnerVehicles.Any(p => p.CarID == x.CarID && p.IsActive && p.VehiclePartnerApplication.Status == "Đã duyệt"))
                .Include(x => x.Location)
                .Include(x => x.Car).ThenInclude(x => x.Brand)
                .Include(x => x.Car).ThenInclude(x => x.CarPricings).ThenInclude(x => x.Pricing)
                .Include(x => x.Car).ThenInclude(x => x.Reviews)
                .AsQueryable();

            if (seats.HasValue && seats.Value > 0) baseQuery = baseQuery.Where(x => x.Car.Seat >= seats.Value);
            if (!string.IsNullOrWhiteSpace(transmission)) baseQuery = baseQuery.Where(x => x.Car.Transmission == transmission);
            if (!string.IsNullOrWhiteSpace(fuel)) baseQuery = baseQuery.Where(x => x.Car.Fuel == fuel);

            var cars = await baseQuery.ToListAsync();
            if (pickUpDateTime.HasValue && dropOffDateTime.HasValue)
            {
                var carIds = cars.Select(x => x.CarID).Distinct().ToList();
                var now = DateTime.UtcNow;
                var schedules = await _context.Reservations.AsNoTracking()
                    .Where(x => carIds.Contains(x.CarID))
                    .Select(x => new { x.CarID, x.Status, x.HoldExpiresAt, x.PickUpDate, x.DropOffDate, x.PickUpTime, x.DropOffTime })
                    .ToListAsync();
                var blocked = schedules
                    .Where(x => ReservationAvailabilityRules.IsBlocking(x.Status, x.HoldExpiresAt, now))
                    .Where(x => ReservationAvailabilityRules.OverlapsWithTurnaroundBuffer(
                        x.PickUpDate.Date.Add(x.PickUpTime),
                        x.DropOffDate.Date.Add(x.DropOffTime),
                        pickUpDateTime.Value,
                        dropOffDateTime.Value))
                    .Select(x => x.CarID).ToHashSet();
                cars = cars.Where(x => !blocked.Contains(x.CarID)).ToList();
            }

            var candidateCarIds = cars.Select(c => c.CarID).Distinct().ToList();
            var partnerInfo = await _context.PartnerVehicles.AsNoTracking()
                .Include(x => x.VehiclePartnerApplication)
                .Where(x => candidateCarIds.Contains(x.CarID) && x.IsActive && x.VehiclePartnerApplication.Status == "Đã duyệt")
                .ToDictionaryAsync(x => x.CarID);

            var expiredCarIds = await _context.VehicleDocuments.AsNoTracking()
                .Where(x => x.Status == "Đã xác minh" && x.ExpiryDate.HasValue && x.ExpiryDate.Value.Date < VietnamTime.Today)
                .Where(x => candidateCarIds.Contains(x.PartnerVehicle.CarID))
                .Select(x => x.PartnerVehicle.CarID)
                .Distinct()
                .ToListAsync();

            var maintenanceRows = await _context.MaintenanceRecords.AsNoTracking()
                .Where(x => candidateCarIds.Contains(x.PartnerVehicle.CarID))
                .Select(x => new { x.PartnerVehicle.CarID, x.MaintenanceRecordID, x.MaintenanceDate, x.HasUnresolvedSafetyIssue })
                .ToListAsync();
            var unsafeCarIds = maintenanceRows
                .GroupBy(x => x.CarID)
                .Where(g => g.OrderByDescending(x => x.MaintenanceDate).ThenByDescending(x => x.MaintenanceRecordID).First().HasUnresolvedSafetyIssue)
                .Select(g => g.Key)
                .ToHashSet();
            var blockedOperationalCars = expiredCarIds.Concat(unsafeCarIds).ToHashSet();
            cars = cars.Where(x => partnerInfo.ContainsKey(x.CarID) && !blockedOperationalCars.Contains(x.CarID)).ToList();

            var result = cars.Select(x =>
            {
                var dailyPrice = x.Car.CarPricings
                    .Where(p => p.Pricing != null && p.Pricing.Name == "Theo ngày")
                    .Select(p => p.Amount).FirstOrDefault();
                var validReviews = x.Car.Reviews?.Where(r => !r.IsDeleted).ToList() ?? new();
                partnerInfo.TryGetValue(x.CarID, out var partner);
                var distance = distanceByLocation.TryGetValue(x.LocationID, out var value) ? value : 0d;
                return new FilterRentACarDto
                {
                    carID = x.CarID,
                    Brand = x.Car.Brand?.Name ?? string.Empty,
                    Model = x.Car.Model,
                    Amount = dailyPrice,
                    CoverImageUrl = x.Car.CoverImageUrl,
                    LocationID = x.LocationID,
                    LocationName = x.Location?.Name ?? string.Empty,
                    LocationAddress = BuildFullAddress(x.Location),
                    Latitude = x.Location?.Latitude,
                    Longitude = x.Location?.Longitude,
                    DistanceKm = Math.Round(distance, 1),
                    Seat = x.Car.Seat,
                    Transmission = x.Car.Transmission,
                    Fuel = x.Car.Fuel,
                    VehicleType = partner?.VehiclePartnerApplication.RentalMode ?? "Tự lái",
                    Rating = validReviews.Count == 0 ? 0 : Math.Round((decimal)validReviews.Average(r => r.RaytingValue), 1),
                    RatingCount = validReviews.Count,
                    DepositAmount = partner?.VehiclePartnerApplication.RentalMode == "Có tài xế" ? 0 : partner?.DepositAmount ?? 0,
                    Available = true
                };
            })
            .Where(x => !minPrice.HasValue || x.Amount >= minPrice.Value)
            .Where(x => !maxPrice.HasValue || x.Amount <= maxPrice.Value)
            .Where(x => string.IsNullOrWhiteSpace(vehicleType) ||
                        x.VehicleType == vehicleType ||
                        x.VehicleType == "Tự lái hoặc có tài xế")
            .OrderBy(x => x.DistanceKm)
            .ThenBy(x => x.Amount)
            .ToList();

            return Ok(result);
        }

        private static string BuildFullAddress(Location? location)
        {
            if (location is null) return string.Empty;
            return string.Join(", ", new[] { location.AddressDetail, location.Ward, location.District, location.ProvinceCity }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private static double CalculateDistanceKm(Location from, Location to)
        {
            if (from.LocationID == to.LocationID) return 0d;
            if (!from.Latitude.HasValue || !from.Longitude.HasValue) return double.MaxValue;
            return CalculateDistanceKm((double)from.Latitude.Value, (double)from.Longitude.Value, to);
        }

        private static double CalculateDistanceKm(double fromLatitude, double fromLongitude, Location to)
        {
            if (!to.Latitude.HasValue || !to.Longitude.HasValue) return double.MaxValue;

            const double earthRadiusKm = 6371.0088;
            var toLatitude = (double)to.Latitude.Value;
            var toLongitude = (double)to.Longitude.Value;
            var lat1 = DegreesToRadians(fromLatitude);
            var lat2 = DegreesToRadians(toLatitude);
            var deltaLat = DegreesToRadians(toLatitude - fromLatitude);
            var deltaLon = DegreesToRadians(toLongitude - fromLongitude);
            var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                    Math.Cos(lat1) * Math.Cos(lat2) *
                    Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadiusKm * c;
        }

        private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;

        private static bool TryParseDate(string? value, out DateTime result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var formats = new[] { "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "dd/MM/yyyy", "d/M/yyyy", "MM/dd/yyyy", "M/d/yyyy" };
            return DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out result)
                || DateTime.TryParse(value.Trim(), new CultureInfo("vi-VN"), DateTimeStyles.AllowWhiteSpaces, out result);
        }

        private static bool TryParseTime(string? value, out TimeSpan result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var formats = new[] { @"hh\:mm", @"h\:mm", @"hh\:mm\:ss", @"h\:mm\:ss" };
            return TimeSpan.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, TimeSpanStyles.None, out result);
        }
    }
}
