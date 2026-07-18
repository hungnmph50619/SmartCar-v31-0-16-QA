using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCar.Application.Interfaces.CarDescriptionInterfaces;
using SmartCar.Domain.Entities;
using SmartCar.Persistence.Context;

namespace SmartCar.Persistence.Repositories.CarDescriptionRepositories
{
    public class CarDescriptionRepository : ICarDescriptionRepository
    {
        private readonly CarBookContext _context;

        public CarDescriptionRepository(CarBookContext context)
        {
            _context = context;
        }

		async Task<CarDescription> ICarDescriptionRepository.GetCarDescription(int carId)
		{
			var values =await _context.CarDescriptions.Where(x => x.CarID == carId).FirstOrDefaultAsync();
			return values;
		}
	}
}
