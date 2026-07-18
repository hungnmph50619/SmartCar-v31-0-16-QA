using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartCar.Application.Features.CQRS.Commands.BrandCommands;
using SmartCar.Application.Features.CQRS.Handlers.BrandHandlers;
using SmartCar.Application.Features.CQRS.Queries.BrandQueries;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BrandsController : ControllerBase
    {
        private readonly CreateBrandCommandHandler _createBrandCommandHandler;
        private readonly GetBrandByIdQueryHandler _getBrandByIdQueryHandler;
        private readonly GetBrandQueryHandler _getBrandQueryHandler;
        private readonly UpdateBrandCommandHandler _updateBrandCommandHandler;
        private readonly RemoveBrandCommandHandler _removeBrandCommandHandler;

        public BrandsController(CreateBrandCommandHandler createBrandCommandHandler, GetBrandByIdQueryHandler getBrandByIdQueryHandler, GetBrandQueryHandler getBrandQueryHandler, UpdateBrandCommandHandler updateBrandCommandHandler, RemoveBrandCommandHandler removeBrandCommandHandler)
        {
            _createBrandCommandHandler = createBrandCommandHandler;
            _getBrandByIdQueryHandler = getBrandByIdQueryHandler;
            _getBrandQueryHandler = getBrandQueryHandler;
            _updateBrandCommandHandler = updateBrandCommandHandler;
            _removeBrandCommandHandler = removeBrandCommandHandler;
        }
        [AllowAnonymous]

        [HttpGet]
        public async Task<IActionResult> BrandList()
        {
            var values = await _getBrandQueryHandler.Handle();
            return Ok(values);
        }
        [AllowAnonymous]

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBrand(int id)
        {
            var value = await _getBrandByIdQueryHandler.Handle(new GetBrandByIdQuery(id));
            return Ok(value);
        }

        [HttpPost]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateBrand(CreateBrandCommand command)
        {
            await _createBrandCommandHandler.Handle(command);
            return Ok("Đã thêm thông tin hãng xe");
        }

        [HttpDelete]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveBrand(int id)
        {
            await _removeBrandCommandHandler.Handle(new RemoveBrandCommand(id));
            return Ok("Đã xóa thông tin hãng xe");
        }

        [HttpPut]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBrand(UpdateBrandCommand command)
        {
            await _updateBrandCommandHandler.Handle(command);
            return Ok("Đã cập nhật thông tin hãng xe");
        }
    }
}
