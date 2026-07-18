using SmartCar.Dto.AccountDtos;
using SmartCar.Dto.ReservationDtos;

namespace SmartCar.WebUI.Models
{
    public class AccountProfileViewModel
    {
        public UserProfileDto Profile { get; set; } = new();
        public CustomerReadinessDto? CustomerReadiness { get; set; }
    }
}
