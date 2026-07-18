using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.BusinessRules;
using SmartCar.Domain.Entities;
using SmartCar.Domain.Security;
using SmartCar.Dto.StaffDtos;
using SmartCar.Persistence.Context;
using System.Net.Mail;

namespace SmartCar.WebApi.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class StaffAccountsController : ControllerBase
    {
        private readonly CarBookContext _context;

        public StaffAccountsController(CarBookContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var values = await _context.AppUsers
                .AsNoTracking()
                .Include(x => x.AppRole)
                .Where(x => x.AppRole.AppRoleName == "Staff")
                .OrderBy(x => x.Surname)
                .ThenBy(x => x.Name)
                .Select(x => new ResultStaffDto
                {
                    AppUserId = x.AppUserId,
                    Username = x.Username,
                    FullName = (x.Surname + " " + x.Name).Trim(),
                    Email = x.Email,
                    Role = x.AppRole.AppRoleName
                })
                .ToListAsync();
            return Ok(values);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateStaffDto dto)
        {
            var username = dto.Username?.Trim() ?? string.Empty;
            var password = dto.Password?.Trim() ?? string.Empty;
            var fullName = dto.FullName?.Trim() ?? string.Empty;
            var email = dto.Email?.Trim().ToLowerInvariant() ?? string.Empty;

            if (username.Length < 4 || string.IsNullOrWhiteSpace(fullName))
                return BadRequest("Tên đăng nhập hoặc họ tên chưa hợp lệ.");
            var passwordError = PasswordPolicy.Validate(password);
            if (passwordError is not null) return BadRequest(passwordError);
            try { _ = new MailAddress(email); }
            catch (FormatException) { return BadRequest("Email không hợp lệ."); }

            if (await _context.AppUsers.IgnoreQueryFilters().AnyAsync(x => x.Username == username))
                return Conflict("Tên đăng nhập đã tồn tại.");
            if (await _context.AppUsers.IgnoreQueryFilters().AnyAsync(x => x.Email == email))
                return Conflict("Email đã thuộc một tài khoản khác. Nhân viên không được đồng thời sử dụng tài khoản khách hoặc đối tác trong phạm vi đồ án.");

            var role = await _context.AppRoles.FirstOrDefaultAsync(x => x.AppRoleName == "Staff");
            if (role is null)
            {
                role = new AppRole { AppRoleName = "Staff" };
                _context.AppRoles.Add(role);
                await _context.SaveChangesAsync();
            }

            SplitName(fullName, out var surname, out var name);
            var user = new AppUser
            {
                Username = username,
                Password = PasswordSecurity.Hash(password),
                Name = name,
                Surname = surname,
                Email = email,
                AppRoleId = role.AppRoleId,
                AccountType = AccountTypes.Staff,
                EmailConfirmed = true,
                IsActive = true
            };
            _context.AppUsers.Add(user);
            await _context.SaveChangesAsync();
            _context.AuditLogs.Add(new AuditLog
            {
                AppUserID = int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var adminId) ? adminId : null,
                Action = "Tạo tài khoản nhân viên",
                EntityName = nameof(AppUser),
                EntityID = user.AppUserId.ToString(),
                NewValues = $"Username={user.Username}; Email={user.Email}; AccountType={user.AccountType}",
                CreatedDate = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync();
            return Ok("Đã tạo tài khoản nhân viên.");
        }

        [HttpPut("{id:int}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, ResetStaffPasswordDto dto)
        {
            var passwordError = PasswordPolicy.Validate(dto.NewPassword);
            if (passwordError is not null) return BadRequest(passwordError);

            var user = await _context.AppUsers
                .Include(x => x.AppRole)
                .FirstOrDefaultAsync(x => x.AppUserId == id && x.AppRole.AppRoleName == "Staff");
            if (user is null) return NotFound("Không tìm thấy tài khoản nhân viên.");

            user.Password = PasswordSecurity.Hash((dto.NewPassword ?? string.Empty).Trim());
            user.TokenVersion++;
            await _context.SaveChangesAsync();
            return Ok("Đã đặt lại mật khẩu nhân viên.");
        }

        private static void SplitName(string fullName, out string surname, out string name)
        {
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            name = parts.Length == 0 ? fullName : parts[^1];
            surname = parts.Length <= 1 ? string.Empty : string.Join(' ', parts[..^1]);
        }
    }
}
