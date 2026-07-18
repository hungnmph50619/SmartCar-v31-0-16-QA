using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.Entities;
using SmartCar.Domain.BusinessRules;
using SmartCar.Domain.Policies;
using SmartCar.Domain.Time;
using SmartCar.Persistence.Context;

namespace SmartCar.WebApi.Services
{
    public sealed record CancellationPreviewResult(
        bool CanCancel,
        int StatusCode,
        string Message,
        decimal PaidAmount,
        decimal FeeRate,
        decimal CancellationFee,
        decimal RefundAmount,
        string PolicyVersion);

    public interface IReservationCancellationService
    {
        Task<CancellationPreviewResult> PreviewAsync(int reservationId, int actorUserId, bool privileged, CancellationToken cancellationToken = default);
        Task<CancellationPreviewResult> CancelAsync(int reservationId, int actorUserId, bool privileged, string reason, CancellationToken cancellationToken = default);
    }

    public sealed class ReservationCancellationService : IReservationCancellationService
    {
        private static readonly HashSet<string> CancellableStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Chờ chủ xe xác nhận", "Chờ thanh toán", "Chờ khách đặt cọc", "Chờ khách thanh toán giữ chỗ",
            "Chờ nhân viên xác nhận cọc", "Chờ nhân viên xác nhận thanh toán",
            "Đã đặt cọc", "Đã thanh toán", "Đã xác nhận", "Chờ giao xe"
        };

        private readonly CarBookContext _context;
        private readonly TimeProvider _timeProvider;

        public ReservationCancellationService(CarBookContext context, TimeProvider timeProvider)
        {
            _context = context;
            _timeProvider = timeProvider;
        }

        public async Task<CancellationPreviewResult> PreviewAsync(int reservationId, int actorUserId, bool privileged, CancellationToken cancellationToken = default)
        {
            var reservation = await _context.Reservations.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ReservationID == reservationId, cancellationToken);
            if (reservation is null)
                return Denied(404, "Không tìm thấy đơn thuê.");
            if (!privileged && reservation.CustomerAppUserID != actorUserId)
                return Denied(403, "Bạn không có quyền hủy đơn này.");
            if (!CancellableStatuses.Contains(reservation.Status))
                return Denied(409, "Đơn ở trạng thái hiện tại không thể hủy trực tiếp.");

