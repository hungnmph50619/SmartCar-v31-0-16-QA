using MediatR;
using SmartCar.Application.Features.Mediator.Queries.CarPricingQueries;
using SmartCar.Application.Features.Mediator.Results.CarPricingResults;
using SmartCar.Application.Interfaces.CarPricingInterfaces;

namespace SmartCar.Application.Features.Mediator.Handlers.CarPricingHandlers
{
    public sealed class GetCarPricingWithTimePeriodQueryHandler
        : IRequestHandler<GetCarPricingWithTimePeriodQuery, List<GetCarPricingWithTimePeriodQueryResult>>
    {
        private readonly ICarPricingRepository _repository;

        public GetCarPricingWithTimePeriodQueryHandler(ICarPricingRepository repository)
        {
            _repository = repository;
        }

        public Task<List<GetCarPricingWithTimePeriodQueryResult>> Handle(
            GetCarPricingWithTimePeriodQuery request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var values = _repository.GetCarPricingWithTimePeriod1();
            var result = values.Select(x => new GetCarPricingWithTimePeriodQueryResult
            {
                CarId = x.CarId,
                Brand = x.Brand ?? string.Empty,
                Model = x.Model ?? string.Empty,
                CoverImageUrl = x.CoverImageUrl ?? string.Empty,
                DailyAmount = x.Amounts.ElementAtOrDefault(0),
                WeeklyAmount = x.Amounts.ElementAtOrDefault(1),
                MonthlyAmount = x.Amounts.ElementAtOrDefault(2)
            }).ToList();

            return Task.FromResult(result);
        }
    }
}
