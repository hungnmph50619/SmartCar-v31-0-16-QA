using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartCar.Application.Features.CQRS.Commands.CategoryCommands;
using SmartCar.Application.Features.CQRS.Handlers.CategoryHandlers;
using SmartCar.Application.Features.CQRS.Queries.CategoryQueries;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly CreateCategoryCommandHandler _createCategoryCommandHandler;
        private readonly GetCategoryByIdQueryHandler _getCategoryByIdQueryHandler;
        private readonly GetCategoryQueryHandler _getCategoryQueryHandler;
        private readonly UpdateCategoryCommandHandler _updateCategoryCommandHandler;
        private readonly RemoveCategoryCommandHandler _removeCategoryCommandHandler;

        public CategoriesController(CreateCategoryCommandHandler createCategoryCommandHandler, GetCategoryByIdQueryHandler getCategoryByIdQueryHandler, GetCategoryQueryHandler getCategoryQueryHandler, UpdateCategoryCommandHandler updateCategoryCommandHandler, RemoveCategoryCommandHandler removeCategoryCommandHandler)
        {
            _createCategoryCommandHandler = createCategoryCommandHandler;
            _getCategoryByIdQueryHandler = getCategoryByIdQueryHandler;
            _getCategoryQueryHandler = getCategoryQueryHandler;
            _updateCategoryCommandHandler = updateCategoryCommandHandler;
            _removeCategoryCommandHandler = removeCategoryCommandHandler;
        }
        [AllowAnonymous]

        [HttpGet]
        public async Task<IActionResult> CategoryList()
        {
            var values = await _getCategoryQueryHandler.Handle();
            return Ok(values);
        }
        [AllowAnonymous]

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var value = await _getCategoryByIdQueryHandler.Handle(new GetCategoryByIdQuery(id));
            return Ok(value);
        }

        [HttpPost]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCategory(CreateCategoryCommand command)
        {
            await _createCategoryCommandHandler.Handle(command);
            return Ok("Đã thêm thông tin danh mục");
        }

        [HttpDelete]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveCategory(int id)
        {
            await _removeCategoryCommandHandler.Handle(new RemoveCategoryCommand(id));
            return Ok("Đã xóa thông tin danh mục");
        }

        [HttpPut]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCategory(UpdateCategoryCommand command)
        {
            await _updateCategoryCommandHandler.Handle(command);
            return Ok("Đã cập nhật thông tin danh mục");
        }
    }
}
