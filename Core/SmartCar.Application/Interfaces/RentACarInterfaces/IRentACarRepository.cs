using SmartCar.Domain.Entities;

namespace SmartCar.Application.Interfaces.RentACarInterfaces
{
    public interface IRentACarRepository
    {
        Task<List<RentACar>> GetAvailableCarsAsync(
            int locationId,
            bool available,
            DateTime? pickUpDateTime = null,
            DateTime? dropOffDateTime = null);
    }
}
