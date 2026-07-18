using MediatR;
using SmartCar.Application.Features.Mediator.Commands.CarFeatureCommands;
using SmartCar.Application.Interfaces.CarFeatureInterfaces;

namespace SmartCar.Application.Features.Mediator.Handlers.CarFeatureHandlers
{
    public class UpdateCarFeatureAvailableChangeToTrueCommandHandler : IRequestHandler<UpdateCarFeatureAvailableChangeToTrueCommand>
    {
        private readonly ICarFeatureRepository _repository;

        public UpdateCarFeatureAvailableChangeToTrueCommandHandler(ICarFeatureRepository repository)
        {
            _repository = repository;
        }

        public async Task Handle(UpdateCarFeatureAvailableChangeToTrueCommand request, CancellationToken cancellationToken)
        {
            await _repository.ChangeCarFeatureAvailableToTrue(request.Id);
        }
    }
}
