using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCar.Domain.Entities;

namespace SmartCar.Application.Interfaces.ReviewInterfaces
{
    public interface IReviewRepository
    {
        public List<Review> GetReviewsByCarId(int carId);
    }
}
