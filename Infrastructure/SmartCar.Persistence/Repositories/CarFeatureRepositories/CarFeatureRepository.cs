using Microsoft.EntityFrameworkCore;
using SmartCar.Application.Interfaces.CarFeatureInterfaces;
using SmartCar.Domain.Entities;
using SmartCar.Persistence.Context;

namespace SmartCar.Persistence.Repositories.CarFeatureRepositories
{
    public class CarFeatureRepository : ICarFeatureRepository
    {
        private readonly CarBookContext _context;

        public CarFeatureRepository(CarBookContext context)
        {
            _context = context;
        }

        public async Task ChangeCarFeatureAvailableToFalse(int id)
        {
            var value = await _context.CarFeatures.FirstOrDefaultAsync(x => x.CarFeatureID == id);
            if (value is null) throw new KeyNotFoundException("Không tìm thấy tiện ích của xe.");
            value.Available = false;
            await _context.SaveChangesAsync();
        }

        public async Task ChangeCarFeatureAvailableToTrue(int id)
        {
            var value = await _context.CarFeatures.FirstOrDefaultAsync(x => x.CarFeatureID == id);
            if (value is null) throw new KeyNotFoundException("Không tìm thấy tiện ích của xe.");
            value.Available = true;
            await _context.SaveChangesAsync();
        }

        public async Task CreateCarFeatureByCar(CarFeature carFeature)
        {
            _context.CarFeatures.Add(carFeature);
            await _context.SaveChangesAsync();
        }

        public Task<List<CarFeature>> GetCarFeaturesByCarID(int carID)
        {
            return _context.CarFeatures
                .AsNoTracking()
                .Include(x => x.Feature)
                .Where(x => x.CarID == carID)
                .ToListAsync();
        }
    }
}
