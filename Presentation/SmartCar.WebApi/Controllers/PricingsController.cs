using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartCar.Application.Features.Mediator.Commands.PricingCommands;
using SmartCar.Application.Features.Mediator.Queries.PricingQueries;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PricingsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public PricingsController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> PricingList()
        {
            var values = await _mediator.Send(new GetPricingQuery());
            return Ok(values);
        }
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPricing(int id)
        {
            var value = await _mediator.Send(new GetPricingByIdQuery(id));
            return Ok(value);
        }
        [HttpPost]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreatePricing(CreatePricingCommand command)
        {
            await _mediator.Send(command);
            return Ok("Đã thêm loại thanh toán thành công");
        }
        [HttpDelete]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemovePricing(int id)
        {
            await _mediator.Send(new RemovePricingCommand(id));
            return Ok("Đã xóa loại thanh toán thành công");
        }
        [HttpPut]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePricing(UpdatePricingCommand command)
        {
            await _mediator.Send(command);
            return Ok("Đã cập nhật loại thanh toán thành công");
        }
    }
}
