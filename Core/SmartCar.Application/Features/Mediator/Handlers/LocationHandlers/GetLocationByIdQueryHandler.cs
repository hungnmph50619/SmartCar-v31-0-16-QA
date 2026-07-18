using MediatR;
using SmartCar.Application.Features.Mediator.Queries.LocationQueries;
using SmartCar.Application.Features.Mediator.Results.LocationResults;
using SmartCar.Application.Interfaces;
using SmartCar.Domain.Entities;

namespace SmartCar.Application.Features.Mediator.Handlers.LocationHandlers
{
    public class GetLocationByIdQueryHandler : IRequestHandler<GetLocationByIdQuery, GetLocationByIdQueryResult>
    {
        private readonly IRepository<Location> _repository;
        public GetLocationByIdQueryHandler(IRepository<Location> repository) => _repository = repository;

        public async Task<GetLocationByIdQueryResult> Handle(GetLocationByIdQuery request, CancellationToken cancellationToken)
        {
            var values = await _repository.GetByIdAsync(request.Id);
            return new GetLocationByIdQueryResult
            {
                LocationID = values.LocationID,
                Name = values.Name,
                ProvinceCity = values.ProvinceCity,
                District = values.District,
                Ward = values.Ward,
                AddressDetail = values.AddressDetail,
                Latitude = values.Latitude,
                Longitude = values.Longitude,
                SearchRadiusKm = values.SearchRadiusKm,
                IsActive = values.IsActive
            };
        }
    }
}
