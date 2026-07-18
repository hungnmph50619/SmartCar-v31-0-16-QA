using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCar.Application.Features.CQRS.Commands.CategoryCommands;
using SmartCar.Application.Interfaces;
using SmartCar.Domain.Entities;

namespace SmartCar.Application.Features.CQRS.Handlers.CategoryHandlers
{
    public class CreateCategoryCommandHandler
    {
        private readonly IRepository<Category> _repository;
        public CreateCategoryCommandHandler(IRepository<Category> repository)
        {
            _repository = repository;
        }
        public async Task Handle(CreateCategoryCommand command)
        {
            await _repository.CreateAsync(new Category
            {
                Name = command.Name
            });
        }
    }
}
