using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartCar.Application.Features.Mediator.Commands.SocialMediaCommands;
using SmartCar.Application.Features.Mediator.Queries.SocialMediaQueries;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SocialMediasController : ControllerBase
    {
        private readonly IMediator _mediator;
        public SocialMediasController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> SocialMediaList()
        {
            var values = await _mediator.Send(new GetSocialMediaQuery());
            return Ok(values);
        }
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSocialMedia(int id)
        {
            var value = await _mediator.Send(new GetSocialMediaByIdQuery(id));
            return Ok(value);
        }
        [HttpPost]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateSocialMedia(CreateSocialMediaCommand command)
        {
            await _mediator.Send(command);
            return Ok("Đã thêm mạng xã hội thành công");
        }
        [HttpDelete]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveSocialMedia(int id)
        {
            await _mediator.Send(new RemoveSocialMediaCommand(id));
            return Ok("Đã xóa mạng xã hội thành công");
        }
        [HttpPut]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSocialMedia(UpdateSocialMediaCommand command)
        {
            await _mediator.Send(command);
            return Ok("Đã cập nhật mạng xã hội thành công");
        }
    }
}
