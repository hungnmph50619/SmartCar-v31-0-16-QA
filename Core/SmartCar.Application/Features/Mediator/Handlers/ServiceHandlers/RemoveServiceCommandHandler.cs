using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCar.Application.Features.Mediator.Commands.ServiceCommands;
using SmartCar.Application.Interfaces;
using SmartCar.Domain.Entities;

namespace SmartCar.Application.Features.Mediator.Handlers.ServiceHandlers
{
    public class RemovePricingCommandHandler : IRequestHandler<RemoveServiceCommand>
    {
        private readonly IRepository<Service> _repository;

        public RemovePricingCommandHandler(IRepository<Service> repository)
        {
            _repository = repository;
        }
        public async Task Handle(RemoveServiceCommand request, CancellationToken cancellationToken)
        {
            var value = await _repository.GetByIdAsync(request.Id);
            await _repository.RemoveAsync(value);
        }
    }
}