            var paid = await GetPaidAmountsAsync(reservationId, cancellationToken);
            return Calculate(reservation, paid.ChargeablePaid, paid.SecurityDepositPaid);
        }

        public async Task<CancellationPreviewResult> CancelAsync(int reservationId, int actorUserId, bool privileged, string reason, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return Denied(400, "Phải nhập lý do hủy đơn.");

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var reservation = await _context.Reservations
                    .FirstOrDefaultAsync(x => x.ReservationID == reservationId, cancellationToken);
                if (reservation is null)
                    return Denied(404, "Không tìm thấy đơn thuê.");
                if (!privileged && reservation.CustomerAppUserID != actorUserId)
                    return Denied(403, "Bạn không có quyền hủy đơn này.");
                if (!CancellableStatuses.Contains(reservation.Status))
                    return Denied(409, "Đơn ở trạng thái hiện tại không thể hủy trực tiếp.");

                var paid = await GetPaidAmountsAsync(reservationId, cancellationToken);
                var paidAmount = paid.ChargeablePaid + paid.SecurityDepositPaid;
                var preview = Calculate(reservation, paid.ChargeablePaid, paid.SecurityDepositPaid);
                if (!preview.CanCancel)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return preview;
                }

                var oldStatus = reservation.Status;
                var now = _timeProvider.GetUtcNow().UtcDateTime;

                reservation.Status = "Đã hủy";
                reservation.CancellationPolicyVersion = ReservationCancellationPolicy.Version;
                reservation.CancellationReason = reason.Trim();
                reservation.CancellationFeeAmount = preview.CancellationFee;
                reservation.CancelledByAppUserID = actorUserId;
                reservation.CancelledDate = now;
                reservation.HoldExpiresAt = null;

                var commission = await _context.CommissionTransactions
                    .FirstOrDefaultAsync(x => x.ReservationID == reservationId && x.Status != "Đã thanh toán", cancellationToken);
                if (commission is not null)
                {
                    commission.Status = "Đã hủy";
                    commission.Note = $"Hủy theo đơn #{reservationId} - chính sách {ReservationCancellationPolicy.Version}.";
                }

                if (preview.RefundAmount > 0)
                {
                    var refundKey = $"reservation-cancel-refund:{reservationId}";
                    var refundExists = await _context.Payments.AnyAsync(x => x.IdempotencyKey == refundKey, cancellationToken);
                    if (!refundExists)
                    {
                        _context.Payments.Add(new Payment
                        {
                            ReservationID = reservationId,
                            PaymentType = "Hoàn tiền",
                            Amount = preview.RefundAmount,
                            Status = "Chờ hoàn tiền",
                            Provider = "SmartCar",
                            IdempotencyKey = refundKey,
                            VerificationNote = $"Hoàn tiền do hủy đơn theo chính sách {ReservationCancellationPolicy.Version}.",
                            CreatedDate = now
                        });
                    }
                }

                var note = $"{reason.Trim()}. Đã thu {paidAmount.ToString("#,0", CultureInfo.InvariantCulture)} đồng; " +
                           $"phí hủy {preview.FeeRate:0}% = {preview.CancellationFee.ToString("#,0", CultureInfo.InvariantCulture)} đồng; " +
                           $"dự kiến hoàn {preview.RefundAmount.ToString("#,0", CultureInfo.InvariantCulture)} đồng.";

                _context.ReservationStatusHistories.Add(new ReservationStatusHistory
                {
                    ReservationID = reservationId,
                    OldStatus = oldStatus,
                    NewStatus = "Đã hủy",
                    ChangedByAppUserID = actorUserId,
                    Note = note,
                    ChangedDate = now
                });
                _context.DataChangeHistories.Add(new DataChangeHistory
                {
                    EntityName = nameof(Reservation),
                    EntityID = reservationId.ToString(CultureInfo.InvariantCulture),
                    Action = "Cancel",
                    OldDataJson = oldStatus,
                    NewDataJson = "Đã hủy",
                    Reason = reason.Trim(),
                    ChangedByAppUserID = actorUserId,
                    ChangedAt = now
                });
                _context.AuditLogs.Add(new AuditLog
                {
                    AppUserID = actorUserId,
                    Action = "Hủy đơn",
                    EntityName = nameof(Reservation),
                    EntityID = reservationId.ToString(CultureInfo.InvariantCulture),
                    OldValues = oldStatus,
                    NewValues = "Đã hủy",
                    Note = note,
                    CreatedDate = now
                });

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return preview with { Message = "Đã hủy đơn thuê." };
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Denied(409, "Đơn đã được người khác thay đổi. Vui lòng tải lại trước khi hủy.");
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Denied(409, "Yêu cầu hủy đã được xử lý hoặc dữ liệu tài chính đang thay đổi. Vui lòng tải lại.");
            }
        }

        private async Task<(decimal ChargeablePaid, decimal SecurityDepositPaid)> GetPaidAmountsAsync(int reservationId, CancellationToken cancellationToken)
        {
            var values = await _context.Payments.AsNoTracking()
                .Where(x => x.ReservationID == reservationId && x.Status == "Thành công" &&
                            (x.PaymentType == PaymentTypes.LegacyDeposit || x.PaymentType == PaymentTypes.ReservationDeposit ||
                             x.PaymentType == PaymentTypes.SecurityDeposit || x.PaymentType == PaymentTypes.Rental))
                .GroupBy(x => x.PaymentType)
                .Select(g => new { PaymentType = g.Key, Amount = g.Sum(x => x.Amount) })
                .ToListAsync(cancellationToken);
            var security = values.Where(x => x.PaymentType == PaymentTypes.SecurityDeposit).Sum(x => x.Amount);
            var chargeable = values.Where(x => x.PaymentType != PaymentTypes.SecurityDeposit).Sum(x => x.Amount);
            return (chargeable, security);
        }

        private CancellationPreviewResult Calculate(Reservation reservation, decimal chargeablePaid, decimal securityDepositPaid)
        {
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var pickupUtc = VietnamTime.LocalToUtc(reservation.PickUpDate, reservation.PickUpTime);
            if (pickupUtc <= nowUtc)
                return Denied(409, "Đã đến hoặc quá giờ nhận xe. Không thể hủy theo chính sách thông thường; vui lòng liên hệ hỗ trợ để xử lý không đến nhận xe/sự cố.");

            var quote = ReservationCancellationPolicy.Calculate(pickupUtc, chargeablePaid, nowUtc);
            return new CancellationPreviewResult(
                true, 200, "Có thể hủy đơn. Cọc bảo đảm chưa sử dụng được hoàn toàn bộ.", chargeablePaid + securityDepositPaid, quote.FeeRate,
                quote.CancellationFee, quote.RefundAmount + securityDepositPaid, quote.PolicyVersion);
        }

        private static CancellationPreviewResult Denied(int statusCode, string message) =>
            new(false, statusCode, message, 0m, 0m, 0m, 0m, ReservationCancellationPolicy.Version);
    }
}
