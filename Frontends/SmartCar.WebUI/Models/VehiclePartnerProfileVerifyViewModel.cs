using Microsoft.AspNetCore.Http;
using SmartCar.Dto.MarketplaceDtos;

namespace SmartCar.WebUI.Models
{
    public class VehiclePartnerProfileVerifyViewModel
    {
        public SubmitVehiclePartnerProfileDto Form { get; set; } = new();
        public VehiclePartnerProfileDto? CurrentProfile { get; set; }
        public IFormFile? CitizenFrontImage { get; set; }
        public IFormFile? CitizenBackImage { get; set; }
        public IFormFile? PortraitImage { get; set; }
        public IFormFile? BusinessLicenseImage { get; set; }
        public IFormFile? AuthorizationDocument { get; set; }
    }
}
