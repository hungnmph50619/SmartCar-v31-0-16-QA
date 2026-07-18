using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.Entities;
using SmartCar.Persistence.Context;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SmartCar.WebApi.Controllers
{
    [ApiController]
    [Route("api/system-settings")]
    [Authorize(Roles = "Admin")]
    public class SystemSettingsController : ControllerBase
    {
        private readonly CarBookContext _context;
        public SystemSettingsController(CarBookContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _context.SystemSettings.AsNoTracking().OrderBy(x => x.SettingKey).ToListAsync());

        [HttpPut("{key}")]
        public async Task<IActionResult> Upsert(string key, UpdateSystemSettingRequest request)
        {
            key = (key ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(key) || key.Length > 100) return BadRequest("Khóa cấu hình không hợp lệ.");
            if (!IsValidValue(request.ValueType, request.SettingValue)) return BadRequest("Giá trị không đúng kiểu dữ liệu đã chọn.");
            var adminId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            var entity = await _context.SystemSettings.FirstOrDefaultAsync(x => x.SettingKey == key);
            var oldValue = entity?.SettingValue;
            if (entity is null)
            {
                entity = new SystemSetting { SettingKey = key };
                _context.SystemSettings.Add(entity);
            }
            entity.SettingValue = request.SettingValue.Trim();
            entity.ValueType = request.ValueType.Trim();
            entity.Description = request.Description?.Trim();
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedByAppUserID = adminId;
            _context.AuditLogs.Add(new AuditLog
            {
                AppUserID = adminId,
                Action = "Cập nhật cấu hình nghiệp vụ",
                EntityName = nameof(SystemSetting),
                EntityID = key,
                OldValues = oldValue,
                NewValues = entity.SettingValue,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        private static bool IsValidValue(string? type, string? value)
            => (type ?? string.Empty).Trim() switch
            {
                "Integer" => int.TryParse(value, out _),
                "Decimal" => decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out _),
                "Boolean" => bool.TryParse(value, out _),
                "String" => value is not null,
                _ => false
            };
    }

    public sealed class UpdateSystemSettingRequest
    {
        [Required, MaxLength(1000)] public string SettingValue { get; set; } = string.Empty;
        [Required, MaxLength(30)] public string ValueType { get; set; } = "Integer";
        [MaxLength(500)] public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
