using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.Entities;
using SmartCar.Dto.AdminAccountDtos;
using SmartCar.Persistence.Context;
using System.Security.Claims;

namespace SmartCar.WebApi.Controllers
{
    [ApiController]
    [Route("api/admin-accounts")]
    [Authorize(Roles = "Admin")]
    public class AdminAccountsController : ControllerBase
    {
        private static readonly string[] TerminalStatuses = { "Đã hủy", "Hoàn thành", "Bị từ chối", "Hết hạn thanh toán", "Hết hạn chủ xe xác nhận" };
        private static readonly string[] HoldBeforeHandoverStatuses = { "Chờ chủ xe xác nhận", "Chờ thanh toán", "Chờ khách đặt cọc", "Chờ khách thanh toán giữ chỗ", "Chờ nhân viên xác nhận thanh toán", "Đã xác nhận", "Chờ giao xe" };
        private readonly CarBookContext _context;
        public AdminAccountsController(CarBookContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? type,
            [FromQuery] string? search,
            [FromQuery] string? province,
            [FromQuery] string? ward,
            [FromQuery] string? gender,
            [FromQuery] string? verificationStatus,
            [FromQuery] string? accountStatus,
            [FromQuery] int? minAge,
            [FromQuery] int? maxAge)
        {
            var selectedType = string.IsNullOrWhiteSpace(type) ? "Khách hàng" : type.Trim();
            var users = await _context.AppUsers.IgnoreQueryFilters().AsNoTracking().Include(x => x.AppRole).ToListAsync();
            var customerIds = users.Where(x => AccountType(x) == "Khách hàng").Select(x => x.AppUserId).ToList();
            var verifications = await _context.UserVerifications.AsNoTracking()
                .Where(x => customerIds.Contains(x.AppUserID) && x.VerificationType == "Khách thuê")
                .ToDictionaryAsync(x => x.AppUserID, x => x);
            var violations = await _context.FraudFlags.AsNoTracking().Where(x => x.AppUserID.HasValue)
                .GroupBy(x => x.AppUserID!.Value)
                .Select(x => new { ID = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.ID, x => x.Count);
            var openCustomer = await _context.Reservations.AsNoTracking()
                .Where(x => !TerminalStatuses.Contains(x.Status))
                .GroupBy(x => x.CustomerAppUserID)
                .Select(x => new { ID = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.ID, x => x.Count);
            var openOwner = await _context.Reservations.AsNoTracking()
                .Where(x => !TerminalStatuses.Contains(x.Status))
                .GroupBy(x => x.PartnerVehicle.OwnerAppUserID)
                .Select(x => new { ID = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.ID, x => x.Count);

            var filtered = users.Where(x => AccountType(x) == selectedType);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim().ToLower();
                filtered = filtered.Where(x => x.Username.ToLower().Contains(keyword)
                    || x.Email.ToLower().Contains(keyword)
                    || (x.Surname + " " + x.Name).ToLower().Contains(keyword)
                    || (x.Phone ?? string.Empty).Contains(keyword));
            }

            var items = filtered.Select(x => ToItem(x, verifications.GetValueOrDefault(x.AppUserId), violations.GetValueOrDefault(x.AppUserId), AccountType(x) == "Chủ xe" ? openOwner.GetValueOrDefault(x.AppUserId) : openCustomer.GetValueOrDefault(x.AppUserId))).ToList();

            if (!string.IsNullOrWhiteSpace(province))
                items = items.Where(x => string.Equals(x.Province, province.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            if (!string.IsNullOrWhiteSpace(ward))
                items = items.Where(x => string.Equals(x.Ward, ward.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            if (!string.IsNullOrWhiteSpace(gender))
                items = items.Where(x => string.Equals(x.Gender, gender.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            if (!string.IsNullOrWhiteSpace(verificationStatus))
                items = items.Where(x => string.Equals(x.VerificationStatus, verificationStatus.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            if (minAge.HasValue)
                items = items.Where(x => x.Age.HasValue && x.Age.Value >= minAge.Value).ToList();
            if (maxAge.HasValue)
                items = items.Where(x => x.Age.HasValue && x.Age.Value <= maxAge.Value).ToList();
            if (!string.IsNullOrWhiteSpace(accountStatus))
            {
                var now = DateTime.UtcNow;
                items = accountStatus.Trim() switch
                {
                    "Đang hoạt động" => items.Where(x => x.IsActive && !x.IsDeleted && (!x.LockoutEnd.HasValue || x.LockoutEnd <= now)).ToList(),
                    "Khóa tạm thời" => items.Where(x => x.IsActive && x.LockType != "Khóa vĩnh viễn" && x.LockoutEnd.HasValue && x.LockoutEnd > now).ToList(),
                    "Khóa vĩnh viễn" => items.Where(x => !x.IsActive && x.LockType == "Khóa vĩnh viễn").ToList(),
                    "Đã xóa mềm" => items.Where(x => x.IsDeleted).ToList(),
                    "Chưa xác minh email" => items.Where(x => x.VerificationStatus == "Chưa xác minh email").ToList(),
                    _ => items
                };
            }

            items = items.OrderByDescending(x => x.LastLoginAt).ThenBy(x => x.AppUserID).Take(300).ToList();

            return Ok(new AdminAccountListDto
            {
                SelectedType = selectedType,
                CustomerCount = users.Count(x => AccountType(x) == "Khách hàng"),
                PartnerCount = users.Count(x => AccountType(x) == "Chủ xe"),
                StaffCount = users.Count(x => AccountType(x) == "Nhân viên"),
                AdminCount = users.Count(x => AccountType(x) == "Admin"),
                Accounts = items
            });
        }

        [HttpGet("{id:int}/detail")]
        public async Task<IActionResult> Detail(int id)
        {
            var user = await _context.AppUsers.IgnoreQueryFilters().AsNoTracking().Include(x => x.AppRole).FirstOrDefaultAsync(x => x.AppUserId == id);
            if (user is null) return NotFound("Không tìm thấy tài khoản.");
            var verification = await _context.UserVerifications.AsNoTracking().FirstOrDefaultAsync(x => x.AppUserID == id && x.VerificationType == "Khách thuê");
            var violations = await _context.FraudFlags.AsNoTracking().CountAsync(x => x.AppUserID == id);
            var openCount = await _context.Reservations.AsNoTracking().CountAsync(x => x.CustomerAppUserID == id && !TerminalStatuses.Contains(x.Status));
            var reviewerIds = verification?.ReviewedByAppUserID is int reviewerId ? new[] { reviewerId }.ToList() : new List<int>();
            var reviewerUsers = reviewerIds.Count == 0
                ? new List<AppUser>()
                : await _context.AppUsers.IgnoreQueryFilters().AsNoTracking().Where(x => reviewerIds.Contains(x.AppUserId)).ToListAsync();
            var reviewerNames = reviewerUsers.ToDictionary(x => x.AppUserId, DisplayName);

            var reservations = await _context.Reservations.AsNoTracking()
                .Where(x => x.CustomerAppUserID == id)
                .Include(x => x.Car).ThenInclude(x => x.Brand)
                .OrderByDescending(x => x.CreatedDate)
                .Take(20)
                .Select(x => new AdminReservationSummaryDto
                {
                    ReservationID = x.ReservationID,
                    CarName = x.Car.Brand.Name + " " + x.Car.Model,
                    Status = x.Status,
                    PickUpDate = x.PickUpDate,
                    DropOffDate = x.DropOffDate,
                    TotalPrice = x.TotalPrice,
                    CreatedDate = x.CreatedDate
                }).ToListAsync();

            var entityIds = new List<string> { id.ToString() };
            if (verification != null) entityIds.Add(verification.UserVerificationID.ToString());
            var rawLogs = await _context.AuditLogs.AsNoTracking()
                .Where(x => (x.EntityName == nameof(AppUser) && x.EntityID == id.ToString())
                         || (verification != null && x.EntityName == nameof(UserVerification) && x.EntityID == verification.UserVerificationID.ToString())
                         || x.AppUserID == id)
                .OrderByDescending(x => x.CreatedDate)
                .Take(80)
                .ToListAsync();
            var actorIds = rawLogs.Where(x => x.AppUserID.HasValue).Select(x => x.AppUserID!.Value).Distinct().ToList();
            var actorUsers = actorIds.Count == 0 ? new List<AppUser>() : await _context.AppUsers.IgnoreQueryFilters().AsNoTracking().Where(x => actorIds.Contains(x.AppUserId)).ToListAsync();
            var actors = actorUsers.ToDictionary(x => x.AppUserId, DisplayName);

            var staffIssues = await _context.StaffOperationalIssues.AsNoTracking()
                .Where(x => x.CustomerAppUserID == id || (verification != null && x.UserVerificationID == verification.UserVerificationID))
                .OrderByDescending(x => x.CreatedDate)
                .Take(20)
                .ToListAsync();
            var staffIds = staffIssues.Select(x => x.StaffAppUserID).Distinct().ToList();
            var staffUsers = staffIds.Count == 0 ? new List<AppUser>() : await _context.AppUsers.IgnoreQueryFilters().AsNoTracking().Where(x => staffIds.Contains(x.AppUserId)).ToListAsync();
            var staffNames = staffUsers.ToDictionary(x => x.AppUserId, DisplayName);

            return Ok(new AdminAccountDetailDto
            {
                Account = ToItem(user, verification, violations, openCount),
                Verification = verification == null ? null : new AdminCustomerVerificationDetailDto
                {
                    UserVerificationID = verification.UserVerificationID,
                    Status = verification.Status,
                    LegalFullName = verification.LegalFullName,
                    Gender = verification.Gender,
                    DateOfBirth = verification.DateOfBirth,
                    Age = Age(verification.DateOfBirth),
                    CitizenIdMasked = verification.CitizenIdMasked,
                    CitizenIdIssuedDate = verification.CitizenIdIssuedDate,
                    CitizenIdExpiryDate = verification.CitizenIdExpiryDate,
                    CitizenIdAddress = verification.CitizenIdAddress,
                    PermanentProvince = verification.PermanentProvince,
                    PermanentWard = verification.PermanentWard,
                    PermanentDetail = verification.PermanentDetail,
                    PermanentAddress = verification.PermanentAddress,
                    CurrentAddressSameAsPermanent = verification.CurrentAddressSameAsPermanent,
                    CurrentProvince = verification.CurrentProvince,
                    CurrentWard = verification.CurrentWard,
                    CurrentDetail = verification.CurrentDetail,
                    CurrentAddress = verification.CurrentAddress,
                    DriverLicenseNumber = verification.DriverLicenseNumber,
                    DriverLicenseClass = verification.DriverLicenseClass,
                    DriverLicenseIssuedDate = verification.DriverLicenseIssuedDate,
                    DriverLicenseExpiry = verification.DriverLicenseExpiry,
                    CitizenIdFrontUrl = SecureViewUrl(verification.CitizenIdFrontFileID, verification.CitizenIdFrontUrl),
                    CitizenIdBackUrl = SecureViewUrl(verification.CitizenIdBackFileID, verification.CitizenIdBackUrl),
                    DriverLicenseUrl = SecureViewUrl(verification.DriverLicenseFileID, verification.DriverLicenseUrl),
                    PortraitUrl = SecureViewUrl(verification.PortraitFileID, verification.PortraitUrl),
                    ReviewedByAppUserID = verification.ReviewedByAppUserID,
                    ReviewedByName = verification.ReviewedByAppUserID.HasValue ? reviewerNames.GetValueOrDefault(verification.ReviewedByAppUserID.Value) : null,
                    CreatedDate = verification.CreatedDate,
                    ReviewedDate = verification.ReviewedDate,
                    RejectionReason = verification.RejectionReason
                },
                Reservations = reservations,
                AuditLogs = rawLogs.Select(x => new AdminAuditLogDto
                {
                    AuditLogID = x.AuditLogID,
                    ActorName = x.AppUserID.HasValue ? actors.GetValueOrDefault(x.AppUserID.Value, "Không rõ") : "Hệ thống",
                    Action = x.Action,
                    EntityName = x.EntityName,
                    EntityID = x.EntityID,
                    Note = x.Note,
                    IpAddress = x.IpAddress,
                    CreatedDate = x.CreatedDate
                }).ToList(),
                StaffIssues = staffIssues.Select(x => new AdminStaffIssueDto
                {
                    StaffOperationalIssueID = x.StaffOperationalIssueID,
                    StaffName = staffNames.GetValueOrDefault(x.StaffAppUserID, "Nhân viên không rõ"),
                    IssueType = x.IssueType,
                    Severity = x.Severity,
                    Reason = x.Reason,
                    Status = x.Status,
                    CreatedDate = x.CreatedDate
                }).ToList()
            });
        }

        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, AdminAccountStatusDto dto)
        {
            if (id == CurrentUserId()) return BadRequest("Không thể tự khóa hoặc ngừng tài khoản đang đăng nhập.");
            if (string.IsNullOrWhiteSpace(dto.Reason)) return BadRequest("Phải nhập lý do thay đổi trạng thái tài khoản.");
            var user = await _context.AppUsers.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.AppUserId == id);
            if (user is null) return NotFound("Không tìm thấy tài khoản.");
            var adminId = CurrentUserId();
            if (dto.IsActive)
            {
                user.IsActive = true;
                user.FailedLoginCount = 0;
                user.LockoutEnd = null;
                user.LockType = null;
                user.LockReason = null;
                user.LockedAt = null;
                user.LockedByAppUserID = null;
                user.TokenVersion++;
                AddAudit(adminId, "Mở khóa tài khoản", nameof(AppUser), id.ToString(), (dto.Reason ?? string.Empty).Trim());
                await _context.SaveChangesAsync();
                return Ok("Đã mở khóa tài khoản.");
            }

            var lockMode = dto.LockMode == "Temporary" ? "Khóa tạm thời" : "Khóa vĩnh viễn";
            var days = Math.Clamp(dto.LockDays ?? 7, 1, 365);
            user.IsActive = dto.LockMode == "Temporary";
            user.LockType = lockMode;
            user.LockReason = (dto.Reason ?? string.Empty).Trim();
            user.LockedAt = DateTime.UtcNow;
            user.LockedByAppUserID = adminId;
            user.LockoutEnd = dto.LockMode == "Temporary" ? DateTime.UtcNow.AddDays(days) : DateTime.UtcNow.AddYears(10);
            user.TokenVersion++;
            AddAudit(adminId, lockMode, nameof(AppUser), id.ToString(), dto.LockMode == "Temporary" ? $"{(dto.Reason ?? string.Empty).Trim()} | Thời hạn: {days} ngày" : (dto.Reason ?? string.Empty).Trim());
            await _context.SaveChangesAsync();
            return Ok(dto.LockMode == "Temporary" ? $"Đã khóa tạm thời tài khoản trong {days} ngày." : "Đã khóa vĩnh viễn tài khoản.");
        }

        [HttpPost("{id:int}/revoke-sessions")]
        public async Task<IActionResult> RevokeSessions(int id, [FromBody] AdminRevokeSessionDto dto)
        {
            if (id == CurrentUserId()) return BadRequest("Không thể tự đăng xuất toàn bộ thiết bị trong màn hình này; hãy dùng chức năng đăng xuất.");
            if (string.IsNullOrWhiteSpace(dto.Reason)) return BadRequest("Phải nhập lý do đăng xuất khỏi tất cả thiết bị.");
            var user = await _context.AppUsers.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.AppUserId == id);
            if (user is null) return NotFound("Không tìm thấy tài khoản.");
            user.TokenVersion++;
            AddAudit(CurrentUserId(), "Đăng xuất khỏi tất cả thiết bị", nameof(AppUser), id.ToString(), (dto.Reason ?? string.Empty).Trim());
            await _context.SaveChangesAsync();
            return Ok("Đã đăng xuất tài khoản khỏi tất cả thiết bị đang đăng nhập.");
        }

        [HttpPost("{id:int}/request-reverification")]
        public async Task<IActionResult> RequestReverification(int id, [FromBody] AdminRequestReverificationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Reason)) return BadRequest("Phải nhập lý do yêu cầu xác minh lại.");
            var user = await _context.AppUsers.Include(x => x.AppRole).FirstOrDefaultAsync(x => x.AppUserId == id);
            if (user is null) return NotFound("Không tìm thấy tài khoản.");
            var verification = await _context.UserVerifications.FirstOrDefaultAsync(x => x.AppUserID == id && x.VerificationType == "Khách thuê");
            if (verification is null) return BadRequest("Khách hàng chưa có hồ sơ xác minh để thu hồi kết quả duyệt.");
            if (verification.Status != "Đã xác minh") return BadRequest("Chỉ thu hồi kết quả duyệt đối với hồ sơ đang ở trạng thái Đã xác minh.");

            var adminId = CurrentUserId();
            var previousReviewerId = verification.ReviewedByAppUserID;
            await using var transaction = await _context.Database.BeginTransactionAsync();

            verification.Status = "Cần xác minh lại";
            verification.RejectionReason = (dto.Reason ?? string.Empty).Trim();
            verification.ReviewedByAppUserID = adminId;
            verification.ReviewedDate = DateTime.UtcNow;

            if (dto.RestrictBooking)
            {
                user.BookingRestrictedUntil = DateTime.UtcNow.AddYears(10);
                user.BookingRestrictionReason = (dto.Reason ?? string.Empty).Trim();
            }

            _context.Notifications.Add(new Notification
            {
                AppUserID = id,
                Title = "Hồ sơ cần xác minh lại",
                Message = $"Lý do: {(dto.Reason ?? string.Empty).Trim()}. Trong thời gian xác minh lại, bạn chưa thể tạo đơn thuê mới. Vui lòng cập nhật hồ sơ và gửi lại.",
                Type = "Verification",
                Link = "/Verification"
            });

            if (previousReviewerId.HasValue && previousReviewerId.Value != 0)
            {
                _context.StaffOperationalIssues.Add(new StaffOperationalIssue
                {
                    StaffAppUserID = previousReviewerId.Value,
                    AdminAppUserID = adminId,
                    CustomerAppUserID = id,
                    UserVerificationID = verification.UserVerificationID,
                    IssueType = "Duyệt hồ sơ chưa đạt yêu cầu",
                    Severity = string.IsNullOrWhiteSpace(dto.Severity) ? "Trung bình" : (dto.Severity ?? string.Empty).Trim(),
                    Reason = (dto.Reason ?? string.Empty).Trim(),
                    Status = "Mới"
                });
            }

            var beforeHandover = await _context.Reservations.Where(x => x.CustomerAppUserID == id && HoldBeforeHandoverStatuses.Contains(x.Status)).ToListAsync();
            foreach (var reservation in beforeHandover)
            {
                var oldStatus = reservation.Status;
                reservation.Status = "Tạm giữ do hồ sơ cần xác minh lại";
                _context.ReservationStatusHistories.Add(new ReservationStatusHistory
                {
                    ReservationID = reservation.ReservationID,
                    OldStatus = oldStatus,
                    NewStatus = reservation.Status,
                    ChangedByAppUserID = adminId,
                    Note = (dto.Reason ?? string.Empty).Trim()
                });
            }

            var activeRentals = await _context.Reservations.Where(x => x.CustomerAppUserID == id && x.Status == "Đang thuê").ToListAsync();
            foreach (var reservation in activeRentals)
            {
                _context.FraudFlags.Add(new FraudFlag
                {
                    AppUserID = id,
                    ReservationID = reservation.ReservationID,
                    RuleCode = "HO_SO_CAN_XAC_MINH_LAI",
                    Description = "Hồ sơ khách cần kiểm tra sau chuyến thuê; không cho tạo đơn mới.",
                    RiskScore = 50,
                    Status = "Mới"
                });
            }

            var oldClaims = await _context.WorkItemClaims.Where(x => x.QueueType == "Xác minh khách" && x.EntityID == verification.UserVerificationID).ToListAsync();
            foreach (var claim in oldClaims)
            {
                claim.Status = "Đã nhả";
                claim.DueAt = null;
                claim.AssignedAt = DateTime.UtcNow;
            }

            AddAudit(adminId, "Thu hồi kết quả duyệt hồ sơ", nameof(UserVerification), verification.UserVerificationID.ToString(), $"Khách #{id}; lý do: {(dto.Reason ?? string.Empty).Trim()}; tạm hạn chế đặt xe: {(dto.RestrictBooking ? "Có" : "Không")}; nhân viên duyệt trước: {(previousReviewerId?.ToString() ?? "Không rõ")}");
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok("Đã chuyển hồ sơ sang Cần xác minh lại, thông báo cho khách và ghi audit log.");
        }

        private void AddAudit(int adminId, string action, string entityName, string entityId, string note)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                AppUserID = adminId,
                Action = action,
                EntityName = entityName,
                EntityID = entityId,
                Note = note,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
        }

        private static string AccountType(AppUser x) => x.AppRole.AppRoleName switch
        {
            "Admin" => "Admin",
            "Staff" => "Nhân viên",
            _ when x.IsVehiclePartner => "Chủ xe",
            _ => "Khách hàng"
        };

        private static string DisplayName(AppUser x)
        {
            var name = $"{x.Surname} {x.Name}".Trim();
            return string.IsNullOrWhiteSpace(name) ? x.Username : name;
        }

        private static int? Age(DateTime? dob)
        {
            if (!dob.HasValue) return null;
            var today = SmartCar.Domain.Time.VietnamTime.Today;
            var age = today.Year - dob.Value.Year;
            if (dob.Value.Date > today.AddYears(-age)) age--;
            return age;
        }

        private AdminAccountItemDto ToItem(AppUser user, UserVerification? verification, int violations, int openCount)
        {
            var accountType = AccountType(user);
            var status = accountType == "Khách hàng"
                ? (user.EmailConfirmed ? verification?.Status ?? "Chưa xác minh" : "Chưa xác minh email")
                : "Không áp dụng";
            return new AdminAccountItemDto
            {
                AppUserID = user.AppUserId,
                Username = user.Username,
                FullName = DisplayName(user),
                Email = user.Email,
                Phone = user.Phone,
                AccountType = accountType,
                IsActive = user.IsActive,
                IsDeleted = user.IsDeleted,
                LockType = user.LockType,
                LockReason = user.LockReason,
                LockoutEnd = user.LockoutEnd,
                LastLoginAt = user.LastLoginAt,
                VerificationStatus = status,
                ViolationCount = violations,
                OpenReservationCount = openCount,
                Province = verification?.CurrentProvince ?? verification?.PermanentProvince,
                Ward = verification?.CurrentWard ?? verification?.PermanentWard,
                Gender = verification?.Gender,
                Age = Age(verification?.DateOfBirth)
            };
        }

        private static string SecureViewUrl(Guid? fileId, string? legacyUrl)
            => fileId.HasValue ? $"/SecureFiles/View/{fileId.Value}" : legacyUrl ?? string.Empty;

        private int CurrentUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    }
}
