using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.Entities;
using SmartCar.Domain.Security;
using SmartCar.Dto.AccountDtos;
using SmartCar.Persistence.Context;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("auth")]
    public class AccountController : ControllerBase
    {
        private readonly CarBookContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;
        private readonly ISensitiveDataProtector _sensitiveData;

        public AccountController(
            CarBookContext context,
            IConfiguration configuration,
            ILogger<AccountController> logger,
            ISensitiveDataProtector? sensitiveData = null)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _sensitiveData = sensitiveData ?? new SensitiveDataProtector(configuration);
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();
            var username = (dto.Username ?? string.Empty).Trim();
            var user = await _context.AppUsers.FirstOrDefaultAsync(x => x.Username == username && x.Email.ToLower() == email);
            var genericMessage = "Nếu email tồn tại trong hệ thống, Smart Car đã gửi liên kết đặt lại mật khẩu.";
            if (user is null)
            {
                return Ok(new ForgotPasswordResponseDto { Message = genericMessage, EmailSent = true });
            }

            var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var tokenHash = HashToken(rawToken);
            var oldTokens = await _context.PasswordResetTokens
                .Where(x => x.AppUserID == user.AppUserId && x.UsedDate == null)
                .ToListAsync();
            foreach (var old in oldTokens) old.UsedDate = DateTime.UtcNow;

            var resetToken = new PasswordResetToken
            {
                AppUserID = user.AppUserId,
                TokenHash = tokenHash,
                CreatedDate = DateTime.UtcNow,
                ExpiresDate = DateTime.UtcNow.AddMinutes(30)
            };
            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            var webBaseUrl = _configuration["EmailSettings:WebBaseUrl"]?.TrimEnd('/') ?? "https://localhost:7154";
            var resetUrl = $"{webBaseUrl}/Account/ResetPassword?username={Uri.EscapeDataString(user.Username)}&email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(rawToken)}";
            var emailSent = await SendResetEmailAsync(user, resetUrl);

            if (!emailSent)
            {
                // Không để lại liên kết đặt lại mật khẩu còn hiệu lực khi email không được gửi.
                resetToken.UsedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // Không để người gọi suy ra tài khoản có tồn tại hay SMTP có lỗi.
            // Chi tiết gửi thất bại chỉ được ghi log nội bộ.
            return Ok(new ForgotPasswordResponseDto
            {
                Message = genericMessage,
                EmailSent = true
            });
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest("Mật khẩu nhập lại không khớp.");

            var passwordError = PasswordPolicy.Validate(dto.NewPassword);
            if (passwordError is not null)
                return BadRequest(passwordError);

            var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();
            var username = (dto.Username ?? string.Empty).Trim();
            var tokenHash = HashToken(dto.Token ?? string.Empty);
            var newPasswordHash = PasswordSecurity.Hash((dto.NewPassword ?? string.Empty).Trim());

            // SQL Server đang bật EnableRetryOnFailure. Transaction do ứng dụng tự mở
            // phải chạy trong execution strategy của EF Core, nếu không endpoint sẽ trả lỗi 500.
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                // Xóa trạng thái tracking nếu execution strategy phải chạy lại sau lỗi tạm thời.
                _context.ChangeTracker.Clear();

                await using var transaction = await _context.Database
                    .BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                try
                {
                    var user = await _context.AppUsers.FirstOrDefaultAsync(x =>
                        x.Username == username && x.Email.ToLower() == email);
                    if (user is null)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest("Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
                    }

                    var now = DateTime.UtcNow;
                    var resetToken = await _context.PasswordResetTokens.FirstOrDefaultAsync(x =>
                        x.AppUserID == user.AppUserId &&
                        x.TokenHash == tokenHash &&
                        x.UsedDate == null &&
                        x.ExpiresDate > now);
                    if (resetToken is null)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest("Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
                    }

                    user.Password = newPasswordHash;
                    user.FailedLoginCount = 0;
                    if (string.Equals(user.LockType, "Khóa tự động", StringComparison.OrdinalIgnoreCase))
                    {
                        user.LockoutEnd = null;
                        user.LockType = null;
                        user.LockReason = null;
                        user.LockedAt = null;
                        user.LockedByAppUserID = null;
                    }
                    user.TokenVersion++;

                    // Thu hồi toàn bộ liên kết đặt lại mật khẩu còn hiệu lực của tài khoản,
                    // bao gồm chính liên kết đang được sử dụng.
                    var remainingTokens = await _context.PasswordResetTokens
                        .Where(x => x.AppUserID == user.AppUserId && x.UsedDate == null)
                        .ToListAsync();
                    foreach (var token in remainingTokens)
                        token.UsedDate = now;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok("Đã đặt lại mật khẩu. Mọi phiên đăng nhập cũ đã bị thu hồi; bạn có thể đăng nhập bằng mật khẩu mới.");
                }
                catch (DbUpdateException ex)
                {
                    await transaction.RollbackAsync();
                    _context.ChangeTracker.Clear();
                    _logger.LogError(ex,
                        "Không thể đặt lại mật khẩu cho Username {Username} và Email {Email}",
                        username, email);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        "Không thể lưu mật khẩu mới. Vui lòng yêu cầu một liên kết đặt lại mật khẩu mới rồi thử lại.");
                }
            });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var id = GetCurrentUserId();
            var user = await _context.AppUsers.AsNoTracking().Include(x => x.AppRole)
                .FirstOrDefaultAsync(x => x.AppUserId == id);
            if (user is null) return NotFound("Không tìm thấy tài khoản.");

            var partnerProfile = user.IsVehiclePartner
                ? await _context.VehiclePartnerProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.AppUserID == id)
                : null;
            return Ok(MapProfile(user, partnerProfile));
        }

        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe(UpdateUserProfileDto dto)
        {
            var id = GetCurrentUserId();
            var user = await _context.AppUsers.Include(x => x.AppRole).FirstOrDefaultAsync(x => x.AppUserId == id);
            if (user is null) return NotFound("Không tìm thấy tài khoản.");
            if (string.Equals(user.AppRole.AppRoleName, "Staff", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Tài khoản nhân viên không được tự thay đổi thông tin hồ sơ. Vui lòng liên hệ quản trị viên.");
            if (user.IsVehiclePartner)
                return BadRequest("Hồ sơ đối tác đã được xác minh và chỉ được xem. Bạn chỉ có thể đổi mật khẩu hoặc yêu cầu đổi email.");

            var requestedEmail = dto.Email?.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(requestedEmail) && !string.Equals(requestedEmail, user.Email, StringComparison.OrdinalIgnoreCase))
                return BadRequest("Email không được thay đổi trực tiếp. Hãy dùng chức năng Yêu cầu đổi email và xác nhận OTP gửi tới email mới.");

            user.Name = (dto.Name ?? string.Empty).Trim();
            user.Surname = (dto.Surname ?? string.Empty).Trim();
            user.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : (dto.Phone ?? string.Empty).Trim();
            await _context.SaveChangesAsync();
            return Ok(MapProfile(user, null));
        }

        [Authorize]
        [HttpPost("request-email-change")]
        public async Task<IActionResult> RequestEmailChange(RequestEmailChangeDto dto)
        {
            var id = GetCurrentUserId();
            var user = await _context.AppUsers.FirstOrDefaultAsync(x => x.AppUserId == id);
            if (user is null) return NotFound("Không tìm thấy tài khoản.");
            if (!PasswordSecurity.Verify(user.Password, dto.CurrentPassword ?? string.Empty, out _))
                return BadRequest("Mật khẩu hiện tại không đúng.");

            var newEmail = (dto.NewEmail ?? string.Empty).Trim().ToLowerInvariant();
            try { _ = new MailAddress(newEmail); }
            catch (FormatException) { return BadRequest("Email mới không hợp lệ."); }
            if (string.Equals(newEmail, user.Email, StringComparison.OrdinalIgnoreCase))
                return BadRequest("Email mới phải khác email hiện tại.");

            var oldOtps = await _context.EmailVerificationOtps
                .Where(x => x.AppUserID == id && x.Purpose == "ChangeEmail" && x.UsedDate == null)
                .ToListAsync();
            foreach (var old in oldOtps) old.UsedDate = DateTime.UtcNow;

            var rawOtp = OtpSecurity.GenerateSixDigits();
            user.PendingEmail = newEmail;
            user.PendingEmailCreatedDate = DateTime.UtcNow;
            var record = new EmailVerificationOtp
            {
                AppUserID = id, Purpose = "ChangeEmail", TargetEmail = newEmail,
                OtpHash = OtpSecurity.Hash(OtpKey(), id.ToString(), "ChangeEmail", newEmail, rawOtp),
                CreatedDate = DateTime.UtcNow, ExpiresDate = DateTime.UtcNow.AddMinutes(5)
            };
            _context.EmailVerificationOtps.Add(record);
            await _context.SaveChangesAsync();

            if (!await SendEmailChangeOtpAsync(user, newEmail, rawOtp))
            {
                record.UsedDate = DateTime.UtcNow;
                user.PendingEmail = null;
                user.PendingEmailCreatedDate = null;
                await _context.SaveChangesAsync();
                return StatusCode(503, "Không gửi được OTP tới email mới. Email hiện tại chưa bị thay đổi.");
            }
            record.LastSentAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok("Đã gửi OTP tới email mới. Email hiện tại vẫn được giữ nguyên cho đến khi OTP được xác nhận.");
        }

        [Authorize]
        [HttpPost("confirm-email-change")]
        public async Task<IActionResult> ConfirmEmailChange(ConfirmEmailChangeDto dto)
        {
            var id = GetCurrentUserId();
            var user = await _context.AppUsers.FirstOrDefaultAsync(x => x.AppUserId == id);
            if (user is null) return NotFound("Không tìm thấy tài khoản.");
            if (string.IsNullOrWhiteSpace(user.PendingEmail)) return BadRequest("Không có yêu cầu đổi email đang chờ xác nhận.");

            var otp = await _context.EmailVerificationOtps
                .Where(x => x.AppUserID == id && x.Purpose == "ChangeEmail" && x.TargetEmail == user.PendingEmail && x.UsedDate == null)
                .OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync();
            if (otp is null || otp.ExpiresDate <= DateTime.UtcNow) return BadRequest("OTP không hợp lệ hoặc đã hết hạn.");
            if (otp.LockedUntil.HasValue && otp.LockedUntil > DateTime.UtcNow) return StatusCode(429, "OTP đang bị khóa tạm thời.");
            if (!OtpSecurity.Verify(OtpKey(), id.ToString(), "ChangeEmail", user.PendingEmail, (dto.Otp ?? string.Empty).Trim(), otp.OtpHash))
            {
                otp.FailedAttempts++;
                if (otp.FailedAttempts >= 5) { otp.FailedAttempts = 0; otp.LockedUntil = DateTime.UtcNow.AddMinutes(15); }
                await _context.SaveChangesAsync();
                return BadRequest("OTP không đúng.");
            }

            var suppliedOtp = (dto.Otp ?? string.Empty).Trim();
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                _context.ChangeTracker.Clear();

                await using var transaction = await _context.Database
                    .BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                try
                {
                    var currentUser = await _context.AppUsers
                        .FirstOrDefaultAsync(x => x.AppUserId == id);
                    if (currentUser is null)
                    {
                        await transaction.RollbackAsync();
                        return NotFound("Không tìm thấy tài khoản.");
                    }

                    if (string.IsNullOrWhiteSpace(currentUser.PendingEmail))
                    {
                        await transaction.RollbackAsync();
                        return BadRequest("Không có yêu cầu đổi email đang chờ xác nhận.");
                    }

                    var transactionNow = DateTime.UtcNow;
                    var currentOtp = await _context.EmailVerificationOtps
                        .Where(x => x.AppUserID == id &&
                                    x.Purpose == "ChangeEmail" &&
                                    x.TargetEmail == currentUser.PendingEmail &&
                                    x.UsedDate == null)
                        .OrderByDescending(x => x.CreatedDate)
                        .FirstOrDefaultAsync();

                    if (currentOtp is null || currentOtp.ExpiresDate <= transactionNow)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest("OTP không hợp lệ hoặc đã hết hạn.");
                    }

                    if (!OtpSecurity.Verify(
                            OtpKey(),
                            id.ToString(),
                            "ChangeEmail",
                            currentUser.PendingEmail,
                            suppliedOtp,
                            currentOtp.OtpHash))
                    {
                        await transaction.RollbackAsync();
                        return Conflict("OTP đã thay đổi hoặc không còn hiệu lực. Vui lòng nhập mã mới nhất.");
                    }

                    currentUser.Email = currentUser.PendingEmail;
                    currentUser.PendingEmail = null;
                    currentUser.PendingEmailCreatedDate = null;
                    currentUser.EmailConfirmed = true;
                    currentUser.TokenVersion++;

                    var activeOtps = await _context.EmailVerificationOtps
                        .Where(x => x.AppUserID == id &&
                                    x.Purpose == "ChangeEmail" &&
                                    x.UsedDate == null)
                        .ToListAsync();
                    foreach (var active in activeOtps)
                        active.UsedDate = transactionNow;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return Ok("Đổi email thành công. Mọi phiên đăng nhập cũ đã bị thu hồi; vui lòng đăng nhập lại.");
                }
                catch (DbUpdateException ex)
                {
                    await transaction.RollbackAsync();
                    _context.ChangeTracker.Clear();
                    _logger.LogError(ex, "Không thể xác nhận đổi email cho AppUserID {AppUserID}", id);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        "Không thể lưu email mới. Vui lòng thử lại hoặc yêu cầu gửi OTP mới.");
                }
            });
        }

        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest("Mật khẩu nhập lại không khớp.");
            var passwordError = PasswordPolicy.Validate(dto.NewPassword);
            if (passwordError is not null) return BadRequest(passwordError);

            var id = GetCurrentUserId();
            var user = await _context.AppUsers.FirstOrDefaultAsync(x => x.AppUserId == id);
            if (user is null) return NotFound("Không tìm thấy tài khoản.");
            if (!PasswordSecurity.Verify(user.Password, dto.CurrentPassword ?? string.Empty, out _))
                return BadRequest("Mật khẩu hiện tại không đúng.");
            if (dto.NewPassword == dto.CurrentPassword)
                return BadRequest("Mật khẩu mới phải khác mật khẩu hiện tại.");

            user.Password = PasswordSecurity.Hash((dto.NewPassword ?? string.Empty).Trim());
            user.TokenVersion++;
            await _context.SaveChangesAsync();
            return Ok("Đổi mật khẩu thành công. Vui lòng đăng nhập lại.");
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        private static string HashToken(string token)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }

        private UserProfileDto MapProfile(AppUser user, VehiclePartnerProfile? partnerProfile)
        {
            return new UserProfileDto
            {
                AppUserID = user.AppUserId,
                Username = user.Username,
                Name = user.Name,
                Surname = user.Surname,
                FullName = $"{user.Surname} {user.Name}".Trim(),
                Email = user.Email,
                PendingEmail = user.PendingEmail,
                Phone = user.Phone,
                Role = user.AppRole.AppRoleName,
                IsVehiclePartner = user.IsVehiclePartner,
                Address = partnerProfile?.Address,
                CitizenIdentityNumber = _sensitiveData.Mask(_sensitiveData.UnprotectOrLegacy(partnerProfile?.CitizenIdentityNumberEncrypted, partnerProfile?.CitizenIdentityNumber, "partner-citizen-id")),
                BankName = partnerProfile?.BankName,
                BankAccountNumber = _sensitiveData.Mask(_sensitiveData.UnprotectOrLegacy(partnerProfile?.BankAccountNumberEncrypted, partnerProfile?.BankAccountNumber, "partner-bank-account")),
                BankAccountHolder = partnerProfile?.BankAccountHolder,
                CustomerRating = 0,
                CustomerRatingCount = 0
            };
        }

        private string OtpKey()
            => _configuration["Security:OtpHmacKey"]
               ?? throw new InvalidOperationException("Thiếu Security:OtpHmacKey.");


        private async Task<bool> SendEmailChangeOtpAsync(AppUser user, string targetEmail, string otp)
        {
            var host = _configuration["EmailSettings:Host"];
            var username = _configuration["EmailSettings:UserName"];
            var appPassword = _configuration["EmailSettings:AppPassword"];
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? username;
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(appPassword) || string.IsNullOrWhiteSpace(fromEmail)) return false;
            try
            {
                var port = int.TryParse(_configuration["EmailSettings:Port"], out var value) ? value : 587;
                using var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, _configuration["EmailSettings:FromName"] ?? "Smart Car"),
                    Subject = "[SmartCar] Xác nhận email mới",
                    Body = $"<p>Xin chào {WebUtility.HtmlEncode(($"{user.Surname} {user.Name}").Trim())},</p><p>Mã OTP xác nhận email mới là:</p><h2>{otp}</h2><p>Mã có hiệu lực 5 phút.</p>",
                    IsBodyHtml = true
                };
                message.To.Add(targetEmail);
                using var smtp = new SmtpClient(host, port) { EnableSsl = true, UseDefaultCredentials = false, Credentials = new NetworkCredential(username, appPassword), Timeout = 30000 };
                await smtp.SendMailAsync(message);
                return true;
            }
            catch (Exception ex) when (ex is SmtpException or InvalidOperationException or FormatException)
            {
                _logger.LogError(ex, "Không gửi được OTP đổi email cho AppUser {AppUserId}", user.AppUserId);
                return false;
            }
        }

        private async Task<bool> SendResetEmailAsync(AppUser user, string resetUrl)
        {
            var host = _configuration["EmailSettings:Host"];
            var username = _configuration["EmailSettings:UserName"];
            var appPassword = _configuration["EmailSettings:AppPassword"];
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? username;
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(appPassword) || string.IsNullOrWhiteSpace(fromEmail))
                return false;

            var port = int.TryParse(_configuration["EmailSettings:Port"], out var value) ? value : 587;
            var fromName = _configuration["EmailSettings:FromName"] ?? "Smart Car";
            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = "Đặt lại mật khẩu Smart Car",
                Body = $"""
                    <div style='font-family:Arial,sans-serif;line-height:1.7;color:#243b53'>
                      <h2>Đặt lại mật khẩu Smart Car</h2>
                      <p>Xin chào {WebUtility.HtmlEncode(user.Name)},</p>
                      <p>Bạn vừa yêu cầu đặt lại mật khẩu. Liên kết dưới đây có hiệu lực trong 30 phút:</p>
                      <p><a href='{WebUtility.HtmlEncode(resetUrl)}' style='display:inline-block;padding:12px 20px;background:#168cf2;color:#fff;text-decoration:none;border-radius:6px'>Tạo mật khẩu mới</a></p>
                      <p>Nếu bạn không yêu cầu thao tác này, hãy bỏ qua email.</p>
                    </div>
                    """,
                IsBodyHtml = true
            };
            message.To.Add(user.Email);

            using var smtp = new SmtpClient(host, port)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username, appPassword),
                Timeout = 30000
            };
            try
            {
                await smtp.SendMailAsync(message);
                return true;
            }
            catch (Exception ex) when (ex is SmtpException or InvalidOperationException)
            {
                _logger.LogError(ex, "Không gửi được email đặt lại mật khẩu cho tài khoản {AppUserId}", user.AppUserId);
                return false;
            }
        }
    }
}
