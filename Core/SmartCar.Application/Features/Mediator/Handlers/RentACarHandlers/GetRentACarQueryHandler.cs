using MediatR;
using SmartCar.Application.Features.Mediator.Queries.RentACarQueries;
using SmartCar.Application.Features.Mediator.Results.RentACarResults;
using SmartCar.Application.Interfaces.RentACarInterfaces;

namespace SmartCar.Application.Features.Mediator.Handlers.RentACarHandlers
{
    public class GetRentACarQueryHandler : IRequestHandler<GetRentACarQuery, List<GetRentACarQueryResult>>
    {
        private readonly IRentACarRepository _repository;

        public GetRentACarQueryHandler(IRentACarRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<GetRentACarQueryResult>> Handle(
            GetRentACarQuery request,
            CancellationToken cancellationToken)
        {
            DateTime? pickUpDateTime = null;
            DateTime? dropOffDateTime = null;

            if (request.PickUpDate.HasValue && request.PickUpTime.HasValue)
            {
                pickUpDateTime = request.PickUpDate.Value.Date.Add(request.PickUpTime.Value);
            }

            if (request.DropOffDate.HasValue && request.DropOffTime.HasValue)
            {
                dropOffDateTime = request.DropOffDate.Value.Date.Add(request.DropOffTime.Value);
            }

            var values = await _repository.GetAvailableCarsAsync(
                request.LocationID,
                request.Available,
                pickUpDateTime,
                dropOffDateTime);

            return values.Select(y => new GetRentACarQueryResult
            {
                CarId = y.CarID,
                Brand = y.Car.Brand.Name,
                Model = y.Car.Model,
                CoverImageUrl = y.Car.CoverImageUrl,
                Amount = y.Car.CarPricings?
                    .Where(x => x.Pricing != null && x.Pricing.Name == "Theo ngày")
                    .Select(x => x.Amount)
                    .FirstOrDefault() ?? 0
            }).ToList();
        }
    }
}
