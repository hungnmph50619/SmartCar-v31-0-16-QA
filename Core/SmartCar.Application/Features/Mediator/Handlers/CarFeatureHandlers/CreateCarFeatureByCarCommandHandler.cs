using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCar.Application.Features.Mediator.Commands.CarFeatureCommands;
using SmartCar.Application.Interfaces.CarFeatureInterfaces;
using SmartCar.Domain.Entities;

namespace SmartCar.Application.Features.Mediator.Handlers.CarFeatureHandlers
{
    public class CreateCarFeatureByCarCommandHandler : IRequestHandler<CreateCarFeatureByCarCommand>
    {
        private readonly ICarFeatureRepository _repository;

        public CreateCarFeatureByCarCommandHandler(ICarFeatureRepository repository)
        {
            _repository = repository;
        }

        public async Task Handle(CreateCarFeatureByCarCommand request, CancellationToken cancellationToken)
        {
            await _repository.CreateCarFeatureByCar(new CarFeature
            {
                Available = false,
                CarID = request.CarID,
                FeatureID = request.FeatureID
            });
        }
    }
}
