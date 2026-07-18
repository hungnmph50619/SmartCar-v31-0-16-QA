using SmartCar.Dto.LocationDtos;
using SmartCar.Dto.MarketplaceDtos;

namespace SmartCar.WebUI.Models
{
    public class VehiclePartnerRegisterViewModel
    {
        public CreateVehiclePartnerApplicationDto Form { get; set; } = new();
        public IFormFile? VehicleImage { get; set; }
        public IFormFile? FrontImage { get; set; }
        public IFormFile? RearImage { get; set; }
        public IFormFile? LeftImage { get; set; }
        public IFormFile? RightImage { get; set; }
        public IFormFile? InteriorImage { get; set; }
        public IFormFile? DashboardImage { get; set; }
        public IFormFile? RegistrationImage { get; set; }
        public IFormFile? InspectionImage { get; set; }
        public IFormFile? InsuranceImage { get; set; }
        public IFormFile? DriverLicenseImage { get; set; }
        public List<ResultLocationDto> Locations { get; set; } = new();
        public decimal CurrentCommissionRate { get; set; } = 20m;
    }
}
