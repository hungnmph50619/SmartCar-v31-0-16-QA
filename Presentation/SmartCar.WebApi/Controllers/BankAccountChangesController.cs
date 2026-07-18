using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.BusinessRules;
using SmartCar.Domain.Entities;
using SmartCar.Domain.Security;
using SmartCar.Persistence.Context;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SmartCar.WebApi.Controllers
{
    [ApiController]
    [Route("api/bank-account-changes")]
    public class BankAccountChangesController : ControllerBase
    {
        private readonly CarBookContext _context;
        public BankAccountChangesController(CarBookContext context) => _context = context;

        [Authorize(Roles = "VehiclePartner")]
        [HttpPost]
        public async Task<IActionResult> RequestChange(CreateBankAccountChangeRequest request)
        {
            var userId = CurrentUserId();
            var user = await _context.AppUsers.AsNoTracking().FirstOrDefaultAsync(x => x.AppUserId == userId);
            if (user is null || !PasswordSecurity.Verify(user.Password, request.CurrentPassword ?? string.Empty, out _))
                return BadRequest("Mật khẩu hiện tại không đúng. Yêu cầu đổi tài khoản ngân hàng cần xác nhận bảo mật.");
            var profile = await _context.VehiclePartnerProfiles.FirstOrDefaultAsync(x => x.AppUserID == userId);
            if (profile is null || profile.Status != VerificationStatuses.Verified)
                return BadRequest("Hồ sơ đối tác phải được xác minh trước khi đổi tài khoản ngân hàng.");
            if (await _context.BankAccountChangeRequests.AnyAsync(x => x.VehiclePartnerProfileID == profile.VehiclePartnerProfileID && x.Status == VerificationStatuses.PendingReview))
                return Conflict("Đang có một yêu cầu đổi tài khoản ngân hàng chờ duyệt.");

            request.Normalize();
            if (string.IsNullOrWhiteSpace(request.NewBankName) || string.IsNullOrWhiteSpace(request.NewAccountNumber) || string.IsNullOrWhiteSpace(request.NewAccountHolder))
                return BadRequest("Vui lòng nhập đủ ngân hàng, số tài khoản và tên chủ tài khoản.");
            var expectedHolder = profile.PartnerType == "Cá nhân" ? profile.FullName : profile.BusinessName;
            if (!NamesEquivalent(expectedHolder, request.NewAccountHolder))
                return BadRequest(profile.PartnerType == "Cá nhân"
                    ? "Tên chủ tài khoản mới phải trùng họ tên đối tác trên CCCD."
                    : "Tên chủ tài khoản mới phải đứng tên doanh nghiệp đã xác minh.");

            var entity = new BankAccountChangeRequest
            {
                VehiclePartnerProfileID = profile.VehiclePartnerProfileID,
                OldBankName = profile.BankName,
                OldAccountNumber = profile.BankAccountNumber,
                OldAccountHolder = profile.BankAccountHolder,
                NewBankName = request.NewBankName,
                NewAccountNumber = request.NewAccountNumber,
                NewAccountHolder = request.NewAccountHolder,
                NewBankBranch = request.NewBankBranch,
                Status = VerificationStatuses.PendingReview,
                RequestedByAppUserID = userId,
                RequestedAt = DateTime.UtcNow
            };
            profile.IsPayoutPaused = true;
            profile.PayoutPauseReason = "Đang xác minh tài khoản ngân hàng mới.";
            _context.BankAccountChangeRequests.Add(entity);
            AddAudit(userId, "Yêu cầu đổi tài khoản ngân hàng", nameof(VehiclePartnerProfile), profile.VehiclePartnerProfileID.ToString(), "Tạm dừng chi trả đối soát đến khi tài khoản mới được duyệt.");
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpGet("mine")]
        public async Task<IActionResult> Mine()
        {
            var userId = CurrentUserId();
            return Ok(await _context.BankAccountChangeRequests.AsNoTracking()
                .Where(x => x.RequestedByAppUserID == userId)
                .OrderByDescending(x => x.RequestedAt)
                .ToListAsync());
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet("pending")]
        public async Task<IActionResult> Pending()
            => Ok(await _context.BankAccountChangeRequests.AsNoTracking()
                .Include(x => x.VehiclePartnerProfile)
                .Where(x => x.Status == VerificationStatuses.PendingReview)
                .OrderBy(x => x.RequestedAt)
                .ToListAsync());

        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("{id:int}/review")]
        public async Task<IActionResult> Review(int id, ReviewBankAccountChangeRequest request)
        {
            var reviewerId = CurrentUserId();
            var entity = await _context.BankAccountChangeRequests
                .Include(x => x.VehiclePartnerProfile)
                .FirstOrDefaultAsync(x => x.BankAccountChangeRequestID == id);
            if (entity is null) return NotFound("Không tìm thấy yêu cầu.");
            if (entity.Status != VerificationStatuses.PendingReview) return Conflict("Yêu cầu đã được xử lý.");
            if (request.Approve)
            {
                entity.VehiclePartnerProfile.BankName = entity.NewBankName;
                entity.VehiclePartnerProfile.BankAccountNumber = entity.NewAccountNumber;
                entity.VehiclePartnerProfile.BankAccountHolder = entity.NewAccountHolder;
                entity.VehiclePartnerProfile.BankBranch = entity.NewBankBranch ?? string.Empty;
                entity.VehiclePartnerProfile.IsPayoutPaused = false;
                entity.VehiclePartnerProfile.PayoutPauseReason = null;
                entity.Status = VehicleApprovalStatuses.Approved;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest("Bắt buộc nhập lý do từ chối.");
                entity.Status = VehicleApprovalStatuses.NotApproved;
                entity.VehiclePartnerProfile.IsPayoutPaused = false;
                entity.VehiclePartnerProfile.PayoutPauseReason = null;
            }
            entity.ReviewReason = request.Reason?.Trim();
            entity.ReviewedByAppUserID = reviewerId;
            entity.ReviewedAt = DateTime.UtcNow;
            AddAudit(reviewerId, request.Approve ? "Duyệt đổi tài khoản ngân hàng" : "Từ chối đổi tài khoản ngân hàng", nameof(BankAccountChangeRequest), id.ToString(), entity.ReviewReason);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        private int CurrentUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        private void AddAudit(int userId, string action, string entityName, string? entityId, string? note)
            => _context.AuditLogs.Add(new AuditLog { AppUserID = userId, Action = action, EntityName = entityName, EntityID = entityId, Note = note, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() });
        private static bool NamesEquivalent(string? left, string? right)
        {
            static string Normalize(string? value) => new string((value ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
            return !string.IsNullOrWhiteSpace(left) && Normalize(left) == Normalize(right);
        }
    }

    public sealed class CreateBankAccountChangeRequest
    {
        [Required, MaxLength(200)] public string CurrentPassword { get; set; } = string.Empty;
        [Required, MaxLength(100)] public string NewBankName { get; set; } = string.Empty;
        [Required, MaxLength(50)] public string NewAccountNumber { get; set; } = string.Empty;
        [Required, MaxLength(200)] public string NewAccountHolder { get; set; } = string.Empty;
        [MaxLength(100)] public string? NewBankBranch { get; set; }
        public void Normalize()
        {
            NewBankName = (NewBankName ?? string.Empty).Trim();
            NewAccountNumber = new string((NewAccountNumber ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            NewAccountHolder = (NewAccountHolder ?? string.Empty).Trim().ToUpperInvariant();
            NewBankBranch = NewBankBranch?.Trim();
        }
    }

    public sealed class ReviewBankAccountChangeRequest
    {
        public bool Approve { get; set; }
        [MaxLength(1000)] public string? Reason { get; set; }
    }
}
