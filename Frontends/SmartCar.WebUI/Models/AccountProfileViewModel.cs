using SmartCar.Dto.AccountDtos;
using SmartCar.Dto.MarketplaceDtos;
using SmartCar.Dto.ReservationDtos;

namespace SmartCar.WebUI.Models
{
    public class AccountProfileViewModel
    {
        public UserProfileDto Profile { get; set; } = new();
        public VehiclePartnerProfileDto? PartnerProfile { get; set; }
        public CustomerReadinessDto? CustomerReadiness { get; set; }
    }
}
