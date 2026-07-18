using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.Entities;
using SmartCar.Domain.Time;
using SmartCar.Dto.NotificationDtos;
using SmartCar.Persistence.Context;
using System.Security.Claims;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CompanyAnnouncementsController : ControllerBase
    {
        private readonly CarBookContext _context;
        public CompanyAnnouncementsController(CarBookContext context) => _context = context;

        [HttpGet("me")]
        public async Task<IActionResult> GetForCurrentUser()
        {
            var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
            // PublishDate/ExpiresDate are business dates in Vietnam, not UTC timestamps.
            var today = VietnamTime.Today;
            var values = await _context.CompanyAnnouncements.AsNoTracking()
                .Where(x => x.IsActive && x.PublishDate <= today &&
                            (!x.ExpiresDate.HasValue || x.ExpiresDate >= today) &&
                            (x.AudienceRole == "All" || x.AudienceRole == role))
                .OrderByDescending(x => x.IsImportant)
                .ThenByDescending(x => x.PublishDate)
                .Take(30)
                .ToListAsync();
            return Ok(values.Select(Map));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var values = await _context.CompanyAnnouncements.AsNoTracking()
                .OrderByDescending(x => x.PublishDate).ToListAsync();
            return Ok(values.Select(Map));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateCompanyAnnouncementDto dto)
        {
            if (dto.AudienceRole is not ("All" or "Staff" or "Customer"))
                return BadRequest("Nhóm người nhận thông báo không hợp lệ.");

            var publishDate = dto.PublishDate == default ? VietnamTime.Today : dto.PublishDate.Date;
            var expiresDate = dto.ExpiresDate?.Date;
            if (expiresDate.HasValue && expiresDate < publishDate)
                return BadRequest("Ngày hết hạn phải sau hoặc bằng ngày đăng.");

            var entity = new CompanyAnnouncement
            {
                Title = (dto.Title ?? string.Empty).Trim(), Content = (dto.Content ?? string.Empty).Trim(), AudienceRole = dto.AudienceRole,
                IsImportant = dto.IsImportant, IsActive = dto.IsActive,
                PublishDate = publishDate, ExpiresDate = expiresDate,
                CreatedByAppUserID = GetCurrentUserId()
            };
            _context.CompanyAnnouncements.Add(entity);
            await _context.SaveChangesAsync();
            return Ok("Đã đăng thông báo.");
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.CompanyAnnouncements.FindAsync(id);
            if (entity is null) return NotFound("Không tìm thấy thông báo.");
            _context.CompanyAnnouncements.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok("Đã xóa thông báo.");
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : null;
        }

        private static ResultCompanyAnnouncementDto Map(CompanyAnnouncement x) => new()
        {
            CompanyAnnouncementID = x.CompanyAnnouncementID, Title = x.Title, Content = x.Content,
            AudienceRole = x.AudienceRole, IsImportant = x.IsImportant, IsActive = x.IsActive,
            PublishDate = x.PublishDate, ExpiresDate = x.ExpiresDate
        };
    }
}
