using MediatR;
using SmartCar.Application.Features.Mediator.Queries.AppUserQueries;
using SmartCar.Application.Features.Mediator.Results.AppUserResults;
using SmartCar.Application.Interfaces;
using SmartCar.Domain.Entities;
using SmartCar.Domain.Security;

namespace SmartCar.Application.Features.Mediator.Handlers.AppUserHandlers
{
    public class GetCheckAppUserQueryHandler : IRequestHandler<GetCheckAppUserQuery, GetCheckAppUserQueryResult>
    {
        private readonly IRepository<AppUser> _appUserRepository;
        private readonly IRepository<AppRole> _appRoleRepository;
        private readonly IRepository<AuditLog> _auditLogRepository;

        public GetCheckAppUserQueryHandler(
            IRepository<AppUser> appUserRepository,
            IRepository<AppRole> appRoleRepository,
            IRepository<AuditLog> auditLogRepository)
        {
            _appUserRepository = appUserRepository;
            _appRoleRepository = appRoleRepository;
            _auditLogRepository = auditLogRepository;
        }

        public async Task<GetCheckAppUserQueryResult> Handle(GetCheckAppUserQuery request, CancellationToken cancellationToken)
        {
            var values = new GetCheckAppUserQueryResult();
            var username = request.Username?.Trim() ?? string.Empty;
            var password = request.Password ?? string.Empty;
            var user = await _appUserRepository.GetByFilterAsync(x => x.Username == username);
            AppRole? role = null;

            if (user != null)
            {
                role = await _appRoleRepository.GetByFilterAsync(x => x.AppRoleId == user.AppRoleId);
                var roleName = role?.AppRoleName ?? string.Empty;

                if (!user.IsActive || user.IsDeleted)
                {
                    user = null;
                }
                else if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                {
                    values.LockoutEnd = user.LockoutEnd;
                    values.FailureReason = BuildLockoutMessage(
                        user.LockoutEnd.Value,
                        string.Equals(user.LockType, "Khóa tự động", StringComparison.OrdinalIgnoreCase));
                    user = null;
                }
                else if (!PasswordSecurity.Verify(user.Password, password, out var needsUpgrade))
                {
                    user.FailedLoginCount++;
                    if (user.FailedLoginCount >= 5)
                    {
                        user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                        user.FailedLoginCount = 0;
                        user.LockType = "Khóa tự động";
                        user.LockReason = "Sai mật khẩu 5 lần liên tiếp.";
                        user.LockedAt = DateTime.UtcNow;
                        user.LockedByAppUserID = null;
                        values.LockoutEnd = user.LockoutEnd;
                        values.FailureReason = BuildLockoutMessage(user.LockoutEnd.Value, true);
                    }
                    await _appUserRepository.UpdateAsync(user);
                    if (values.LockoutEnd.HasValue)
                    {
                        await _auditLogRepository.CreateAsync(new AuditLog
                        {
                            AppUserID = user.AppUserId,
                            Action = "Khóa đăng nhập tự động",
                            EntityName = nameof(AppUser),
                            EntityID = user.AppUserId.ToString(),
                            Note = $"Sai mật khẩu 5 lần liên tiếp. Tạm khóa đến {values.LockoutEnd.Value:O} UTC."
                        });
                    }
                    user = null;
                }
                else if (!user.EmailConfirmed)
                {
                    values.FailureReason = "Tài khoản chưa xác minh email. Vui lòng nhập OTP đã gửi về email trước khi đăng nhập.";
                    user = null;
                }
                else
                {
                    user.FailedLoginCount = 0;
                    user.LockoutEnd = null;
                    user.LockType = null;
                    user.LockReason = null;
                    user.LockedAt = null;
                    user.LockedByAppUserID = null;
                    user.LastLoginAt = DateTime.UtcNow;
                    if (needsUpgrade) user.Password = PasswordSecurity.Hash(password);
                    await _appUserRepository.UpdateAsync(user);
                }
            }
            if (user == null)
            {
                values.IsExist = false;
            }
            else
            {
                values.IsExist = true;
                values.Username = user.Username;
                values.Email = user.Email;
                values.FullName = $"{user.Surname} {user.Name}".Trim();
                values.Role = role?.AppRoleName ?? (await _appRoleRepository.GetByFilterAsync(x => x.AppRoleId == user.AppRoleId)).AppRoleName;
                values.Id = user.AppUserId;
                values.IsVehiclePartner = user.IsVehiclePartner;
                values.TokenVersion = user.TokenVersion;
            }
            return values;
        }

        private static string BuildLockoutMessage(DateTime lockoutEndUtc, bool automated)
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh");
            var localEnd = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(lockoutEndUtc, DateTimeKind.Utc), zone);
            return automated
                ? $"Bạn đã đăng nhập không thành công 5 lần. Tài khoản tạm khóa trong 15 phút, đến {localEnd:HH:mm dd/MM/yyyy}. Vui lòng thử lại sau hoặc sử dụng chức năng Quên mật khẩu."
                : $"Tài khoản đang bị khóa tạm thời đến {localEnd:HH:mm dd/MM/yyyy}. Vui lòng thử lại sau hoặc liên hệ quản trị viên.";
        }
    }
}
