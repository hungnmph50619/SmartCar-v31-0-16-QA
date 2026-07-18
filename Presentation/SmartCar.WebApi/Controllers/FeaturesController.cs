using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartCar.Application.Features.Mediator.Commands.FeatureCommands;
using SmartCar.Application.Features.Mediator.Queries.FeatureQueries;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeaturesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public FeaturesController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> FeatureList()
        {
            var values = await _mediator.Send(new GetFeatureQuery());
            return Ok(values);
        }
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFeature(int id)
        {
            var value = await _mediator.Send(new GetFeatureByIdQuery(id));
            return Ok(value);
        }
        [HttpPost]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateFeature(CreateFeatureCommand command)
        {
            await _mediator.Send(command);
            return Ok("Đã thêm tính năng thành công");
        }
        [HttpDelete("{id}")]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveFeature(int id)
        {
            await _mediator.Send(new RemoveFeatureCommand(id));
            return Ok("Đã xóa tính năng thành công");
        }
        [HttpPut]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateFeature(UpdateFeatureCommand command)
        {
            await _mediator.Send(command);
            return Ok("Đã cập nhật tính năng thành công");
        }
    }
}
