using MediatR;
using SmartCar.Application.Features.Mediator.Queries.LocationQueries;
using SmartCar.Application.Features.Mediator.Results.LocationResults;
using SmartCar.Application.Interfaces;
using SmartCar.Domain.Entities;

namespace SmartCar.Application.Features.Mediator.Handlers.LocationHandlers
{
    public class GetLocationQueryHandler : IRequestHandler<GetLocationQuery, List<GetLocationQueryResult>>
    {
        private readonly IRepository<Location> _repository;
        public GetLocationQueryHandler(IRepository<Location> repository) => _repository = repository;

        public async Task<List<GetLocationQueryResult>> Handle(GetLocationQuery request, CancellationToken cancellationToken)
        {
            var values = await _repository.GetAllAsync();
            return values.OrderByDescending(x => x.IsActive).ThenBy(x => x.ProvinceCity).ThenBy(x => x.Name)
                .Select(x => new GetLocationQueryResult
                {
                    LocationID = x.LocationID,
                    Name = x.Name,
                    ProvinceCity = x.ProvinceCity,
                    District = x.District,
                    Ward = x.Ward,
                    AddressDetail = x.AddressDetail,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    SearchRadiusKm = x.SearchRadiusKm,
                    IsActive = x.IsActive
                }).ToList();
        }
    }
}
