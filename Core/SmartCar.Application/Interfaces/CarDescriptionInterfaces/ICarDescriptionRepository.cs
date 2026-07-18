using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCar.Domain.Entities;

namespace SmartCar.Application.Interfaces.CarDescriptionInterfaces
{
	public interface ICarDescriptionRepository
	{
		Task<CarDescription> GetCarDescription(int carId);
	}
}
