using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartCar.Application.Features.Mediator.Commands.LocationCommands;
using SmartCar.Application.Features.Mediator.Queries.LocationQueries;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public LocationsController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> LocationList()
        {
            var values = await _mediator.Send(new GetLocationQuery());
            return Ok(values);
        }
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLocation(int id)
        {
            var value = await _mediator.Send(new GetLocationByIdQuery(id));
            return Ok(value);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateLocation(CreateLocationCommand command)
        {
            var validationError = ValidateLocation(command.Name, command.ProvinceCity, command.Latitude, command.Longitude, command.SearchRadiusKm);
            if (validationError is not null) return BadRequest(validationError);
            await _mediator.Send(command);
            return Ok("Đã thêm địa điểm thành công");
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete]
        public async Task<IActionResult> RemoveLocation(int id)
        {
            await _mediator.Send(new RemoveLocationCommand(id));
            return Ok("Đã xóa địa điểm thành công");
        }
        [Authorize(Roles = "Admin")]
        [HttpPut]
        public async Task<IActionResult> UpdateLocation(UpdateLocationCommand command)
        {
            if (command.LocationID <= 0) return BadRequest("Địa điểm cần cập nhật không hợp lệ.");
            var validationError = ValidateLocation(command.Name, command.ProvinceCity, command.Latitude, command.Longitude, command.SearchRadiusKm);
            if (validationError is not null) return BadRequest(validationError);
            await _mediator.Send(command);
            return Ok("Đã cập nhật địa điểm thành công");
        }

        private static string? ValidateLocation(string? name, string? provinceCity, decimal? latitude, decimal? longitude, int searchRadiusKm)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Vui lòng nhập tên điểm giao nhận.";
            if (string.IsNullOrWhiteSpace(provinceCity)) return "Vui lòng nhập tỉnh/thành phố.";
            if (latitude.HasValue != longitude.HasValue) return "Phải nhập đồng thời cả vĩ độ và kinh độ.";
            if (latitude is < -90 or > 90) return "Vĩ độ phải từ -90 đến 90.";
            if (longitude is < -180 or > 180) return "Kinh độ phải từ -180 đến 180.";
            if (searchRadiusKm is < 1 or > 100) return "Bán kính tìm kiếm phải từ 1 đến 100 km.";
            return null;
        }
    }
}
