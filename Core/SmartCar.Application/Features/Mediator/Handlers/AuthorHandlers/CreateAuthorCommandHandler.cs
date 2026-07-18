using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCar.Application.Features.Mediator.Commands.AuthorCommands;
using SmartCar.Application.Interfaces;
using SmartCar.Domain.Entities;

namespace SmartCar.Application.Features.Mediator.Handlers.AuthorHandlers
{
    public class CreatePricingCommandHandler : IRequestHandler<CreateAuthorCommand>
    {
        private readonly IRepository<Author> _repository;
        public CreatePricingCommandHandler(IRepository<Author> repository)
        {
            _repository = repository;
        }
        public async Task Handle(CreateAuthorCommand request, CancellationToken cancellationToken)
        {
            await _repository.CreateAsync(new Author
            {
                Name = request.Name,
                Description = request.Description,
                ImageUrl = request.ImageUrl
            });
        }
    }
}
