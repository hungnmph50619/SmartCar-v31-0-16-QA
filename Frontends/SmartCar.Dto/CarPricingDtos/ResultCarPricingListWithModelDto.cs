using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartCar.Dto.CarPricingDtos
{
	public class ResultCarPricingListWithModelDto
	{
        public int carId { get; set; }
		public string model { get; set; }
		public decimal dailyAmount { get; set; }
		public decimal weeklyAmount { get; set; }
		public decimal monthlyAmount { get; set; }
        public string CoverImageUrl { get; set; }
        public string Brand { get; set; }
    }

}