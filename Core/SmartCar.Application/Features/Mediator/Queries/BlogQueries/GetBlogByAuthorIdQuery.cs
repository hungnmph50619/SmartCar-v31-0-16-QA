using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCar.Application.Features.Mediator.Results.BlogResults;

namespace SmartCar.Application.Features.Mediator.Queries.BlogQueries
{
    public class GetBlogByAuthorIdQuery : IRequest<List<GetBlogByAuthorIdQueryResult>>
    {
        public int Id { get; set; }
        public GetBlogByAuthorIdQuery(int id)
        {
            Id = id;
        }
    }
}
