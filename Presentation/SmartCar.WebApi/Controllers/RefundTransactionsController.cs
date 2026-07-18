using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.BusinessRules;
using SmartCar.Domain.Entities;
using SmartCar.Persistence.Context;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SmartCar.WebApi.Controllers
{
    [ApiController]
    [Route("api/refund-transactions")]
    public class RefundTransactionsController : ControllerBase
    {
        private readonly CarBookContext _context;
        public RefundTransactionsController(CarBookContext context) => _context = context;

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        public async Task<IActionResult> Propose(CreateRefundRequest request)
        {
            var proposerId = CurrentUserId();
            var reservation = await _context.Reservations.FirstOrDefaultAsync(x => x.ReservationID == request.ReservationID);
            if (reservation is null) return NotFound("Không tìm thấy đơn.");
            var payment = request.OriginalPaymentID.HasValue
                ? await _context.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.PaymentID == request.OriginalPaymentID && x.ReservationID == request.ReservationID)
                : await _context.Payments.AsNoTracking().Where(x => x.ReservationID == request.ReservationID && x.Status == "Thành công").OrderByDescending(x => x.PaymentID).FirstOrDefaultAsync();
            if (payment is null) return BadRequest("Đơn chưa có giao dịch thanh toán thành công để liên kết hoàn tiền.");
            if (request.Amount <= 0 || request.Amount > payment.Amount) return BadRequest("Số tiền hoàn không hợp lệ hoặc vượt số tiền giao dịch gốc.");
            var idempotency = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                ? $"REFUND-{request.ReservationID}-{payment.PaymentID}-{request.Amount:0.##}-{DateTime.UtcNow:yyyyMMddHHmm}"
                : request.IdempotencyKey.Trim();
            var existing = await _context.RefundTransactions.AsNoTracking().FirstOrDefaultAsync(x => x.IdempotencyKey == idempotency);
            if (existing is not null) return Ok(existing);

            var entity = new RefundTransaction
            {
                ReservationID = request.ReservationID,
                OriginalPaymentID = payment.PaymentID,
                Amount = request.Amount,
                Reason = (request.Reason ?? string.Empty).Trim(),
                Status = "Proposed",
                IdempotencyKey = idempotency,
                ProposedByAppUserID = proposerId,
                ProposedAt = DateTime.UtcNow
            };
            _context.RefundTransactions.Add(entity);
            var oldReservationStatus = reservation.Status;
            reservation.Status = ReservationStatuses.Refunding;
            reservation.StateVersion++;
            _context.ReservationStatusHistories.Add(new ReservationStatusHistory
            {
                ReservationID = reservation.ReservationID,
                OldStatus = oldReservationStatus,
                NewStatus = ReservationStatuses.Refunding,
                ChangedByAppUserID = proposerId,
                Note = $"Đề xuất hoàn {request.Amount:#,0}: {entity.Reason}",
                ChangedDate = DateTime.UtcNow
            });
            AddAudit(proposerId, "Đề xuất hoàn tiền", nameof(RefundTransaction), null, $"Reservation={request.ReservationID}; Amount={request.Amount:#,0}");
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}/approve")]
        public async Task<IActionResult> Approve(int id, ReviewRefundRequest request)
        {
            var adminId = CurrentUserId();
            var entity = await _context.RefundTransactions.Include(x => x.Reservation).FirstOrDefaultAsync(x => x.RefundTransactionID == id);
            if (entity is null) return NotFound("Không tìm thấy giao dịch hoàn.");
            if (entity.Status != "Proposed") return Conflict("Giao dịch hoàn không ở trạng thái chờ phê duyệt.");
            if (entity.ProposedByAppUserID == adminId) return Conflict("Người đề xuất không được tự phê duyệt hoàn tiền.");
            if (!request.Approve)
            {
                if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest("Bắt buộc nhập lý do từ chối.");
                entity.Status = "Rejected";
                entity.ApprovedByAppUserID = adminId;
                entity.ApprovedAt = DateTime.UtcNow;
                entity.Reason += $" | Từ chối: {request.Reason.Trim()}";
            }
            else
            {
                entity.Status = "Approved";
                entity.ApprovedByAppUserID = adminId;
                entity.ApprovedAt = DateTime.UtcNow;
            }
            AddAudit(adminId, request.Approve ? "Phê duyệt hoàn tiền" : "Từ chối hoàn tiền", nameof(RefundTransaction), id.ToString(), request.Reason);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}/complete")]
        public async Task<IActionResult> Complete(int id, CompleteRefundRequest request)
        {
            var adminId = CurrentUserId();
            var entity = await _context.RefundTransactions.Include(x => x.Reservation).FirstOrDefaultAsync(x => x.RefundTransactionID == id);
            if (entity is null) return NotFound("Không tìm thấy giao dịch hoàn.");
            if (entity.Status != "Approved") return Conflict("Giao dịch hoàn chưa được phê duyệt.");
            if (string.IsNullOrWhiteSpace(request.BankReference)) return BadRequest("Vui lòng nhập mã tham chiếu chuyển khoản hoàn.");
            entity.Status = "Completed";
            entity.BankReference = request.BankReference.Trim();
            entity.CompletedAt = DateTime.UtcNow;
            AddAudit(adminId, "Hoàn tất chuyển khoản hoàn", nameof(RefundTransaction), id.ToString(), entity.BankReference);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet("reservation/{reservationId:int}")]
        public async Task<IActionResult> ByReservation(int reservationId)
            => Ok(await _context.RefundTransactions.AsNoTracking().Where(x => x.ReservationID == reservationId).OrderByDescending(x => x.ProposedAt).ToListAsync());

        private int CurrentUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        private void AddAudit(int userId, string action, string entityName, string? entityId, string? note)
            => _context.AuditLogs.Add(new AuditLog { AppUserID = userId, Action = action, EntityName = entityName, EntityID = entityId, Note = note, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() });
    }

    public sealed class CreateRefundRequest
    {
        [Range(1, int.MaxValue)] public int ReservationID { get; set; }
        public int? OriginalPaymentID { get; set; }
        [Range(typeof(decimal), "1", "10000000000")] public decimal Amount { get; set; }
        [Required, MaxLength(500)] public string Reason { get; set; } = string.Empty;
        [MaxLength(100)] public string? IdempotencyKey { get; set; }
    }
    public sealed class ReviewRefundRequest { public bool Approve { get; set; } [MaxLength(1000)] public string? Reason { get; set; } }
    public sealed class CompleteRefundRequest { [Required, MaxLength(100)] public string BankReference { get; set; } = string.Empty; }
}
