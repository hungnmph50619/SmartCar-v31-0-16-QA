using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCar.Application.Features.Mediator.Results.AuthorResults;

namespace SmartCar.Application.Features.Mediator.Queries.AuthorQueries
{
    public class GetAuthorQuery : IRequest<List<GetAuthorQueryResult>>
    {
    }
}
