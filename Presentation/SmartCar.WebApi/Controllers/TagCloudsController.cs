using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartCar.Application.Features.Mediator.Commands.TagCloudCommands;
using SmartCar.Application.Features.Mediator.Queries.TagCloudQueries;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TagCloudsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public TagCloudsController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> TagCloudList()
        {
            var values = await _mediator.Send(new GetTagCloudQuery());
            return Ok(values);
        }
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTagCloud(int id)
        {
            var value = await _mediator.Send(new GetTagCloudByIdQuery(id));
            return Ok(value);
        }
        [HttpPost]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTagCloud(CreateTagCloudCommand command)
        {
            await _mediator.Send(command);
            return Ok("Đã thêm thẻ thành công");
        }
        [HttpDelete]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveTagCloud(int id)
        {
            await _mediator.Send(new RemoveTagCloudCommand(id));
            return Ok("Đã xóa thẻ thành công");
        }
        [HttpPut]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateTagCloud(UpdateTagCloudCommand command)
        {
            await _mediator.Send(command);
            return Ok("Đã cập nhật thẻ thành công");
        }
        [AllowAnonymous]

        [HttpGet("GetTagClodByBlogId")]
        public async Task<IActionResult> GetTagClodByBlogId(int id)
        {
            var values = await _mediator.Send(new GetTagCloudByBlogIdQuery(id));
            return Ok(values);
        }
    }
}
