using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartCar.Application.Features.Mediator.Commands.BlogCommands;
using SmartCar.Application.Features.Mediator.Queries.BlogQueries;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public BlogsController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> BlogList()
        {
            var values = await _mediator.Send(new GetBlogQuery());
            return Ok(values);
        }
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBlog(int id)
        {
            var value = await _mediator.Send(new GetBlogByIdQuery(id));
            return Ok(value);
        }
        [HttpPost]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateBlog(CreateBlogCommand command)
        {
            await _mediator.Send(command);
            return Ok("Đã thêm bài viết thành công");
        }
        [HttpDelete]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveBlog(int id)
        {
            await _mediator.Send(new RemoveBlogCommand(id));
            return Ok("Đã xóa bài viết thành công");
        }
        [HttpPut]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBlog(UpdateBlogCommand command)
        {
            await _mediator.Send(command);
            return Ok("Đã cập nhật bài viết thành công");
        }
        [AllowAnonymous]

        [HttpGet("GetLast3BlogsWitAuthorsList")]
        public async Task<IActionResult> GetLast3BlogsWitAuthorsList()
        {
            var values = await _mediator.Send(new GetLast3BlogsWithAuthorsQuery());
            return Ok(values);
        }
        [AllowAnonymous]

        [HttpGet("GetAllBlogsWithAuthorList")]
        public async Task<IActionResult> GetAllBlogsWithAuthorList()
        {
            var values = await _mediator.Send(new GetAllBlogsWithAuthorQuery());
            return Ok(values);
        }
        [AllowAnonymous]

        [HttpGet("GetBlogByAuthorId")]
        public async Task<IActionResult> GetBlogByAuthorId(int id)
        {
            var values = await _mediator.Send(new GetBlogByAuthorIdQuery(id));
            return Ok(values);
        }
    }
}
