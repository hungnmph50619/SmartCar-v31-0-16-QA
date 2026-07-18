using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCar.Application.Features.Mediator.Queries.AuthorQueries;
using SmartCar.Application.Features.Mediator.Results.FeatureResults;
using SmartCar.Application.Features.Mediator.Results.AuthorResults;
using SmartCar.Application.Interfaces;
using SmartCar.Domain.Entities;

namespace SmartCar.Application.Features.Mediator.Handlers.AuthorHandlers
{
    public class GetPricingByIdQueryHandler : IRequestHandler<GetAuthorByIdQuery, GetAuthorByIdQueryResult>
    {
        private readonly IRepository<Author> _repository;
        public GetPricingByIdQueryHandler(IRepository<Author> repository)
        {
            _repository = repository;
        }

        public async Task<GetAuthorByIdQueryResult> Handle(GetAuthorByIdQuery request, CancellationToken cancellationToken)
        {
            var values = await _repository.GetByIdAsync(request.Id);
            return new GetAuthorByIdQueryResult
            {
                AuthorID = values.AuthorID,
                Name = values.Name,
                Description = values.Description,
                ImageUrl = values.ImageUrl
            };
        }
    }
}
