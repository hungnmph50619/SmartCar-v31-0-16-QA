using MediatR;
using SmartCar.Application.Features.Mediator.Commands.CarFeatureCommands;
using SmartCar.Application.Interfaces.CarFeatureInterfaces;

namespace SmartCar.Application.Features.Mediator.Handlers.CarFeatureHandlers
{
    public class UpdateCarFeatureAvailableChangeToFalseCommandHandler : IRequestHandler<UpdateCarFeatureAvailableChangeToFalseCommand>
    {
        private readonly ICarFeatureRepository _repository;

        public UpdateCarFeatureAvailableChangeToFalseCommandHandler(ICarFeatureRepository repository)
        {
            _repository = repository;
        }

        public async Task Handle(UpdateCarFeatureAvailableChangeToFalseCommand request, CancellationToken cancellationToken)
        {
            await _repository.ChangeCarFeatureAvailableToFalse(request.Id);
        }
    }
}
