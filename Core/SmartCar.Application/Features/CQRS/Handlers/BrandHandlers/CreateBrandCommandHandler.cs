using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCar.Application.Features.CQRS.Commands.BannerCommands;
using SmartCar.Application.Features.CQRS.Commands.BrandCommands;
using SmartCar.Application.Interfaces;
using SmartCar.Domain.Entities;

namespace SmartCar.Application.Features.CQRS.Handlers.BrandHandlers
{
    public class CreateBrandCommandHandler
    {
        private readonly IRepository<Brand> _repository;
        public CreateBrandCommandHandler(IRepository<Brand> repository)
        {
            _repository = repository;
        }
        public async Task Handle(CreateBrandCommand command)
        {
            await _repository.CreateAsync(new Brand
            {
                Name = command.Name
            });
        }
    }
}
