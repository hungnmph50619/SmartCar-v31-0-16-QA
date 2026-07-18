using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCar.Application.Features.CQRS.Queries.AboutQueries;
using SmartCar.Application.Features.CQRS.Results.AboutResults;
using SmartCar.Application.Interfaces;
using SmartCar.Domain.Entities;

namespace SmartCar.Application.Features.CQRS.Handlers.AboutHandlers
{
    public class GetAboutByIdQueryHandler
    {
        private readonly IRepository<About> _repository;
        public GetAboutByIdQueryHandler(IRepository<About> repository)
        {
            _repository = repository;
        }
        public async Task<GetAboutByIdQueryResult> Handle(GetAboutByIdQuery query)
        {
            var values = await _repository.GetByIdAsync(query.Id);
            return new GetAboutByIdQueryResult
            {
                AboutID = values.AboutID,
                Description = values.Description,
                ImageUrl = values.ImageUrl,
                Title = values.Title
            };
        }
    }
}
