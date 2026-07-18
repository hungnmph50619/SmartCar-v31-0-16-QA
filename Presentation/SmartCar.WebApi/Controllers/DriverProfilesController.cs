using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.BusinessRules;
using SmartCar.Domain.Entities;
using SmartCar.Domain.Security;
using SmartCar.Domain.Time;
using SmartCar.Persistence.Context;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SmartCar.WebApi.Controllers
{
    [ApiController]
    [Route("api/driver-profiles")]
    public class DriverProfilesController : ControllerBase
    {
        private readonly CarBookContext _context;
        private readonly IConfiguration _configuration;

        public DriverProfilesController(CarBookContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpGet("mine")]
        public async Task<IActionResult> Mine()
        {
            var userId = CurrentUserId();
            var data = await _context.DriverProfiles.AsNoTracking()
                .Where(x => x.PartnerAppUserID == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
            return Ok(data);
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateDriverProfileRequest request)
        {
            var userId = CurrentUserId();
            var partnerVerified = await _context.VehiclePartnerProfiles.AsNoTracking()
                .AnyAsync(x => x.AppUserID == userId && x.Status == VerificationStatuses.Verified);
            if (!partnerVerified) return BadRequest("Đối tác phải được xác minh trước khi gửi hồ sơ tài xế lên duyệt.");

            request.Normalize();
            var validation = request.ValidateForSubmit();
            if (validation is not null) return BadRequest(validation);
            if (await _context.DriverProfiles.AnyAsync(x => x.DriverLicenseNumber == request.DriverLicenseNumber))
                return Conflict("Số GPLX đã tồn tại trong hệ thống.");
            var citizenFingerprint = IdentityFingerprintSecurity.Compute(IdentityKey(), request.CitizenIdentityNumber);
            if (await _context.DriverProfiles.AnyAsync(x => x.CitizenIdFingerprint == citizenFingerprint))
                return Conflict("CCCD đã được sử dụng cho một hồ sơ tài xế khác.");

            var entity = new DriverProfile
            {
                PartnerAppUserID = userId,
                FullName = request.FullName,
                Phone = request.Phone,
                CitizenIdentityNumber = request.CitizenIdentityNumber,
                CitizenIdFingerprint = citizenFingerprint,
                DriverLicenseNumber = request.DriverLicenseNumber,
                DriverLicenseClass = request.DriverLicenseClass,
                DriverLicenseIssuedDate = request.DriverLicenseIssuedDate.Date,
                DriverLicenseExpiryDate = request.DriverLicenseExpiryDate.Date,
                RelationshipType = request.RelationshipType,
                CitizenIdFrontFileID = request.CitizenIdFrontFileID,
                CitizenIdBackFileID = request.CitizenIdBackFileID,
                PortraitFileID = request.PortraitFileID,
                DriverLicenseFileID = request.DriverLicenseFileID,
                Status = VehicleApprovalStatuses.PendingReview,
                SubmittedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _context.DriverProfiles.Add(entity);
            AddAudit(userId, "Gửi hồ sơ tài xế", nameof(DriverProfile), null, $"GPLX={entity.DriverLicenseNumber}");
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateDraft(int id, CreateDriverProfileRequest request)
        {
            var userId = CurrentUserId();
            var entity = await _context.DriverProfiles.FirstOrDefaultAsync(x => x.DriverProfileID == id && x.PartnerAppUserID == userId);
            if (entity is null) return NotFound("Không tìm thấy hồ sơ tài xế.");
            if (entity.Status is not (VehicleApprovalStatuses.Draft or VehicleApprovalStatuses.NotApproved))
                return Conflict("Chỉ hồ sơ bản nháp hoặc chưa đạt mới được sửa.");
            if (!entity.CanResubmit) return Conflict("Hồ sơ này không được phép gửi lại.");

            request.Normalize();
            var validation = request.ValidateForSubmit();
            if (validation is not null) return BadRequest(validation);
            if (await _context.DriverProfiles.AnyAsync(x => x.DriverProfileID != id && x.DriverLicenseNumber == request.DriverLicenseNumber))
                return Conflict("Số GPLX đã tồn tại trong hệ thống.");
            var citizenFingerprint = IdentityFingerprintSecurity.Compute(IdentityKey(), request.CitizenIdentityNumber);
            if (await _context.DriverProfiles.AnyAsync(x => x.DriverProfileID != id && x.CitizenIdFingerprint == citizenFingerprint))
                return Conflict("CCCD đã được sử dụng cho một hồ sơ tài xế khác.");

            var old = $"Status={entity.Status}; GPLX={entity.DriverLicenseNumber}";
            entity.FullName = request.FullName;
            entity.Phone = request.Phone;
            entity.CitizenIdentityNumber = request.CitizenIdentityNumber;
            entity.CitizenIdFingerprint = citizenFingerprint;
            entity.DriverLicenseNumber = request.DriverLicenseNumber;
            entity.DriverLicenseClass = request.DriverLicenseClass;
            entity.DriverLicenseIssuedDate = request.DriverLicenseIssuedDate.Date;
            entity.DriverLicenseExpiryDate = request.DriverLicenseExpiryDate.Date;
            entity.RelationshipType = request.RelationshipType;
            entity.CitizenIdFrontFileID = request.CitizenIdFrontFileID;
            entity.CitizenIdBackFileID = request.CitizenIdBackFileID;
            entity.PortraitFileID = request.PortraitFileID;
            entity.DriverLicenseFileID = request.DriverLicenseFileID;
            entity.Status = VehicleApprovalStatuses.PendingReview;
            entity.SubmittedAt = DateTime.UtcNow;
            entity.ReviewReason = null;
            AddAudit(userId, "Cập nhật và gửi lại hồ sơ tài xế", nameof(DriverProfile), id.ToString(), old);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet("pending")]
        public async Task<IActionResult> Pending()
            => Ok(await _context.DriverProfiles.AsNoTracking()
                .Include(x => x.PartnerAppUser)
                .Where(x => x.Status == VehicleApprovalStatuses.PendingReview)
                .OrderBy(x => x.SubmittedAt)
                .ToListAsync());

        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("{id:int}/review")]
        public async Task<IActionResult> Review(int id, ReviewDriverProfileRequest request)
        {
            var reviewerId = CurrentUserId();
            var entity = await _context.DriverProfiles.FirstOrDefaultAsync(x => x.DriverProfileID == id);
            if (entity is null) return NotFound("Không tìm thấy hồ sơ tài xế.");
            if (entity.Status != VehicleApprovalStatuses.PendingReview) return Conflict("Hồ sơ không ở trạng thái chờ duyệt.");

            var decision = (request.Decision ?? string.Empty).Trim();
            if (decision is not (VehicleApprovalStatuses.Approved or VehicleApprovalStatuses.NotApproved or VehicleOperationStatuses.Locked))
                return BadRequest("Kết quả phải là Đã duyệt, Chưa đạt hoặc Bị khóa.");
            if (decision != VehicleApprovalStatuses.Approved && string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest("Bắt buộc nhập lý do khi hồ sơ chưa đạt hoặc bị khóa.");

            entity.Status = decision;
            entity.ReviewReason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
            entity.CanResubmit = decision == VehicleApprovalStatuses.NotApproved && request.CanResubmit;
            entity.ReviewedByAppUserID = reviewerId;
            entity.ReviewedAt = DateTime.UtcNow;
            AddAudit(reviewerId, "Duyệt hồ sơ tài xế", nameof(DriverProfile), id.ToString(), $"Decision={decision}; Reason={entity.ReviewReason}");
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpPost("assign")]
        public async Task<IActionResult> Assign(AssignDriverRequest request)
        {
            var partnerId = CurrentUserId();
            var reservation = await _context.Reservations
                .Include(x => x.PartnerVehicle)
                .FirstOrDefaultAsync(x => x.ReservationID == request.ReservationID);
            if (reservation is null) return NotFound("Không tìm thấy đơn.");
            if (reservation.PartnerVehicle.OwnerAppUserID != partnerId) return Forbid();
            if (reservation.RentalMode != ServiceTypes.WithDriver) return BadRequest("Chỉ đơn có tài xế mới được phân công tài xế.");
            if (reservation.Status is ReservationStatuses.Cancelled or ReservationStatuses.Completed)
                return Conflict("Đơn đã kết thúc hoặc đã hủy.");

            var driver = await _context.DriverProfiles.FirstOrDefaultAsync(x => x.DriverProfileID == request.DriverProfileID && x.PartnerAppUserID == partnerId);
            if (driver is null) return NotFound("Không tìm thấy tài xế thuộc đối tác.");
            if (driver.Status != VehicleApprovalStatuses.Approved) return BadRequest("Tài xế chưa được duyệt.");

            var startLocal = reservation.PickUpDate.Date.Add(reservation.PickUpTime);
            var endLocal = reservation.DropOffDate.Date.Add(reservation.DropOffTime);
            var startUtc = VietnamTime.LocalToUtc(startLocal);
            var endUtc = VietnamTime.LocalToUtc(endLocal);
            if (driver.DriverLicenseExpiryDate.Date < endLocal.Date) return BadRequest("GPLX tài xế không còn hạn đến hết chuyến.");

            var overlaps = await _context.BookingDriverAssignments.AsNoTracking().AnyAsync(x =>
                x.DriverProfileID == driver.DriverProfileID && x.Status == "Active" &&
                x.AssignmentStartUtc < endUtc && x.AssignmentEndUtc > startUtc);
            if (overlaps) return Conflict("Tài xế đang được phân công cho chuyến khác trùng thời gian.");

            var oldAssignments = await _context.BookingDriverAssignments
                .Where(x => x.ReservationID == reservation.ReservationID && x.Status == "Active")
                .ToListAsync();
            foreach (var old in oldAssignments)
            {
                old.Status = "Replaced";
                old.EndedAt = DateTime.UtcNow;
                old.ChangeReason = string.IsNullOrWhiteSpace(request.ChangeReason) ? "Đổi tài xế" : request.ChangeReason.Trim();
            }

            var assignment = new BookingDriverAssignment
            {
                ReservationID = reservation.ReservationID,
                DriverProfileID = driver.DriverProfileID,
                AssignedByAppUserID = partnerId,
                AssignmentStartUtc = startUtc,
                AssignmentEndUtc = endUtc,
                ChangeReason = request.ChangeReason,
                Status = "Active",
                IsPrimary = true,
                AssignedAt = DateTime.UtcNow
            };
            _context.BookingDriverAssignments.Add(assignment);
            _context.Notifications.Add(new Notification
            {
                AppUserID = reservation.CustomerAppUserID,
                Title = "Đã phân công tài xế",
                Message = $"Tài xế {driver.FullName} đã được phân công cho đơn #{reservation.ReservationID}.",
                CreatedDate = DateTime.UtcNow
            });
            AddAudit(partnerId, "Phân công tài xế", nameof(Reservation), reservation.ReservationID.ToString(), $"DriverProfileID={driver.DriverProfileID}");
            await _context.SaveChangesAsync();
            return Ok(new { assignment.BookingDriverAssignmentID, driver.DriverProfileID, driver.FullName, driver.Phone });
        }

        private int CurrentUserId()
            => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        private string IdentityKey()
            => _configuration["Security:IdentityHmacKey"]
               ?? throw new InvalidOperationException("Thiếu Security:IdentityHmacKey.");

        private void AddAudit(int userId, string action, string entityName, string? entityId, string? note)
            => _context.AuditLogs.Add(new AuditLog
            {
                AppUserID = userId,
                Action = action,
                EntityName = entityName,
                EntityID = entityId,
                Note = note,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedDate = DateTime.UtcNow
            });
    }

    public sealed class CreateDriverProfileRequest
    {
        [Required, MaxLength(150)] public string FullName { get; set; } = string.Empty;
        [Required, MaxLength(20)] public string Phone { get; set; } = string.Empty;
        [Required, RegularExpression(@"^\d{12}$")] public string CitizenIdentityNumber { get; set; } = string.Empty;
        [Required, MaxLength(50)] public string DriverLicenseNumber { get; set; } = string.Empty;
        [Required, MaxLength(20)] public string DriverLicenseClass { get; set; } = string.Empty;
        public DateTime DriverLicenseIssuedDate { get; set; }
        public DateTime DriverLicenseExpiryDate { get; set; }
        [Required, MaxLength(50)] public string RelationshipType { get; set; } = "Nhân viên";
        public Guid? CitizenIdFrontFileID { get; set; }
        public Guid? CitizenIdBackFileID { get; set; }
        public Guid? PortraitFileID { get; set; }
        public Guid? DriverLicenseFileID { get; set; }

        public void Normalize()
        {
            FullName = (FullName ?? string.Empty).Trim();
            Phone = (Phone ?? string.Empty).Trim().Replace(" ", string.Empty).Replace("-", string.Empty);
            CitizenIdentityNumber = (CitizenIdentityNumber ?? string.Empty).Trim();
            DriverLicenseNumber = (DriverLicenseNumber ?? string.Empty).Trim().ToUpperInvariant();
            DriverLicenseClass = (DriverLicenseClass ?? string.Empty).Trim().ToUpperInvariant();
            RelationshipType = (RelationshipType ?? string.Empty).Trim();
        }

        public string? ValidateForSubmit()
        {
            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Phone)) return "Họ tên và số điện thoại không được để trống.";
            if (!System.Text.RegularExpressions.Regex.IsMatch(CitizenIdentityNumber, @"^\d{12}$")) return "CCCD tài xế phải gồm 12 chữ số.";
            if (DriverLicenseIssuedDate == default || DriverLicenseIssuedDate > DateTime.UtcNow.Date) return "Ngày cấp GPLX không hợp lệ.";
            if (DriverLicenseExpiryDate <= DriverLicenseIssuedDate || DriverLicenseExpiryDate < DateTime.UtcNow.Date) return "GPLX tài xế đã hết hạn hoặc ngày hết hạn không hợp lệ.";
            if (!CitizenIdFrontFileID.HasValue || !CitizenIdBackFileID.HasValue || !PortraitFileID.HasValue || !DriverLicenseFileID.HasValue)
                return "Bắt buộc tải đủ CCCD, ảnh chân dung và GPLX tài xế.";
            return null;
        }
    }

    public sealed class ReviewDriverProfileRequest
    {
        [Required] public string Decision { get; set; } = string.Empty;
        [MaxLength(1000)] public string? Reason { get; set; }
        public bool CanResubmit { get; set; } = true;
    }

    public sealed class AssignDriverRequest
    {
        [Range(1, int.MaxValue)] public int ReservationID { get; set; }
        [Range(1, int.MaxValue)] public int DriverProfileID { get; set; }
        [MaxLength(1000)] public string? ChangeReason { get; set; }
    }
}
