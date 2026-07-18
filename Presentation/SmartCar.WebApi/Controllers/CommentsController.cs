using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartCar.Application.Features.Mediator.Commands.CommentCommands;
using SmartCar.Application.Features.Mediator.Commands.ReservationCommands;
using SmartCar.Application.Features.RepositoryPattern;
using SmartCar.Domain.Entities;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly IGenericRepository<Comment> _commentsRepository;
        private readonly IMediator _mediator;
        public CommentsController(IGenericRepository<Comment> commentsRepository, IMediator mediator)
        {
            _commentsRepository = commentsRepository;
            _mediator = mediator;
        }
        [AllowAnonymous]

        [HttpGet]
        public IActionResult CommentList()
        {
            var values = _commentsRepository.GetAll();
            return Ok(values);
        }

        [HttpPost]
                [Authorize(Roles = "Admin")]
        public IActionResult CreateComment(Comment comment)
        {           
            _commentsRepository.Create(comment);
            return Ok("Đã thêm bình luận thành công");
        }

        [HttpDelete]
                [Authorize(Roles = "Admin")]
        public IActionResult RemoveComment(int id)
        {
            var value = _commentsRepository.GetById(id);
            _commentsRepository.Remove(value);
            return Ok("Đã xóa bình luận thành công");
        }

        [HttpPut]
                [Authorize(Roles = "Admin")]
        public IActionResult UpdateComment(Comment comment)
        {
            _commentsRepository.Update(comment);
            return Ok("Đã xóa bình luận thành công");
        }
        [AllowAnonymous]

        [HttpGet("{id}")]
        public IActionResult GetComment(int id)
        {
            var value = _commentsRepository.GetById(id);
            return Ok(value);
        }
        [AllowAnonymous]

        [HttpGet("CommentListByBlog")]
        public IActionResult CommentListByBlog(int id)
        {
            var value = _commentsRepository.GetCommentsByBlogId(id);
            return Ok(value);
        }
        [AllowAnonymous]

        [HttpGet("CommentCountByBlog")]
        public IActionResult CommentCountByBlog(int id)
        {
            var value=_commentsRepository.GetCountCommentByBlog(id);
            return Ok(value);
        }

        [HttpPost("CreateCommentWithMediator")]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCommentWithMediator(CreateCommentCommand command)
        {
            await _mediator.Send(command);
            return Ok("Đã thêm bình luận thành công");
        }
    }
}
