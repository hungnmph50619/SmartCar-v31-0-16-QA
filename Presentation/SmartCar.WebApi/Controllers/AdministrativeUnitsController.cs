using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Persistence.Context;

namespace SmartCar.WebApi.Controllers
{
    [ApiController]
    [Route("api/administrative-units")]
    [AllowAnonymous]
    public sealed class AdministrativeUnitsController : ControllerBase
    {
        private readonly CarBookContext _context;

        public AdministrativeUnitsController(CarBookContext context) => _context = context;

        [HttpGet("provinces")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetProvinces(CancellationToken cancellationToken)
        {
            var provinces = await _context.AdministrativeProvinces
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.ProvinceName)
                .Select(x => new
                {
                    code = x.ProvinceCode,
                    name = x.ProvinceName,
                    type = x.ProvinceType,
                    fullName = x.ProvinceType + " " + x.ProvinceName
                })
                .ToListAsync(cancellationToken);

            return Ok(provinces);
        }

        [HttpGet("provinces/{provinceCode}/wards")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetWards(string provinceCode, CancellationToken cancellationToken)
        {
            var code = (provinceCode ?? string.Empty).Trim();
            if (code.Length != 2) return BadRequest(new { message = "Mã tỉnh/thành phố không hợp lệ." });

            var exists = await _context.AdministrativeProvinces
                .AsNoTracking()
                .AnyAsync(x => x.ProvinceCode == code && x.IsActive, cancellationToken);
            if (!exists) return NotFound(new { message = "Không tìm thấy tỉnh/thành phố." });

            var wards = await _context.AdministrativeWards
                .AsNoTracking()
                .Where(x => x.ProvinceCode == code && x.IsActive)
                .OrderBy(x => x.WardName)
                .Select(x => new
                {
                    code = x.WardCode,
                    name = x.WardName,
                    type = x.WardType,
                    fullName = x.WardType + " " + x.WardName
                })
                .ToListAsync(cancellationToken);

            return Ok(wards);
        }
    }
}
