using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartCar.Application.Features.Mediator.Commands.ServiceCommands;
using SmartCar.Application.Features.Mediator.Queries.ServiceQueries;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServicesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ServicesController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ServiceList()
        {
            var values = await _mediator.Send(new GetServiceQuery());
            return Ok(values);
        }
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetService(int id)
        {
            var value = await _mediator.Send(new GetServiceByIdQuery(id));
            return Ok(value);
        }
        [HttpPost]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateService(CreateServiceCommand command)
        {
            await _mediator.Send(command);
            return Ok("Đã thêm dịch vụ thành công");
        }
        [HttpDelete]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveService(int id)
        {
            await _mediator.Send(new RemoveServiceCommand(id));
            return Ok("Đã xóa dịch vụ thành công");
        }
        [HttpPut]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateService(UpdateServiceCommand command)
        {
            await _mediator.Send(command);
            return Ok("Đã cập nhật dịch vụ thành công");
        }
    }
}
