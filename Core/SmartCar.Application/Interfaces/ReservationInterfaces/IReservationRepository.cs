using SmartCar.Domain.Entities;

namespace SmartCar.Application.Interfaces.ReservationInterfaces
{
    public interface IReservationRepository
    {
        Task<List<Reservation>> GetAllWithDetailsAsync();
        Task<Reservation?> GetByIdWithDetailsAsync(int id);
        Task<Reservation?> GetForTrackingAsync(int id, string contact);
        Task<List<Reservation>> GetByContactAsync(string contact);
        Task<bool> IsCarAvailableAsync(int carId, DateTime pickUpDateTime, DateTime dropOffDateTime, int? excludeReservationId = null, string? serviceType = null);
        Task<decimal> GetDailyPriceAsync(int carId);
        Task<bool> CanRentAtLocationAsync(int carId, int pickUpLocationId, int dropOffLocationId);
        Task<bool> UpdateStatusAsync(int id, string status);
    }
}
