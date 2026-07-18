using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCar.Application.Features.CQRS.Commands.BrandCommands;
using SmartCar.Application.Features.CQRS.Commands.CarCommands;
using SmartCar.Application.Interfaces;
using SmartCar.Domain.Entities;

namespace SmartCar.Application.Features.CQRS.Handlers.CarHandlers
{
    public class RemoveCarCommandHandler
    {
        private readonly IRepository<Car> _repository;
        public RemoveCarCommandHandler(IRepository<Car> repository)
        {
            _repository = repository;
        }
        public async Task Handle(RemoveCarCommand command)
        {
            var value = await _repository.GetByIdAsync(command.Id);
            await _repository.RemoveAsync(value);
        }
    }
}
