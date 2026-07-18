using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartCar.Application.Features.Mediator.Commands.FooterAddressCommands;
using SmartCar.Application.Features.Mediator.Queries.FooterAddressQueries;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FooterAddressesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public FooterAddressesController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [AllowAnonymous]

        [HttpGet]
        public async Task<IActionResult> FooterAddressList()
        {
            var values =await _mediator.Send(new GetFooterAddressQuery());
            return Ok(values);
        }

        [HttpPost]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateFooterAddress(CreateFooterAddressCommand command)
        {
            await _mediator.Send(command);
            return Ok("Đã thêm thông tin địa chỉ chân trang");
        }
        [AllowAnonymous]

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFooterAddress(int id)
        {
            var values = await _mediator.Send(new GetFooterAddressByIdQuery(id));
            return Ok(values);  
        }

        [HttpDelete]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveFooterAddress(int id)
        {
            await _mediator.Send(new RemoveFooterAddressCommand(id));
            return Ok("Đã xóa thông tin địa chỉ chân trang");
        }

        [HttpPut]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateFooterAddress(UpdateFooterAddressCommand command)
        {
            await _mediator.Send(command);
            return Ok("Đã cập nhật thông tin địa chỉ chân trang");
        }
    }
}
