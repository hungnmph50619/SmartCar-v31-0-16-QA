using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartCar.Application.Features.Mediator.Commands.CarFeatureCommands;
using SmartCar.Application.Features.Mediator.Queries.BlogQueries;
using SmartCar.Application.Features.Mediator.Queries.CarFeatureQueries;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CarFeaturesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public CarFeaturesController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> CarFeatureListByCarId(int id)
        {
            var values = await _mediator.Send(new GetCarFeatureByCarIdQuery(id));
            return Ok(values);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("CarFeatureChangeAvailableToFalse")]
        public async Task<IActionResult> CarFeatureChangeAvailableToFalse(int id)
        {
            await _mediator.Send(new UpdateCarFeatureAvailableChangeToFalseCommand(id));
            return Ok("Đã cập nhật");
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("CarFeatureChangeAvailableToTrue")]
        public async Task<IActionResult> CarFeatureChangeAvailableToTrue(int id)
        {
            await _mediator.Send(new UpdateCarFeatureAvailableChangeToTrueCommand(id));
            return Ok("Đã cập nhật");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCarFeatureByCarID(CreateCarFeatureByCarCommand command)
        {
            await _mediator.Send(command);
            return Ok("Đã thêm mới");
        }
    }
}