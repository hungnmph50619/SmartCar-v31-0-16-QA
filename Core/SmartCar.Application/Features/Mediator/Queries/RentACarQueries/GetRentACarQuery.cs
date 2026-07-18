using MediatR;
using SmartCar.Application.Features.Mediator.Results.RentACarResults;

namespace SmartCar.Application.Features.Mediator.Queries.RentACarQueries
{
    public class GetRentACarQuery : IRequest<List<GetRentACarQueryResult>>
    {
        public int LocationID { get; set; }
        public bool Available { get; set; }
        public DateTime? PickUpDate { get; set; }
        public DateTime? DropOffDate { get; set; }
        public TimeSpan? PickUpTime { get; set; }
        public TimeSpan? DropOffTime { get; set; }
    }
}
