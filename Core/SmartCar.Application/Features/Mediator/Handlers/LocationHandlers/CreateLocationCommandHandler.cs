using MediatR;
using SmartCar.Application.Features.Mediator.Commands.LocationCommands;
using SmartCar.Application.Interfaces;
using SmartCar.Domain.Entities;

namespace SmartCar.Application.Features.Mediator.Handlers.LocationHandlers
{
    public class CreateLocationCommandHandler : IRequestHandler<CreateLocationCommand>
    {
        private readonly IRepository<Location> _repository;
        public CreateLocationCommandHandler(IRepository<Location> repository) => _repository = repository;

        public async Task Handle(CreateLocationCommand request, CancellationToken cancellationToken)
        {
            await _repository.CreateAsync(new Location
            {
                Name = request.Name.Trim(),
                ProvinceCity = request.ProvinceCity.Trim(),
                District = request.District?.Trim() ?? string.Empty,
                Ward = request.Ward?.Trim() ?? string.Empty,
                AddressDetail = request.AddressDetail?.Trim() ?? string.Empty,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                SearchRadiusKm = request.SearchRadiusKm is >= 1 and <= 100 ? request.SearchRadiusKm : 20,
                IsActive = request.IsActive
            });
        }
    }
}
