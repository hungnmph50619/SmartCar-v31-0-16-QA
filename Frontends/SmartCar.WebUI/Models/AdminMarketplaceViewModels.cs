using SmartCar.Dto.MarketplaceDtos;

namespace SmartCar.WebUI.Models
{
    public class AdminVehiclePartnerViewModel
    {
        public List<VehiclePartnerProfileDto> Profiles { get; set; } = new();
        public List<ResultVehiclePartnerApplicationDto> Applications { get; set; } = new();
        public List<ResultPartnerVehicleDto> Vehicles { get; set; } = new();
        public decimal GlobalCommissionRate { get; set; } = 20m;
    }

    public class AdminMarketplaceViewModel
    {
        public PlatformFeeSettingDto Settings { get; set; } = new();
        public List<ResultCommissionTransactionDto> Transactions { get; set; } = new();
        public string? Status { get; set; }
    }
}
