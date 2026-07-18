using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Application.Features.CQRS.Handlers.CarHandlers;
using SmartCar.Application.Features.CQRS.Queries.CarQueries;
using SmartCar.Persistence.Context;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CarsController : ControllerBase
    {
        private readonly GetCarByIdQueryHandler _getCarByIdQueryHandler;
        private readonly GetCarWithBrandQueryHandler _getCarWithBrandQueryHandler;
        private readonly GetLast5CarsWithBrandQueryHandler _getLast5CarsWithBrandQueryHandler;
        private readonly CarBookContext _context;

        public CarsController(
            GetCarByIdQueryHandler getCarByIdQueryHandler,
            GetCarWithBrandQueryHandler getCarWithBrandQueryHandler,
            GetLast5CarsWithBrandQueryHandler getLast5CarsWithBrandQueryHandler,
            CarBookContext context)
        {
            _getCarByIdQueryHandler = getCarByIdQueryHandler;
            _getCarWithBrandQueryHandler = getCarWithBrandQueryHandler;
            _getLast5CarsWithBrandQueryHandler = getLast5CarsWithBrandQueryHandler;
            _context = context;
        }
        [AllowAnonymous]

        [HttpGet]
        public async Task<IActionResult> CarList()
        {
            var values = await _context.PartnerVehicles
                .AsNoTracking()
                .Where(x => x.VehiclePartnerApplication.Status == "Đã duyệt" && x.IsActive)
                .Include(x => x.Car)
                .Select(x => new
                {
                    x.Car.CarID,
                    x.Car.BrandID,
                    x.Car.Model,
                    x.Car.CoverImageUrl,
                    x.Car.BigImageUrl,
                    x.Car.Km,
                    x.Car.Transmission,
                    x.Car.Seat,
                    x.Car.Luggage,
                    x.Car.Fuel
                })
                .ToListAsync();
            return Ok(values);
        }
        [AllowAnonymous]

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCar(int id)
        {
            var isPublished = await _context.PartnerVehicles.AnyAsync(x =>
                x.CarID == id && x.VehiclePartnerApplication.Status == "Đã duyệt" && x.IsActive);
            if (!isPublished)
                return NotFound("Xe không tồn tại hoặc chưa được duyệt trên sàn.");

            var value = await _getCarByIdQueryHandler.Handle(new GetCarByIdQuery(id));
            return Ok(value);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult CreateCar()
            => BadRequest("Smart Car không sở hữu xe. Xe mới chỉ được tạo khi Admin duyệt hồ sơ đăng xe của đối tác.");

        [Authorize(Roles = "Admin")]
        [HttpPut]
        public IActionResult UpdateCar()
            => BadRequest("Thông tin xe phải được cập nhật qua hồ sơ xe đối tác và quy trình kiểm duyệt.");

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public IActionResult RemoveCar(int id)
            => BadRequest("Không xóa xe trực tiếp. Hãy khóa hoặc tạm ngừng xe đối tác để giữ lịch sử giao dịch.");
        [AllowAnonymous]

        [HttpGet("GetCarWithBrand")]
        public IActionResult GetCarWithBrand()
            => Ok(_getCarWithBrandQueryHandler.Handle());
        [AllowAnonymous]

        [HttpGet("GetLast5CarsWithBrandQueryHandler")]
        public IActionResult GetLast5CarsWithBrandQueryHandler()
            => Ok(_getLast5CarsWithBrandQueryHandler.Handle());
    }
}
