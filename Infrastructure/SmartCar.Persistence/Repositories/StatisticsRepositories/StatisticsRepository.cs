using Microsoft.EntityFrameworkCore;
using SmartCar.Application.Interfaces.StatisticsInterfaces;
using SmartCar.Persistence.Context;

namespace SmartCar.Persistence.Repositories.StatisticsRepositories
{
    public class StatisticsRepository : IStatisticsRepository
    {
        private readonly CarBookContext _context;
        public StatisticsRepository(CarBookContext context) => _context = context;

        public string GetBlogTitleByMaxBlogComment()
        {
            var blogId = _context.Comments
                .GroupBy(x => x.BlogID)
                .OrderByDescending(g => g.Count())
                .Select(g => (int?)g.Key)
                .FirstOrDefault();
            return blogId.HasValue
                ? _context.Blogs.Where(x => x.BlogID == blogId.Value).Select(x => x.Title).FirstOrDefault() ?? "Chưa có dữ liệu"
                : "Chưa có dữ liệu";
        }

        public string GetBrandNameByMaxCar()
        {
            var brandId = _context.Cars
                .GroupBy(x => x.BrandID)
                .OrderByDescending(g => g.Count())
                .Select(g => (int?)g.Key)
                .FirstOrDefault();
            return brandId.HasValue
                ? _context.Brands.Where(x => x.BrandID == brandId.Value).Select(x => x.Name).FirstOrDefault() ?? "Chưa có dữ liệu"
                : "Chưa có dữ liệu";
        }

        public int GetAuthorCount() => _context.Authors.Count();
        public int GetBlogCount() => _context.Blogs.Count();
        public int GetBrandCount() => _context.Brands.Count();
        public int GetCarCount() => _context.Cars.Count();
        public int GetLocationCount() => _context.Locations.Count();

        private decimal GetAveragePrice(string pricingName)
        {
            var pricingId = _context.Pricings.Where(x => x.Name == pricingName).Select(x => (int?)x.PricingID).FirstOrDefault();
            if (!pricingId.HasValue) return 0m;
            return _context.CarPricings.Where(x => x.PricingID == pricingId.Value)
                .Select(x => (decimal?)x.Amount).Average() ?? 0m;
        }

        public decimal GetAvgRentPriceForDaily() => GetAveragePrice("Theo ngày");
        public decimal GetAvgRentPriceForWeekly() => GetAveragePrice("Theo tuần");
        public decimal GetAvgRentPriceForMonthly() => GetAveragePrice("Theo tháng");

        private string GetCarByDailyPrice(bool highest)
        {
            var pricingId = _context.Pricings.Where(x => x.Name == "Theo ngày").Select(x => (int?)x.PricingID).FirstOrDefault();
            if (!pricingId.HasValue) return "Chưa có dữ liệu";
            var query = _context.CarPricings.AsNoTracking()
                .Where(x => x.PricingID == pricingId.Value)
                .Include(x => x.Car).ThenInclude(x => x.Brand);
            var item = highest ? query.OrderByDescending(x => x.Amount).FirstOrDefault() : query.OrderBy(x => x.Amount).FirstOrDefault();
            return item is null ? "Chưa có dữ liệu" : $"{item.Car.Brand?.Name} {item.Car.Model}".Trim();
        }

        public string GetCarBrandAndModelByRentPriceDailyMax() => GetCarByDailyPrice(true);
        public string GetCarBrandAndModelByRentPriceDailyMin() => GetCarByDailyPrice(false);

        public int GetCarCountByFuelElectric() => _context.Cars.Count(x => x.Fuel == "Điện" || x.Fuel == "Elektrik" || x.Fuel == "Electric");
        public int GetCarCountByFuelGasolineOrDiesel() => _context.Cars.Count(x => x.Fuel == "Xăng" || x.Fuel == "Dầu" || x.Fuel == "Diesel" || x.Fuel == "Benzin" || x.Fuel == "Dizel");
        public int GetCarCountByKmSmallerThen1000() => _context.Cars.Count(x => x.Km <= 1000);
        public int GetCarCountByTranmissionIsAuto() => _context.Cars.Count(x => x.Transmission == "Tự động" || x.Transmission == "Otomatik" || x.Transmission == "Automatic");
    }
}
