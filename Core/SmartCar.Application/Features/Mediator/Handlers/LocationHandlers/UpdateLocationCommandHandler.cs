using MediatR;
using SmartCar.Application.Features.Mediator.Commands.LocationCommands;
using SmartCar.Application.Interfaces;
using SmartCar.Domain.Entities;

namespace SmartCar.Application.Features.Mediator.Handlers.LocationHandlers
{
    public class UpdateLocationCommandHandler : IRequestHandler<UpdateLocationCommand>
    {
        private readonly IRepository<Location> _repository;
        public UpdateLocationCommandHandler(IRepository<Location> repository) => _repository = repository;

        public async Task Handle(UpdateLocationCommand request, CancellationToken cancellationToken)
        {
            var values = await _repository.GetByIdAsync(request.LocationID);
            values.Name = request.Name.Trim();
            values.ProvinceCity = request.ProvinceCity.Trim();
            values.District = request.District?.Trim() ?? string.Empty;
            values.Ward = request.Ward?.Trim() ?? string.Empty;
            values.AddressDetail = request.AddressDetail?.Trim() ?? string.Empty;
            values.Latitude = request.Latitude;
            values.Longitude = request.Longitude;
            values.SearchRadiusKm = request.SearchRadiusKm is >= 1 and <= 100 ? request.SearchRadiusKm : 20;
            values.IsActive = request.IsActive;
            await _repository.UpdateAsync(values);
        }
    }
}
