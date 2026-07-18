using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCar.Application.Features.Mediator.Commands.SocialMediaCommands;
using SmartCar.Application.Interfaces;
using SmartCar.Domain.Entities;

namespace SmartCar.Application.Features.Mediator.Handlers.SocialMediaHandlers
{
    public class RemovePricingCommandHandler : IRequestHandler<RemoveSocialMediaCommand>
    {
        private readonly IRepository<SocialMedia> _repository;

        public RemovePricingCommandHandler(IRepository<SocialMedia> repository)
        {
            _repository = repository;
        }
        public async Task Handle(RemoveSocialMediaCommand request, CancellationToken cancellationToken)
        {
            var value = await _repository.GetByIdAsync(request.Id);
            await _repository.RemoveAsync(value);
        }
    }
}
