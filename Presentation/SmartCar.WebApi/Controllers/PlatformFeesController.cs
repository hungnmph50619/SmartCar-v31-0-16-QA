using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.Entities;
using SmartCar.Dto.MarketplaceDtos;
using SmartCar.Persistence.Context;
using System.Security.Claims;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlatformFeesController : ControllerBase
    {
        private readonly CarBookContext _context;
        public PlatformFeesController(CarBookContext context) => _context = context;

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var setting = await _context.PlatformFeeSettings.AsNoTracking()
                .OrderBy(x => x.PlatformFeeSettingID)
                .FirstOrDefaultAsync();
            return setting is null
                ? Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: "Chưa cấu hình phí nền tảng. Vui lòng chạy seed/migration đúng phiên bản.")
                : Ok(Map(setting));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut]
        public async Task<IActionResult> Update(UpdatePlatformFeeSettingDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage)));
            }

            var setting = await GetOrCreateAsync();
            setting.VehiclePartnerCommissionPercent = decimal.Round(dto.VehiclePartnerCommissionPercent, 2);
            setting.Note = string.IsNullOrWhiteSpace(dto.Note) ? null : (dto.Note ?? string.Empty).Trim();
            setting.UpdatedDate = DateTime.UtcNow;
            setting.UpdatedByAppUserID = GetCurrentUserId();
            await _context.SaveChangesAsync();
            return Ok(Map(setting));
        }

        private async Task<PlatformFeeSetting> GetOrCreateAsync()
        {
            var setting = await _context.PlatformFeeSettings.OrderBy(x => x.PlatformFeeSettingID).FirstOrDefaultAsync();
            if (setting is not null) return setting;

            setting = new PlatformFeeSetting
            {
                VehiclePartnerCommissionPercent = 20m,
                Note = "Hoa hồng mặc định của sàn môi giới xe tự lái.",
                UpdatedDate = DateTime.UtcNow
            };
            _context.PlatformFeeSettings.Add(setting);
            await _context.SaveChangesAsync();
            return setting;
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : null;
        }

        private static PlatformFeeSettingDto Map(PlatformFeeSetting x) => new()
        {
            PlatformFeeSettingID = x.PlatformFeeSettingID,
            VehiclePartnerCommissionPercent = x.VehiclePartnerCommissionPercent,
            Note = x.Note,
            UpdatedDate = x.UpdatedDate
        };
    }
}
