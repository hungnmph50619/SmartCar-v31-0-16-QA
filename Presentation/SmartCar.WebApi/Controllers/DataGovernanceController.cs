using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.Entities;
using SmartCar.Domain.Security;
using SmartCar.Persistence.Context;
using SmartCar.WebApi.Services;
using System.Security.Claims;
using System.Text.Json;

namespace SmartCar.WebApi.Controllers
{
    [ApiController]
    [Route("api/data-governance")]
    [Authorize]
    public class DataGovernanceController : ControllerBase
    {
        private readonly CarBookContext _context;
        private readonly IReservationCancellationService _cancellationService;
        private readonly IUserAnonymizationService _anonymizationService;
        private static readonly string[] TerminalReservationStatuses = ["Đã hủy", "Hoàn thành", "Bị từ chối", "Hết hạn thanh toán", "Hết hạn chủ xe xác nhận"];


        public DataGovernanceController(
            CarBookContext context,
            IReservationCancellationService cancellationService,
            IUserAnonymizationService anonymizationService)
        {
            _context = context;
            _cancellationService = cancellationService;
            _anonymizationService = anonymizationService;
        }

        [Authorize(Roles = "Admin,Customer,VehiclePartner")]
        [HttpDelete("cars/{id:int}")]
        public async Task<IActionResult> SoftDeleteCar(int id, DeleteRequest dto)
        {
            var car = await _context.Cars.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.CarID == id);
            if (car is null) return NotFound("Không tìm thấy xe.");
            if (car.IsDeleted) return Conflict("Xe đã được xóa mềm trước đó.");

            var userId = UserId();
            if (!User.IsInRole("Admin"))
            {
                if (!IsVehiclePartnerAccount()) return Forbid();
                var ownsCar = await _context.PartnerVehicles.AnyAsync(x => x.CarID == id && x.OwnerAppUserID == userId);
                if (!ownsCar) return Forbid();
            }

            var hasActiveBooking = await _context.Reservations.AnyAsync(x =>
                x.CarID == id && !TerminalReservationStatuses.Contains(x.Status));
            if (hasActiveBooking) return BadRequest("Không thể xóa xe vì đang có đơn thuê hoặc tranh chấp chưa hoàn tất.");

            var oldData = JsonSerializer.Serialize(car);
            car.IsDeleted = true;
            car.DeletedAt = DateTime.UtcNow;
            car.DeletedByUserId = userId;
            car.DeleteReason = string.IsNullOrWhiteSpace(dto.Reason) ? "Người dùng yêu cầu xóa" : (dto.Reason ?? string.Empty).Trim();
            car.LifecycleStatus = "Đã xóa";

            AddChange("Car", id.ToString(), "SoftDelete", oldData, JsonSerializer.Serialize(car), car.DeleteReason);
            AddAudit("Xóa mềm xe", nameof(Car), id.ToString(), car.DeleteReason);
            await _context.SaveChangesAsync();
            return Ok("Đã xóa mềm xe. Dữ liệu lịch sử vẫn được giữ lại.");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("cars/{id:int}/restore")]
        public async Task<IActionResult> RestoreCar(int id)
        {
            var car = await _context.Cars.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.CarID == id);
            if (car is null) return NotFound("Không tìm thấy xe.");
            if (!car.IsDeleted) return Conflict("Xe chưa bị xóa.");

            var oldData = JsonSerializer.Serialize(car);
            var partnerVehicle = await _context.PartnerVehicles.FirstOrDefaultAsync(x => x.CarID == id);
            car.IsDeleted = false;
            car.DeletedAt = null;
            car.DeletedByUserId = null;
            car.DeleteReason = null;
            car.LifecycleStatus = partnerVehicle?.IsActive == true ? "Đang hoạt động" : "Đã khôi phục - Tạm khóa";

            AddChange("Car", id.ToString(), "Restore", oldData, JsonSerializer.Serialize(car), "Khôi phục bởi quản trị viên");
            AddAudit("Khôi phục xe", nameof(Car), id.ToString(), car.LifecycleStatus);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã khôi phục xe.", status = car.LifecycleStatus });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("cars/{id:int}/permanent")]
        public async Task<IActionResult> HardDeleteCar(int id)
        {
            var car = await _context.Cars.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.CarID == id);
            if (car is null) return NotFound("Không tìm thấy xe.");
            if (!car.IsDeleted) return BadRequest("Chỉ được xóa vĩnh viễn xe đã xóa mềm.");
            if (await _context.Reservations.AnyAsync(x => x.CarID == id)) return BadRequest("Xe đã phát sinh đơn thuê nên không được xóa vĩnh viễn.");
            if (await _context.PartnerVehicles.AnyAsync(x => x.CarID == id)) return BadRequest("Xe đang có hồ sơ đối tác; hãy lưu trữ hoặc xóa quan hệ phụ thuộc trước.");

