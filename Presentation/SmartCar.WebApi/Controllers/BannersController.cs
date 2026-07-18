using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartCar.Application.Features.CQRS.Commands.BannerCommands;
using SmartCar.Application.Features.CQRS.Handlers.BannerHandlers;
using SmartCar.Application.Features.CQRS.Queries.BannerQueries;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BannersController : ControllerBase
    {
        private readonly GetBannerQueryHandler _getBannerQueryHandler;
        private readonly GetBannerByIdQueryHandler _getBannerByIdQueryHandler;
        private readonly CreateBannerCommandHandler _createBannerCommandHandler;
        private readonly UpdateBannerCommandHandler _updateBannerCommandHandler;
        private readonly RemoveBannerCommandHandler _removeBannerCommandHandler;

        public BannersController(GetBannerQueryHandler getBannerQueryHandler, GetBannerByIdQueryHandler getBannerByIdQueryHandler, CreateBannerCommandHandler createBannerCommandHandler, UpdateBannerCommandHandler updateBannerCommandHandler, RemoveBannerCommandHandler removeBannerCommandHandler)
        {
            _getBannerQueryHandler = getBannerQueryHandler;
            _getBannerByIdQueryHandler = getBannerByIdQueryHandler;
            _createBannerCommandHandler = createBannerCommandHandler;
            _updateBannerCommandHandler = updateBannerCommandHandler;
            _removeBannerCommandHandler = removeBannerCommandHandler;
        }
        [AllowAnonymous]

        [HttpGet]
        public async Task<IActionResult> BannerList()
        {
            var values = await _getBannerQueryHandler.Handle();
            return Ok(values);
        }
        [AllowAnonymous]

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBanner(int id)
        {
            var value = await _getBannerByIdQueryHandler.Handle(new GetBannerByIdQuery(id));
            return Ok(value);
        }
        [HttpPost]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateBanner(CreateBannerCommand command)
        {
            await _createBannerCommandHandler.Handle(command);
            return Ok("Đã thêm thông tin");
        }

        [HttpDelete]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveBanner(int id)
        {
            await _removeBannerCommandHandler.Handle(new RemoveBannerCommand(id));
            return Ok("Đã xóa thông tin");
        }

        [HttpPut]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBanner(UpdateBannerCommand command)
        {
            await _updateBannerCommandHandler.Handle(command);
            return Ok("Đã cập nhật thông tin");
        }
    }
}
