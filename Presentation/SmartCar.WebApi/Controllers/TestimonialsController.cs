using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartCar.Application.Features.Mediator.Commands.TestimonialCommands;
using SmartCar.Application.Features.Mediator.Queries.TestimonialQueries;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestimonialsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public TestimonialsController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> TestimonialList()
        {
            var values = await _mediator.Send(new GetTestimonialQuery());
            return Ok(values);
        }
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTestimonial(int id)
        {
            var value = await _mediator.Send(new GetTestimonialByIdQuery(id));
            return Ok(value);
        }
        [HttpPost]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTestimonial(CreateTestimonialCommand command)
        {
            await _mediator.Send(command);
            return Ok("Đã thêm đánh giá khách hàng thành công");
        }
        [HttpDelete]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveTestimonial(int id)
        {
            await _mediator.Send(new RemoveTestimonialCommand(id));
            return Ok("Đã xóa đánh giá khách hàng thành công");
        }
        [HttpPut]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateTestimonial(UpdateTestimonialCommand command)
        {
            await _mediator.Send(command);
            return Ok("Đã cập nhật đánh giá khách hàng thành công");
        }
    }
}
