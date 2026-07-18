using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartCar.Application.Features.Mediator.Queries.AppUserQueries;
using SmartCar.Application.Tools;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("auth")]
    public class LoginController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LoginController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Index(GetCheckAppUserQuery query)
        {
            var values = await _mediator.Send(query);
            if (values.IsExist)
            {
                return Created("", JwtTokenGenerator.GenerateToken(values));
            }
            else
            {
                var message = string.IsNullOrWhiteSpace(values.FailureReason)
                    ? "Tên đăng nhập hoặc mật khẩu không đúng"
                    : values.FailureReason;
                return values.LockoutEnd.HasValue
                    ? StatusCode(StatusCodes.Status423Locked, message)
                    : BadRequest(message);
            }
        }
    }
}
