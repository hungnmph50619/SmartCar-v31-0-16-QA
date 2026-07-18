using SmartCar.Domain.Entities;

namespace SmartCar.Application.Interfaces.CarFeatureInterfaces
{
    public interface ICarFeatureRepository
    {
        Task<List<CarFeature>> GetCarFeaturesByCarID(int carID);
        Task ChangeCarFeatureAvailableToFalse(int id);
        Task ChangeCarFeatureAvailableToTrue(int id);
        Task CreateCarFeatureByCar(CarFeature carFeature);
    }
}
