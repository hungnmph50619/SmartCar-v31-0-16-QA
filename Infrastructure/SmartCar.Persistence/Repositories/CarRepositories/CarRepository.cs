using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCar.Application.Interfaces.CarInterfaces;
using SmartCar.Domain.Entities;
using SmartCar.Persistence.Context;

namespace SmartCar.Persistence.Repositories.CarRepositories
{
    public class CarRepository : ICarRepository
    {
        private readonly CarBookContext _context;
        public CarRepository(CarBookContext context)
        {
            _context = context;
        }

        public int GetCarCount()
        {
            var value = _context.PartnerVehicles.Count(x => x.VehiclePartnerApplication.Status == "Đã duyệt" && x.IsActive);
            return value;
        }

        public List<Car> GetCarsListWithBrands()
        {
            var values = _context.PartnerVehicles
                .Where(x => x.VehiclePartnerApplication.Status == "Đã duyệt" && x.IsActive)
                .Include(x => x.Car).ThenInclude(x => x.Brand)
                .Select(x => x.Car)
                .ToList();
            return values;
        }
        public List<Car> GetLast5CarsWithBrands()
        {
            var values = _context.PartnerVehicles
                .Where(x => x.VehiclePartnerApplication.Status == "Đã duyệt" && x.IsActive)
                .Include(x => x.Car).ThenInclude(x => x.Brand)
                .OrderByDescending(x => x.PartnerVehicleID)
                .Select(x => x.Car)
                .Take(5)
                .ToList();
            return values;
        }
    }
}
