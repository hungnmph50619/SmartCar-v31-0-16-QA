using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCar.Application.Features.Mediator.Queries.CarFeatureQueries;
using SmartCar.Application.Features.Mediator.Queries.LocationQueries;
using SmartCar.Application.Features.Mediator.Results.BlogResults;
using SmartCar.Application.Features.Mediator.Results.CarFeatureResults;
using SmartCar.Application.Features.Mediator.Results.LocationResults;
using SmartCar.Application.Interfaces;
using SmartCar.Application.Interfaces.BlogInterfaces;
using SmartCar.Application.Interfaces.CarFeatureInterfaces;
using SmartCar.Domain.Entities;

namespace SmartCar.Application.Features.Mediator.Handlers.CarFeatureHandlers
{
    public class GetCarFeatureByCarIdQueryHandler : IRequestHandler<GetCarFeatureByCarIdQuery, List<GetCarFeatureByCarIdQueryResult>>
    {
        private readonly ICarFeatureRepository _repository;
        public GetCarFeatureByCarIdQueryHandler(ICarFeatureRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<GetCarFeatureByCarIdQueryResult>> Handle(GetCarFeatureByCarIdQuery request, CancellationToken cancellationToken)
        {
            var values = await _repository.GetCarFeaturesByCarID(request.Id);
            return values.Select(x => new GetCarFeatureByCarIdQueryResult
            {
                Available = x.Available,
                CarFeatureID = x.CarFeatureID,
                FeatureID = x.FeatureID,
                FeatureName=x.Feature.Name
            }).ToList();
        }
    }
}
