using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartCar.Application.Features.CQRS.Commands.AboutCommands;
using SmartCar.Application.Features.CQRS.Handlers.AboutHandlers;
using SmartCar.Application.Features.CQRS.Queries.AboutQueries;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AboutsController : ControllerBase
    {
        private readonly CreateAboutCommandHandler _createAboutCommandHandler;
        private readonly GetAboutByIdQueryHandler _getAboutByIdQueryHandler;
        private readonly GetAboutQueryHandler _getAboutQueryHandler;
        private readonly UpdateAboutCommandHandler _updateAboutCommandHandler;
        private readonly RemoveAboutCommandHandler _removeAboutCommandHandler;

        public AboutsController(CreateAboutCommandHandler createAboutCommandHandler, GetAboutByIdQueryHandler getAboutByIdQueryHandler, GetAboutQueryHandler getAboutQueryHandler, UpdateAboutCommandHandler updateAboutCommandHandler, RemoveAboutCommandHandler removeAboutCommandHandler)
        {
            _createAboutCommandHandler = createAboutCommandHandler;
            _getAboutByIdQueryHandler = getAboutByIdQueryHandler;
            _getAboutQueryHandler = getAboutQueryHandler;
            _updateAboutCommandHandler = updateAboutCommandHandler;
            _removeAboutCommandHandler = removeAboutCommandHandler;
        }
        [AllowAnonymous]

        [HttpGet]
        public async Task<IActionResult> AboutList()
        {
            var values = await _getAboutQueryHandler.Handle();
            return Ok(values);
        }
        [AllowAnonymous]

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAbout(int id)
        {
            var value = await _getAboutByIdQueryHandler.Handle(new GetAboutByIdQuery(id));
            return Ok(value);
        }

        [HttpPost]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAbout(CreateAboutCommand command)
        {
            await _createAboutCommandHandler.Handle(command);
            return Ok("Đã thêm thông tin giới thiệu");
        }

        [HttpDelete]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveAbout(int id)
        {
            await _removeAboutCommandHandler.Handle(new RemoveAboutCommand(id));
            return Ok("Đã xóa thông tin giới thiệu");
        }

        [HttpPut]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAbout(UpdateAboutCommand command)
        {
            await _updateAboutCommandHandler.Handle(command);
            return Ok("Đã cập nhật thông tin giới thiệu");
        }
    }
}
