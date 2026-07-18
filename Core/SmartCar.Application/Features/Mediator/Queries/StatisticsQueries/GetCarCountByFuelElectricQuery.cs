using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCar.Application.Features.Mediator.Results.StatisticsResults;

namespace SmartCar.Application.Features.Mediator.Queries.StatisticsQueries
{
    public class GetCarCountByFuelElectricQuery:IRequest<GetCarCountByFuelElectricQueryResult>
    {
    }
}
