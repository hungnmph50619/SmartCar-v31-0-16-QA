using MediatR;

namespace SmartCar.Application.Features.Mediator.Commands.LocationCommands
{
    public class CreateLocationCommand : IRequest
    {
        public string Name { get; set; } = string.Empty;
        public string ProvinceCity { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string AddressDetail { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int SearchRadiusKm { get; set; } = 20;
        public bool IsActive { get; set; } = true;
    }
}
