using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.BusinessRules;
using SmartCar.Domain.Entities;
using SmartCar.Persistence.Context;
using System.Security.Claims;

namespace SmartCar.WebApi.Controllers
{
    [ApiController]
    [Route("api/payment-resolution")]
    [Authorize(Roles = "Admin,Staff")]
    public class PaymentResolutionController : ControllerBase
    {
        private static readonly HashSet<string> RecoverableDecisions = new(StringComparer.Ordinal)
        {
            "Chưa tìm thấy giao dịch",
            "Thanh toán thiếu",
            "Sai nội dung",
            "Chuyển sai tài khoản"
        };

        private static readonly HashSet<string> TerminalDecisions = new(StringComparer.Ordinal)
        {
            "Thanh toán muộn",
            "Giao dịch không hợp lệ",
            "Bị từ chối"
        };

        private readonly CarBookContext _context;
        private readonly ILogger<PaymentResolutionController> _logger;

        public PaymentResolutionController(CarBookContext context, ILogger<PaymentResolutionController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("reservations/{reservationId:int}/review")]
        public async Task<IActionResult> Review(int reservationId, PaymentResolutionRequest request)
        {
            if (request.PaymentID <= 0) return BadRequest("Không xác định được giao dịch cần xử lý.");

            var decision = (request.Decision ?? string.Empty).Trim();
            var note = (request.Note ?? string.Empty).Trim();
            if (!RecoverableDecisions.Contains(decision) && !TerminalDecisions.Contains(decision))
                return BadRequest("Kết quả đối chiếu không hợp lệ.");
            if (note.Length < 10 || note.Length > 500)
                return BadRequest("Lý do hoặc hướng dẫn khách phải từ 10 đến 500 ký tự.");

            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var reservation = await _context.Reservations
                    .Include(x => x.PartnerVehicle)
                    .FirstOrDefaultAsync(x => x.ReservationID == reservationId);
                if (reservation is null) return NotFound("Không tìm thấy đơn thuê.");
                if (reservation.Status is "Đã hủy" or "Hoàn thành")
                    return Conflict("Không thể xử lý thanh toán của đơn đã đóng.");

                var payment = await _context.Payments.FirstOrDefaultAsync(x =>
                    x.PaymentID == request.PaymentID && x.ReservationID == reservationId && x.Status == "Chờ xác nhận");
                if (payment is null)
                    return Conflict("Khoản thanh toán đã được nhân viên khác xử lý hoặc không còn chờ đối chiếu.");

                var now = DateTime.UtcNow;
                var oldStatus = reservation.Status;
                var isRecoverable = RecoverableDecisions.Contains(decision);

                payment.Status = isRecoverable ? "Cần khách xử lý" : "Bị từ chối";
                payment.VerificationNote = note;
                payment.ConfirmedDate = now;

                if (isRecoverable)
                {
                    var retryExpiresAt = now.AddMinutes(15);
                    reservation.Status = ReservationStatuses.PaymentPending;
                    reservation.HoldExpiresAt = retryExpiresAt;
                    reservation.PaymentExpiresAt = retryExpiresAt;

                    AddNotification(
                        reservation.CustomerAppUserID,
                        $"Thanh toán đơn #{reservationId} cần xử lý lại",
                        $"{decision}. {note} Bạn có 15 phút để kiểm tra và báo chuyển khoản lại.",
                        $"/ReservationLookup/Details/{reservationId}#paymentSection");
                    AddNotification(
                        reservation.PartnerVehicle.OwnerAppUserID,
                        $"Thanh toán đơn #{reservationId} đang gặp vấn đề",
                        $"SmartCar đã yêu cầu khách xử lý lại: {decision}. Lịch xe được giữ thêm tối đa 15 phút.",
                        $"/ReservationLookup/Details/{reservationId}#paymentSection");
                }
                else
                {
                    reservation.Status = ReservationStatuses.PaymentExpired;
                    reservation.HoldExpiresAt = now;
                    reservation.PaymentExpiresAt = now;

                    AddNotification(
                        reservation.CustomerAppUserID,
                        $"Thanh toán đơn #{reservationId} không được xác nhận",
                        $"{decision}. {note} Lịch giữ xe đã được giải phóng.",
                        $"/ReservationLookup/Details/{reservationId}#paymentSection");
                    AddNotification(
                        reservation.PartnerVehicle.OwnerAppUserID,
                        $"Đơn #{reservationId} đã đóng do thanh toán không thành công",
                        "SmartCar không thể xác nhận giao dịch của khách. Lịch xe đã được mở lại để nhận đơn khác.",
                        $"/ReservationLookup/Details/{reservationId}");
                }

                _context.ReservationStatusHistories.Add(new ReservationStatusHistory
                {
                    ReservationID = reservationId,
                    OldStatus = oldStatus,
                    NewStatus = reservation.Status,
                    ChangedByAppUserID = CurrentUserId(),
                    ChangedDate = now,
                    Note = $"Kết quả đối chiếu: {decision}. {note}"
                });

                var claim = await _context.WorkItemClaims.FirstOrDefaultAsync(x =>
                    x.QueueType == "Thanh toán" && x.EntityID == payment.PaymentID && x.Status == "Đang xử lý");
                if (claim is not null) claim.Status = "Đã hoàn tất";

                _context.AuditLogs.Add(new AuditLog
                {
                    AppUserID = CurrentUserId(),
                    Action = isRecoverable ? "Yêu cầu xử lý lại thanh toán" : "Từ chối xác nhận thanh toán",
                    EntityName = nameof(Payment),
                    EntityID = payment.PaymentID.ToString(),
                    Note = $"{decision}: {note}",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    ReservationStatus = reservation.Status,
                    PaymentStatus = payment.Status,
                    RetryExpiresAt = isRecoverable ? reservation.HoldExpiresAt : null,
                    Message = isRecoverable
                        ? "Đã yêu cầu khách xử lý lại. Lịch xe được giữ thêm 15 phút."
                        : "Đã từ chối xác nhận và giải phóng lịch xe."
                });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Xung đột khi xử lý thanh toán đơn {ReservationId}", reservationId);
                return Conflict("Giao dịch đã được nhân viên khác xử lý. Vui lòng tải lại trang.");
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Không thể lưu kết quả thanh toán đơn {ReservationId}", reservationId);
                return Conflict("Dữ liệu thanh toán đã thay đổi. Vui lòng tải lại trang.");
            }
        }

        private void AddNotification(int appUserId, string title, string message, string link)
        {
            _context.Notifications.Add(new Notification
            {
                AppUserID = appUserId,
                Title = title,
                Message = message,
                Type = "Payment",
                Link = link,
                CreatedDate = DateTime.UtcNow
            });
        }

        private int? CurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public sealed class PaymentResolutionRequest
    {
        public int PaymentID { get; set; }
        public string? Decision { get; set; }
        public string? Note { get; set; }
    }
}