            _context.ArchivedRecords.Add(new ArchivedRecord
            {
                EntityName = "Car",
                EntityID = id.ToString(),
                DataJson = JsonSerializer.Serialize(car),
                ArchivedByAppUserID = UserId()
            });
            _context.Cars.Remove(car);
            AddAudit("Xóa vĩnh viễn xe", nameof(Car), id.ToString(), "Chỉ áp dụng cho xe chưa phát sinh nghiệp vụ");
            await _context.SaveChangesAsync();
            return Ok("Đã xóa vĩnh viễn xe chưa phát sinh nghiệp vụ và lưu bản sao lưu trữ.");
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("users/{id:int}")]
        public async Task<IActionResult> SoftDeleteUser(int id, DeleteRequest dto)
        {
            var user = await _context.AppUsers.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.AppUserId == id);
            if (user is null) return NotFound("Không tìm thấy tài khoản.");
            if (user.IsDeleted) return Conflict("Tài khoản đã bị xóa mềm.");
            if (id == UserId()) return BadRequest("Không thể tự xóa tài khoản đang đăng nhập.");

            var hasOpenBusiness = await _context.Reservations.AnyAsync(x =>
                    (x.CustomerAppUserID == id || x.PartnerVehicle.OwnerAppUserID == id) &&
                    !TerminalReservationStatuses.Contains(x.Status))
                || await _context.PartnerVehicles.AnyAsync(x => x.OwnerAppUserID == id && x.IsActive)
                || await _context.Disputes.AnyAsync(x => x.CreatedByAppUserID == id && x.Status != "Đã giải quyết" && x.Status != "Đã đóng");
            if (hasOpenBusiness) return BadRequest("Không thể xóa tài khoản vì còn đơn thuê, xe đang hoạt động hoặc khiếu nại chưa xử lý.");

            var oldData = JsonSerializer.Serialize(user);
            user.IsDeleted = true;
            user.IsActive = false;
            user.TokenVersion++;
            user.DeletedAt = DateTime.UtcNow;
            user.DeletedByUserId = UserId();
            user.DeleteReason = string.IsNullOrWhiteSpace(dto.Reason) ? "Quản trị viên ngừng tài khoản" : (dto.Reason ?? string.Empty).Trim();
            AddChange("AppUser", id.ToString(), "SoftDelete", oldData, JsonSerializer.Serialize(user), user.DeleteReason);
            AddAudit("Xóa mềm tài khoản", nameof(AppUser), id.ToString(), user.DeleteReason);
            await _context.SaveChangesAsync();
            return Ok("Đã ngừng hoạt động và ẩn tài khoản.");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("users/{id:int}/anonymize")]
        public async Task<IActionResult> AnonymizeUser(int id, DeleteRequest dto)
        {
            if (id == UserId()) return BadRequest("Không thể tự ẩn danh tài khoản quản trị đang đăng nhập.");
            try
            {
                var result = await _anonymizationService.AnonymizeAsync(
                    id,
                    UserId(),
                    dto.Reason,
                    HttpContext.RequestAborted);
                return Ok(new
                {
                    message = "Đã ẩn danh định danh trực tiếp, xóa tài liệu riêng tư và làm sạch bản sao lịch sử; các khóa giao dịch vẫn được giữ.",
                    result.PrivateFilesQueuedForDeletion,
                    result.VerificationRecordsScrubbed,
                    result.PartnerProfilesScrubbed,
                    result.PartnerApplicationsScrubbed,
                    result.ReservationSnapshotsScrubbed,
                    result.HistoryRecordsRedacted
                });
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("users/{id:int}/restore")]
        public async Task<IActionResult> RestoreUser(int id)
        {
            var user = await _context.AppUsers.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.AppUserId == id);
            if (user is null) return NotFound("Không tìm thấy tài khoản.");
            if (!user.IsDeleted) return Conflict("Tài khoản chưa bị xóa.");
            if (user.AnonymizedAt is not null) return BadRequest("Tài khoản đã ẩn danh nên không thể khôi phục thông tin cũ tự động.");

            user.IsDeleted = false;
            user.IsActive = true;
            user.TokenVersion++;
            user.DeletedAt = null;
            user.DeletedByUserId = null;
            user.DeleteReason = null;
            AddChange("AppUser", id.ToString(), "Restore", null, JsonSerializer.Serialize(user), "Khôi phục tài khoản");
            AddAudit("Khôi phục tài khoản", nameof(AppUser), id.ToString(), null);
            await _context.SaveChangesAsync();
            return Ok("Đã khôi phục tài khoản.");
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("reservations/{id:int}/transition")]
        public async Task<IActionResult> TransitionReservation(int id, TransitionRequest dto)
        {
            var target = dto.NewStatus?.Trim() ?? string.Empty;
            if (target.Equals("Đã hủy", StringComparison.OrdinalIgnoreCase))
            {
                var reason = string.IsNullOrWhiteSpace(dto.Reason)
                    ? "Nhân viên/Quản trị viên hủy đơn."
                    : (dto.Reason ?? string.Empty).Trim();
                var cancellation = await _cancellationService.CancelAsync(id, UserId(), true, reason, HttpContext.RequestAborted);
                return StatusCode(cancellation.StatusCode, cancellation);
            }

            return Conflict(new
            {
                message = "Đã khóa API chuyển trạng thái tổng quát. Payment, giao xe, trả xe, tranh chấp và Settlement phải dùng endpoint nghiệp vụ chuyên biệt.",
                requestedStatus = target
            });
        }

        [HttpPost("reservations/{id:int}/cancel")]
        public async Task<IActionResult> CancelReservation(int id, CancelReservationRequest dto, CancellationToken cancellationToken)
        {
            var privileged = User.IsInRole("Admin") || User.IsInRole("Staff");
            var result = await _cancellationService.CancelAsync(id, UserId(), privileged, dto.Reason, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("cars/bulk-soft-delete")]
        public async Task<IActionResult> BulkSoftDeleteCars(BulkDeleteRequest dto)
        {
            if (dto.Ids is null || dto.Ids.Count == 0) return BadRequest("Chưa chọn xe.");
            if (dto.Ids.Count > 200) return BadRequest("Mỗi lần chỉ xử lý tối đa 200 xe.");

            var cars = await _context.Cars.IgnoreQueryFilters().Where(x => dto.Ids.Contains(x.CarID) && !x.IsDeleted).ToListAsync();
            var activeCarIds = await _context.Reservations
                .Where(x => dto.Ids.Contains(x.CarID) && x.Status != "Hoàn thành" && x.Status != "Đã hủy" && x.Status != "Bị từ chối" && x.Status != "Hết hạn thanh toán")
                .Select(x => x.CarID).Distinct().ToListAsync();

            var deleted = new List<int>();
            var skipped = new List<int>();
            foreach (var car in cars)
            {
                if (activeCarIds.Contains(car.CarID)) { skipped.Add(car.CarID); continue; }
                car.IsDeleted = true;
                car.DeletedAt = DateTime.UtcNow;
                car.DeletedByUserId = UserId();
                car.DeleteReason = dto.Reason;
                car.LifecycleStatus = "Đã xóa";
                deleted.Add(car.CarID);
                AddChange("Car", car.CarID.ToString(), "BulkSoftDelete", null, JsonSerializer.Serialize(car), dto.Reason);
            }
            AddAudit("Xóa mềm hàng loạt xe", nameof(Car), string.Join(',', deleted), $"Bỏ qua: {string.Join(',', skipped)}");
            await _context.SaveChangesAsync();
            return Ok(new { deleted, skipped, message = "Đã xử lý theo lô; các xe có nghiệp vụ đang hoạt động được bỏ qua." });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("trash/cars")]
        public async Task<IActionResult> DeletedCars() => Ok(await _context.Cars.IgnoreQueryFilters().AsNoTracking().Where(x => x.IsDeleted).OrderByDescending(x => x.DeletedAt).ToListAsync());

        [Authorize(Roles = "Admin")]
        [HttpGet("trash/users")]
        public async Task<IActionResult> DeletedUsers() => Ok(await _context.AppUsers.IgnoreQueryFilters().AsNoTracking().Where(x => x.IsDeleted).Select(x => new { x.AppUserId, x.Username, x.Name, x.Surname, x.Email, x.Phone, x.DeletedAt, x.DeleteReason, x.AnonymizedAt }).OrderByDescending(x => x.DeletedAt).ToListAsync());

        [Authorize(Roles = "Admin")]
        [HttpGet("changes/{entity}/{entityId}")]
        public async Task<IActionResult> ChangeHistory(string entity, string entityId) => Ok(await _context.DataChangeHistories.AsNoTracking().Where(x => x.EntityName == entity && x.EntityID == entityId).OrderByDescending(x => x.ChangedAt).ToListAsync());


        private int UserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        private bool IsVehiclePartnerAccount() => string.Equals(User.FindFirstValue("IsVehiclePartner"), "true", StringComparison.OrdinalIgnoreCase);
        private bool CanManageReservation(Reservation reservation) => User.IsInRole("Admin") || User.IsInRole("Staff") || (IsVehiclePartnerAccount() && reservation.PartnerVehicle.OwnerAppUserID == UserId());
        private bool CanAccessReservation(Reservation reservation) => User.IsInRole("Admin") || User.IsInRole("Staff") || reservation.CustomerAppUserID == UserId() || reservation.PartnerVehicle.OwnerAppUserID == UserId();
        private void AddAudit(string action, string entity, string? entityId, string? note) => _context.AuditLogs.Add(new AuditLog { AppUserID = UserId(), Action = action, EntityName = entity, EntityID = entityId, Note = note, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() });
        private void AddChange(string entity, string id, string action, string? oldData, string? newData, string? reason) => _context.DataChangeHistories.Add(new DataChangeHistory { EntityName = entity, EntityID = id, Action = action, OldDataJson = oldData, NewDataJson = newData, Reason = reason, ChangedByAppUserID = UserId() });
    }

    public record DeleteRequest(string? Reason);
    public record TransitionRequest(string NewStatus, string? Reason);
    public record CancelReservationRequest(string Reason);
    public record BulkDeleteRequest(List<int> Ids, string? Reason);
    public record HandoverCorrectionRequest(int OdometerKm, int FuelPercent, string? ExistingDamage, string? Accessories, string? LocationText, IReadOnlyList<Guid>? PhotoFileIds, string Reason);
}
