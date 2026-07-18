using MediatR;
using SmartCar.Application.Features.Mediator.Results.StatisticsResults;

namespace SmartCar.Application.Features.Mediator.Queries.StatisticsQueries
{
    public class GetCarBrandAndModelByRentPriceDailyMaxQuery : IRequest<GetCarBrandAndModelByRentPriceDailyMaxQueryResult>
    {
    }
}
