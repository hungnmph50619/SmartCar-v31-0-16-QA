using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCar.Application.Features.CQRS.Commands.AboutCommands;
using SmartCar.Application.Features.CQRS.Commands.BannerCommands;
using SmartCar.Application.Interfaces;
using SmartCar.Domain.Entities;

namespace SmartCar.Application.Features.CQRS.Handlers.BannerHandlers
{
    public class RemoveBannerCommandHandler
    {
        private readonly IRepository<Banner> _repository;
        public RemoveBannerCommandHandler(IRepository<Banner> repository)
        {
            _repository = repository;
        }
        public async Task Handle(RemoveBannerCommand command)
        {
            var value = await _repository.GetByIdAsync(command.Id);
            await _repository.RemoveAsync(value);
        }
    }
}
