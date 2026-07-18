using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartCar.Application.Features.Mediator.Commands.AuthorCommands;
using SmartCar.Application.Features.Mediator.Queries.AuthorQueries;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthorsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public AuthorsController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> AuthorList()
        {
            var values = await _mediator.Send(new GetAuthorQuery());
            return Ok(values);
        }
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAuthor(int id)
        {
            var value = await _mediator.Send(new GetAuthorByIdQuery(id));
            return Ok(value);
        }
        [HttpPost]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAuthor(CreateAuthorCommand command)
        {
            await _mediator.Send(command);
            return Ok("Đã thêm tác giả thành công");
        }
        [HttpDelete]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveAuthor(int id)
        {
            await _mediator.Send(new RemoveAuthorCommand(id));
            return Ok("Đã xóa tác giả thành công");
        }
        [HttpPut]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAuthor(UpdateAuthorCommand command)
        {
            await _mediator.Send(command);
            return Ok("Đã cập nhật tác giả thành công");
        }
    }
}
