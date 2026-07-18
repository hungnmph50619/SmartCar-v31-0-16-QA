using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCar.Application.Features.CQRS.Queries.BannerQueries;
using SmartCar.Application.Features.CQRS.Queries.BrandQueries;
using SmartCar.Application.Features.CQRS.Results.BannerResults;
using SmartCar.Application.Features.CQRS.Results.BrandResults;
using SmartCar.Application.Interfaces;
using SmartCar.Domain.Entities;

namespace SmartCar.Application.Features.CQRS.Handlers.BrandHandlers
{
    public class GetBrandByIdQueryHandler
    {
        private readonly IRepository<Brand> _repository;
        public GetBrandByIdQueryHandler(IRepository<Brand> repository)
        {
            _repository = repository;
        }
        public async Task<GetBrandByIdQueryResult> Handle(GetBrandByIdQuery query)
        {
            var values = await _repository.GetByIdAsync(query.Id);
            return new GetBrandByIdQueryResult
            {
                BrandID = values.BrandID,
                Name = values.Name
            };
        }
    }
}
