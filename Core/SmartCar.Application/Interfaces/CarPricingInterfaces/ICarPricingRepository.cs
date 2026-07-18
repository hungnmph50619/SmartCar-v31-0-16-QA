using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCar.Application.ViewModels;
using SmartCar.Domain.Entities;

namespace SmartCar.Application.Interfaces.CarPricingInterfaces
{
    public interface ICarPricingRepository
    {
        List<CarPricing> GetCarPricingWithCars();
        List<CarPricing> GetCarPricingWithTimePeriod();
        List<CarPricingViewModel> GetCarPricingWithTimePeriod1();

	}
}
