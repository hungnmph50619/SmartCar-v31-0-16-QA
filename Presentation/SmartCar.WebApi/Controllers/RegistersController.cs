using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SmartCar.Application.Features.Mediator.Commands.AppUserCommands;
using SmartCar.Domain.BusinessRules;
using SmartCar.Domain.Entities;
using SmartCar.Domain.Security;
using SmartCar.Dto.MarketplaceDtos;
using SmartCar.Dto.RegisterDtos;
using SmartCar.Persistence.Context;
using System.Data;
using System.Net;
using System.Net.Mail;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("auth")]
    public class RegistersController : ControllerBase
    {
        private const int EmailOtpMinutes = 5;
        private const int EmailOtpMaxAttempts = 5;
        private const int OtpResendCooldownSeconds = 60;
        private const int OtpMaxPerHour = 5;
        private const int OtpMaxPerDay = 10;

        private readonly CarBookContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RegistersController> _logger;

        public RegistersController(
            CarBookContext context,
            IConfiguration configuration,
            ILogger<RegistersController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Đăng ký khách. Theo đặc tả v1.0, AppUser chỉ được tạo sau khi OTP đúng.
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateAppUserCommand command)
        {
            command.Username = (command.Username ?? string.Empty).Trim();
            command.Email = (command.Email ?? string.Empty).Trim().ToLowerInvariant();
            command.Name = (command.Name ?? string.Empty).Trim();
            command.Surname = (command.Surname ?? string.Empty).Trim();
            command.Phone = NormalizePhone(command.Phone);

            var validation = ValidateBaseAccount(command.Username, command.Password, command.Name, command.Surname, command.Email);
            if (validation is not null) return BadRequest(validation);
            if (command.Password != command.ConfirmPassword) return BadRequest("Mật khẩu nhập lại không khớp.");
            if (!command.AgreeTerms) return BadRequest("Bạn cần đồng ý với điều khoản sử dụng của SmartCar.");
            if (!command.AgreePrivacy) return BadRequest("Bạn cần đồng ý với chính sách bảo mật của SmartCar.");
            if (!IsValidVietnamPhone(command.Phone)) return BadRequest("Số điện thoại phải gồm 10 số bắt đầu bằng 0 hoặc dạng +84.");

            return await CreateRegistrationAttemptAsync(
                command.Username,
                command.Password,
                command.Name,
                command.Surname,
                command.Email,
                command.Phone,
                AccountTypes.Customer,
                partnerType: null,
                termsVersion: "Terms-v1.0",
                privacyVersion: "Privacy-v1.0");
        }

        /// <summary>
        /// Đăng ký đối tác. AppUser và hồ sơ đối tác bản nháp chỉ được tạo sau khi OTP đúng.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("vehicle-partner")]
        public async Task<IActionResult> CreateVehiclePartner(CreateVehiclePartnerAccountDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage)));

            dto.Username = (dto.Username ?? string.Empty).Trim();
            dto.Email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();
            dto.Phone = NormalizePhone(dto.Phone);
            dto.PartnerType = NormalizePartnerType(dto.PartnerType);

            var validation = ValidateBaseAccount(dto.Username, dto.Password, dto.Username, "Đối tác", dto.Email);
            if (validation is not null) return BadRequest(validation);
            if (dto.Password != dto.ConfirmPassword) return BadRequest("Mật khẩu nhập lại không khớp.");
            if (!IsValidVietnamPhone(dto.Phone)) return BadRequest("Số điện thoại phải gồm 10 số bắt đầu bằng 0 hoặc dạng +84.");
            if (dto.PartnerType is not (CreateVehiclePartnerAccountDto.IndividualPartnerType or CreateVehiclePartnerAccountDto.OrganizationPartnerType))
                return BadRequest("Loại đối tác không hợp lệ.");
            if (!dto.AgreePartnerTerms) return BadRequest("Bạn cần đồng ý với điều khoản đối tác của SmartCar.");
            if (!dto.AgreePrivacyPolicy) return BadRequest("Bạn cần đồng ý với chính sách bảo mật của SmartCar.");

            return await CreateRegistrationAttemptAsync(
                dto.Username,
                dto.Password,
                dto.Username,
                "Đối tác",
                dto.Email,
                dto.Phone,
                AccountTypes.Partner,
                dto.PartnerType,
                "Partner-Terms-v1.0",
                "Privacy-v1.0");
        }

        [AllowAnonymous]
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail(VerifyEmailOtpDto dto)
        {
            var attempt = await FindAttemptAsync(dto.RegistrationAttemptId, dto.Username, dto.Email);
            if (attempt is null)
                return BadRequest("Yêu cầu đăng ký hoặc OTP không hợp lệ.");

            var now = DateTime.UtcNow;
            if (attempt.Status == "Completed")
                return Ok("Email đã được xác minh trước đó. Bạn có thể đăng nhập.");
            if (attempt.Status == "Locked")
                return StatusCode(StatusCodes.Status429TooManyRequests, "OTP đã bị khóa do nhập sai quá 5 lần. Vui lòng tạo lại yêu cầu đăng ký.");
            if (attempt.Status != "Pending" || attempt.ExpiresAt <= now || attempt.OtpExpiresAt <= now)
            {
                attempt.Status = "Expired";
                await _context.SaveChangesAsync();
                return BadRequest("OTP đã hết hạn sau 5 phút. Vui lòng đăng ký lại hoặc yêu cầu gửi mã mới.");
            }

            var suppliedOtp = (dto.Otp ?? string.Empty).Trim();
            if (!OtpSecurity.Verify(OtpKey(), attempt.RegistrationAttemptID.ToString("N"), "Register", attempt.Email, suppliedOtp, attempt.OtpHash))
            {
                attempt.FailedAttempts++;
                if (attempt.FailedAttempts >= EmailOtpMaxAttempts)
                    attempt.Status = "Locked";
                await _context.SaveChangesAsync();
                return BadRequest(attempt.Status == "Locked"
                    ? "OTP không đúng và yêu cầu đã bị khóa sau 5 lần nhập sai."
                    : $"OTP không đúng. Bạn còn {EmailOtpMaxAttempts - attempt.FailedAttempts} lần thử.");
            }

            // SQL Server đang bật EnableRetryOnFailure trong Program.cs. Vì vậy mọi transaction
            // do ứng dụng tự mở phải chạy bên trong execution strategy của EF Core.
            var registrationAttemptId = attempt.RegistrationAttemptID;
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                // Nếu execution strategy phải chạy lại, loại bỏ trạng thái entity còn sót từ lần trước.
                _context.ChangeTracker.Clear();

                await using var transaction = await _context.Database
                    .BeginTransactionAsync(IsolationLevel.Serializable);
                try
                {
                    var currentAttempt = await _context.RegistrationAttempts
                        .FirstOrDefaultAsync(x => x.RegistrationAttemptID == registrationAttemptId);

                    var transactionNow = DateTime.UtcNow;
                    if (currentAttempt is null ||
                        currentAttempt.Status != "Pending" ||
                        currentAttempt.ExpiresAt <= transactionNow ||
                        currentAttempt.OtpExpiresAt <= transactionNow)
                    {
                        await transaction.RollbackAsync();
                        return Conflict("Yêu cầu đăng ký đã được xử lý hoặc OTP đã hết hạn.");
                    }

                    // Kiểm tra lại OTP trên bản ghi mới nhất. Điều này ngăn mã OTP cũ được dùng
                    // nếu người dùng vừa bấm gửi lại mã trong lúc yêu cầu đang được xử lý.
                    if (!OtpSecurity.Verify(
                            OtpKey(),
                            currentAttempt.RegistrationAttemptID.ToString("N"),
                            "Register",
                            currentAttempt.Email,
                            suppliedOtp,
                            currentAttempt.OtpHash))
                    {
                        await transaction.RollbackAsync();
                        return Conflict("Mã OTP đã thay đổi hoặc không còn hiệu lực. Vui lòng nhập mã mới nhất trong email.");
                    }

                    var duplicate = await GetDuplicateAccountMessageAsync(
                        currentAttempt.Username,
                        currentAttempt.Email,
                        currentAttempt.Phone,
                        currentAttempt.AccountType);
                    if (duplicate is not null)
                    {
                        currentAttempt.Status = "Rejected";
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return Conflict(duplicate);
                    }

                    var roleName = currentAttempt.AccountType == AccountTypes.Partner
                        ? "VehiclePartner"
                        : "Customer";
                    var role = await _context.AppRoles.FirstOrDefaultAsync(x => x.AppRoleName == roleName);
                    if (role is null)
                    {
                        await transaction.RollbackAsync();
                        return StatusCode(500, $"Không tìm thấy vai trò {roleName}. Hãy chạy script cơ sở dữ liệu v31.0.");
                    }

                    var user = new AppUser
                    {
                        Username = currentAttempt.Username,
                        Password = currentAttempt.PasswordHash,
                        Name = currentAttempt.Name,
                        Surname = currentAttempt.Surname,
                        Email = currentAttempt.Email,
                        Phone = currentAttempt.Phone,
                        AppRoleId = role.AppRoleId,
                        AccountType = currentAttempt.AccountType,
                        IsVehiclePartner = currentAttempt.AccountType == AccountTypes.Partner,
                        EmailConfirmed = true,
                        RegistrationExpiresDate = null,
                        IsActive = true
                    };
                    _context.AppUsers.Add(user);
                    await _context.SaveChangesAsync();

                    if (currentAttempt.AccountType == AccountTypes.Partner)
                    {
                        _context.VehiclePartnerProfiles.Add(new VehiclePartnerProfile
                        {
                            AppUserID = user.AppUserId,
                            PartnerType = currentAttempt.PartnerType ?? CreateVehiclePartnerAccountDto.IndividualPartnerType,
                            Phone = currentAttempt.Phone,
                            Email = currentAttempt.Email,
                            Status = VerificationStatuses.Draft,
                            PartnerTermsVersion = currentAttempt.TermsVersion,
                            PrivacyPolicyVersion = currentAttempt.PrivacyVersion,
                            TermsAcceptedAt = currentAttempt.TermsAcceptedAt,
                            PrivacyAcceptedAt = currentAttempt.PrivacyAcceptedAt,
                            CreatedDate = DateTime.UtcNow
                        });
                    }

                    currentAttempt.Status = "Completed";
                    currentAttempt.VerifiedAt = DateTime.UtcNow;
                    currentAttempt.CreatedAppUserID = user.AppUserId;

                    await AddDuplicateContactFraudFlagsAsync(user);
                    _context.AuditLogs.Add(new AuditLog
                    {
                        AppUserID = user.AppUserId,
                        Action = "Xác minh email và tạo tài khoản",
                        EntityName = nameof(RegistrationAttempt),
                        EntityID = currentAttempt.RegistrationAttemptID.ToString(),
                        NewValues = $"AccountType={currentAttempt.AccountType}; Username={currentAttempt.Username}",
                        Note = "AppUser chỉ được tạo sau khi OTP đăng ký hợp lệ theo BR-OTP/FR-AUTH v1.0.",
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                    });

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(currentAttempt.AccountType == AccountTypes.Partner
                        ? "Xác minh email thành công. Tài khoản đối tác đã được tạo; vui lòng hoàn thiện hồ sơ đối tác trước khi gửi xe lên duyệt."
                        : "Xác minh email thành công. Tài khoản khách đã được tạo; bạn có thể xem xe, đặt xe có tài xế và hoàn thiện hồ sơ để thuê xe tự lái.");
                }
                catch (DbUpdateException ex)
                {
                    await transaction.RollbackAsync();
                    _context.ChangeTracker.Clear();
                    _logger.LogWarning(
                        ex,
                        "Xung đột khi hoàn tất RegistrationAttempt {RegistrationAttemptId}",
                        registrationAttemptId);
                    return Conflict("Tên đăng nhập, email hoặc số điện thoại theo loại tài khoản vừa được sử dụng bởi yêu cầu khác.");
                }
            });
        }

        [AllowAnonymous]
        [HttpPost("resend-email-otp")]
        public async Task<IActionResult> ResendEmailOtp(ResendEmailOtpDto dto)
        {
            var attempt = await FindAttemptAsync(dto.RegistrationAttemptId, dto.Username, dto.Email);
            if (attempt is null)
                return Ok("Nếu yêu cầu đăng ký còn hiệu lực, hệ thống sẽ gửi lại OTP.");
            if (attempt.Status == "Completed")
                return Ok("Email đã được xác minh. Bạn có thể đăng nhập.");
            if (attempt.Status == "Locked")
                return StatusCode(StatusCodes.Status429TooManyRequests, "Yêu cầu đã bị khóa do nhập sai OTP quá nhiều lần. Vui lòng đăng ký lại.");

            var now = DateTime.UtcNow;
            if (attempt.LastSentAt.HasValue && attempt.LastSentAt.Value.AddSeconds(OtpResendCooldownSeconds) > now)
            {
                var seconds = Math.Max(1, (int)Math.Ceiling((attempt.LastSentAt.Value.AddSeconds(OtpResendCooldownSeconds) - now).TotalSeconds));
                return StatusCode(StatusCodes.Status429TooManyRequests, $"Vui lòng chờ {seconds} giây trước khi gửi lại OTP.");
            }

            ResetRateWindows(attempt, now);
            if (attempt.SendCountHour >= OtpMaxPerHour)
                return StatusCode(StatusCodes.Status429TooManyRequests, "Đã vượt quá 5 lần gửi OTP trong một giờ.");
            if (attempt.SendCountDay >= OtpMaxPerDay)
                return StatusCode(StatusCodes.Status429TooManyRequests, "Đã vượt quá 10 lần gửi OTP trong 24 giờ.");

            var result = await IssueAndSendOtpAsync(attempt);
            return result.EmailSent
                ? Ok(new RegisterResultDto
                {
                    RegistrationAttemptId = attempt.RegistrationAttemptID,
                    Username = attempt.Username,
                    Email = attempt.Email,
                    AccountType = attempt.AccountType,
                    EmailSent = true,
                    OtpExpiresInMinutes = EmailOtpMinutes,
                    ResendAfterSeconds = OtpResendCooldownSeconds,
                    Message = "Đã gửi OTP mới. OTP cũ mất hiệu lực ngay; mã mới có hiệu lực 5 phút."
                })
                : StatusCode(StatusCodes.Status503ServiceUnavailable, result.Message);
        }

        private async Task<IActionResult> CreateRegistrationAttemptAsync(
            string username,
            string password,
            string name,
            string surname,
            string email,
            string phone,
            string accountType,
            string? partnerType,
            string termsVersion,
            string privacyVersion)
        {
            var duplicate = await GetDuplicateAccountMessageAsync(username, email, phone, accountType);
            if (duplicate is not null) return Conflict(duplicate);

            var now = DateTime.UtcNow;
            var expiredAttempts = await _context.RegistrationAttempts
                .Where(x => x.Status == "Pending" && x.ExpiresAt <= now &&
                    (x.Username == username || (x.AccountType == accountType && x.Email == email)))
                .ToListAsync();
            foreach (var expired in expiredAttempts) expired.Status = "Expired";
            if (expiredAttempts.Count > 0) await _context.SaveChangesAsync();

            var pendingByUsername = await _context.RegistrationAttempts
                .Where(x => x.Username == username && x.Status == "Pending" && x.ExpiresAt > now)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
            if (pendingByUsername is not null && pendingByUsername.ExpiresAt > now &&
                (!string.Equals(pendingByUsername.Email, email, StringComparison.OrdinalIgnoreCase) || pendingByUsername.AccountType != accountType))
                return Conflict("Tên đăng nhập đang thuộc một yêu cầu đăng ký khác còn hiệu lực.");

            var activeByEmailAndType = await _context.RegistrationAttempts
                .Where(x => x.AccountType == accountType && x.Email == email && x.Status == "Pending" && x.ExpiresAt > now)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
            if (activeByEmailAndType is not null && activeByEmailAndType.Username != username)
                return Conflict("Email đang có một yêu cầu đăng ký cùng loại tài khoản. Vui lòng hoàn tất hoặc chờ yêu cầu hết hạn.");

            var attempt = activeByEmailAndType ?? pendingByUsername;
            if (attempt is null || attempt.ExpiresAt <= now)
            {
                attempt = new RegistrationAttempt();
                _context.RegistrationAttempts.Add(attempt);
            }

            attempt.Username = username;
            attempt.PasswordHash = PasswordSecurity.Hash(password);
            attempt.Name = name;
            attempt.Surname = surname;
            attempt.Email = email;
            attempt.Phone = phone;
            attempt.AccountType = accountType;
            attempt.PartnerType = partnerType;
            attempt.TermsVersion = termsVersion;
            attempt.PrivacyVersion = privacyVersion;
            attempt.TermsAcceptedAt = now;
            attempt.PrivacyAcceptedAt = now;
            attempt.Status = "Pending";
            attempt.FailedAttempts = 0;
            attempt.CreatedAt = attempt.CreatedAt == default ? now : attempt.CreatedAt;
            attempt.ExpiresAt = now.AddMinutes(EmailOtpMinutes);

            try
            {
                var sendResult = await IssueAndSendOtpAsync(attempt);
                return Ok(new RegisterResultDto
                {
                    RegistrationAttemptId = attempt.RegistrationAttemptID,
                    Username = attempt.Username,
                    Email = attempt.Email,
                    AccountType = accountType,
                    EmailSent = sendResult.EmailSent,
                    OtpExpiresInMinutes = EmailOtpMinutes,
                    ResendAfterSeconds = OtpResendCooldownSeconds,
                    Message = sendResult.EmailSent
                        ? "Đã tạo yêu cầu đăng ký và gửi OTP. Tài khoản chỉ được tạo sau khi OTP đúng."
                        : sendResult.Message
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Xung đột yêu cầu đăng ký Username={Username}, Email={Email}, AccountType={AccountType}", username, email, accountType);
                return Conflict("Đang có một yêu cầu đăng ký cùng tên đăng nhập hoặc cùng email và loại tài khoản. Vui lòng dùng OTP đã gửi hoặc chờ yêu cầu hết hạn.");
            }
        }

        private async Task<(bool EmailSent, string Message)> IssueAndSendOtpAsync(RegistrationAttempt attempt)
        {
            var emailProblem = GetEmailSettingsProblem();
            var now = DateTime.UtcNow;
            if (attempt.LastSentAt.HasValue && attempt.LastSentAt.Value.AddSeconds(OtpResendCooldownSeconds) > now)
            {
                var seconds = Math.Max(1, (int)Math.Ceiling((attempt.LastSentAt.Value.AddSeconds(OtpResendCooldownSeconds) - now).TotalSeconds));
                return (false, $"Vui lòng chờ {seconds} giây trước khi gửi lại OTP.");
            }
            ResetRateWindows(attempt, now);
            if (attempt.SendCountHour >= OtpMaxPerHour)
                return (false, "Đã vượt quá 5 lần gửi OTP trong một giờ.");
            if (attempt.SendCountDay >= OtpMaxPerDay)
                return (false, "Đã vượt quá 10 lần gửi OTP trong 24 giờ.");

            var otp = OtpSecurity.GenerateSixDigits();
            attempt.OtpHash = OtpSecurity.Hash(OtpKey(), attempt.RegistrationAttemptID.ToString("N"), "Register", attempt.Email, otp);
            attempt.OtpExpiresAt = now.AddMinutes(EmailOtpMinutes);
            attempt.ExpiresAt = now.AddMinutes(EmailOtpMinutes);
            attempt.LastSentAt = now;
            attempt.SendCountHour++;
            attempt.SendCountDay++;
            attempt.FailedAttempts = 0;
            attempt.Status = "Pending";
            await _context.SaveChangesAsync();

            if (emailProblem is not null)
            {
                _logger.LogWarning("Không gửi OTP cho RegistrationAttempt {AttemptId}: {Reason}", attempt.RegistrationAttemptID, emailProblem);
                return (false, emailProblem);
            }

            try
            {
                await SendRegistrationOtpEmailAsync(attempt, otp);
                return (true, "Đã gửi OTP.");
            }
            catch (Exception ex) when (ex is SmtpException or InvalidOperationException or FormatException)
            {
                _logger.LogError(ex, "Không gửi được OTP cho RegistrationAttempt {AttemptId}", attempt.RegistrationAttemptID);
                return (false, "Không gửi được OTP qua email. Hãy kiểm tra cấu hình Gmail/App Password rồi thử lại.");
            }
        }

        private async Task<RegistrationAttempt?> FindAttemptAsync(Guid? id, string? username, string? email)
        {
            if (id.HasValue && id.Value != Guid.Empty)
                return await _context.RegistrationAttempts.FirstOrDefaultAsync(x => x.RegistrationAttemptID == id.Value);

            var normalizedUsername = (username ?? string.Empty).Trim();
            var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
            return await _context.RegistrationAttempts
                .Where(x => x.Username == normalizedUsername && x.Email == normalizedEmail)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        private async Task<string?> GetDuplicateAccountMessageAsync(string username, string email, string phone, string accountType)
        {
            if (await _context.AppUsers.IgnoreQueryFilters().AnyAsync(x => x.Username == username))
                return "Tên đăng nhập đã được sử dụng.";
            if (await _context.AppUsers.IgnoreQueryFilters().AnyAsync(x => x.AccountType == accountType && x.Email == email))
                return accountType == AccountTypes.Partner
                    ? "Email đã được sử dụng cho một tài khoản đối tác."
                    : "Email đã được sử dụng cho một tài khoản khách.";
            if (await _context.AppUsers.IgnoreQueryFilters().AnyAsync(x => x.AccountType == accountType && x.Phone == phone))
                return accountType == AccountTypes.Partner
                    ? "Số điện thoại đã được sử dụng cho một tài khoản đối tác."
                    : "Số điện thoại đã được sử dụng cho một tài khoản khách.";
            return null;
        }

        private async Task AddDuplicateContactFraudFlagsAsync(AppUser user)
        {
            if (await _context.AppUsers.IgnoreQueryFilters().AsNoTracking().AnyAsync(x =>
                    x.AppUserId != user.AppUserId && x.Email == user.Email && x.AccountType != user.AccountType))
            {
                _context.FraudFlags.Add(new FraudFlag
                {
                    AppUserID = user.AppUserId,
                    RuleCode = "CROSS_ROLE_EMAIL",
                    Description = "Email được dùng cho cả tài khoản khách và đối tác; nghiệp vụ cho phép nhưng cần đối chiếu khi có bất thường.",
                    RiskScore = 10,
                    Status = "Mới",
                    CreatedDate = DateTime.UtcNow
                });
            }

            if (!string.IsNullOrWhiteSpace(user.Phone) && await _context.AppUsers.IgnoreQueryFilters().AsNoTracking().AnyAsync(x =>
                    x.AppUserId != user.AppUserId && x.Phone == user.Phone && x.AccountType != user.AccountType))
            {
                _context.FraudFlags.Add(new FraudFlag
                {
                    AppUserID = user.AppUserId,
                    RuleCode = "CROSS_ROLE_PHONE",
                    Description = "Số điện thoại được dùng cho cả tài khoản khách và đối tác; nghiệp vụ cho phép nhưng cần đối chiếu khi có bất thường.",
                    RiskScore = 10,
                    Status = "Mới",
                    CreatedDate = DateTime.UtcNow
                });
            }
        }

        private async Task SendRegistrationOtpEmailAsync(RegistrationAttempt attempt, string otp)
        {
            var problem = GetEmailSettingsProblem();
            if (problem is not null) throw new InvalidOperationException(problem);

            var host = _configuration["EmailSettings:Host"]!;
            var username = _configuration["EmailSettings:UserName"]!;
            var appPassword = _configuration["EmailSettings:AppPassword"]!;
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? username;
            var port = int.TryParse(_configuration["EmailSettings:Port"], out var value) ? value : 587;
            var fromName = _configuration["EmailSettings:FromName"] ?? "Smart Car";

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = "[SmartCar] Mã OTP xác minh yêu cầu đăng ký",
                Body = $"""
                    <div style='font-family:Arial,sans-serif;line-height:1.7;color:#243b53'>
                      <h2>Xác minh đăng ký SmartCar</h2>
                      <p>Xin chào {WebUtility.HtmlEncode($"{attempt.Surname} {attempt.Name}".Trim())},</p>
                      <p>Mã OTP của yêu cầu đăng ký <b>{WebUtility.HtmlEncode(attempt.Username)}</b> là:</p>
                      <p style='font-size:30px;font-weight:bold;letter-spacing:8px'>{otp}</p>
                      <p>Mã có hiệu lực {EmailOtpMinutes} phút, tối đa {EmailOtpMaxAttempts} lần nhập sai và chỉ dùng một lần.</p>
                      <p>SmartCar chưa tạo tài khoản chính thức cho đến khi mã OTP được xác minh đúng.</p>
                    </div>
                    """,
                IsBodyHtml = true
            };
            message.To.Add(attempt.Email);
            using var smtp = new SmtpClient(host, port)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username, appPassword),
                Timeout = 30000
            };
            await smtp.SendMailAsync(message);
        }

        private string? GetEmailSettingsProblem()
        {
            var host = _configuration["EmailSettings:Host"];
            var username = _configuration["EmailSettings:UserName"];
            var appPassword = _configuration["EmailSettings:AppPassword"];
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? username;
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(appPassword) || string.IsNullOrWhiteSpace(fromEmail))
                return "Chưa cấu hình EmailSettings để gửi OTP.";
            if (username.Contains("your-email", StringComparison.OrdinalIgnoreCase) ||
                appPassword.Contains("your-gmail-app-password", StringComparison.OrdinalIgnoreCase) ||
                fromEmail.Contains("your-email", StringComparison.OrdinalIgnoreCase))
                return "Email gửi OTP vẫn đang để giá trị mẫu. Hãy thay Gmail và App Password thật trong cấu hình cục bộ.";
            return null;
        }

        private static void ResetRateWindows(RegistrationAttempt attempt, DateTime now)
        {
            if (attempt.HourWindowStartedAt.AddHours(1) <= now)
            {
                attempt.HourWindowStartedAt = now;
                attempt.SendCountHour = 0;
            }
            if (attempt.DayWindowStartedAt.AddHours(24) <= now)
            {
                attempt.DayWindowStartedAt = now;
                attempt.SendCountDay = 0;
            }
        }

        private string OtpKey()
            => _configuration["Security:OtpHmacKey"]
               ?? throw new InvalidOperationException("Thiếu Security:OtpHmacKey.");

        private static string NormalizePhone(string? phone)
        {
            var value = (phone ?? string.Empty).Trim().Replace(" ", string.Empty).Replace("-", string.Empty).Replace(".", string.Empty);
            return value.StartsWith("+84", StringComparison.Ordinal) ? "0" + value[3..] : value;
        }

        private static bool IsValidVietnamPhone(string phone)
            => System.Text.RegularExpressions.Regex.IsMatch(phone, @"^0\d{9}$");

        private static string? ValidateBaseAccount(string username, string password, string name, string surname, string email)
        {
            if (username.Length is < 4 or > 30 ||
                !System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9_.]+$") ||
                string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(surname))
                return "Thông tin đăng ký chưa hợp lệ.";
            var passwordError = PasswordPolicy.Validate(password);
            if (passwordError is not null) return passwordError;
            try { _ = new MailAddress(email); }
            catch (FormatException) { return "Email không hợp lệ."; }
            return null;
        }

        private static string NormalizePartnerType(string? value)
        {
            var text = (value ?? string.Empty).Trim();
            return string.Equals(text, "Doanh nghiệp", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(text, "Doanh nghiệp/Tổ chức", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(text, "Tổ chức", StringComparison.OrdinalIgnoreCase)
                ? CreateVehiclePartnerAccountDto.OrganizationPartnerType
                : CreateVehiclePartnerAccountDto.IndividualPartnerType;
        }
    }
}
