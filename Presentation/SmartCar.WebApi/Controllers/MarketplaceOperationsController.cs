using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.BusinessRules;
using SmartCar.Domain.Entities;
using SmartCar.Domain.Security;
using SmartCar.Domain.Time;
using SmartCar.Persistence.Context;
using SmartCar.WebApi.Services;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SmartCar.WebApi.Controllers
{
    [ApiController]
    [Route("api/marketplace-operations")]
    [Authorize]
    public class MarketplaceOperationsController : ControllerBase
    {
        private static readonly string[] AllowedPaymentTypes =
        [PaymentTypes.LegacyDeposit, PaymentTypes.ReservationDeposit, PaymentTypes.SecurityDeposit, PaymentTypes.Rental, PaymentTypes.AdditionalCharge];
        private readonly CarBookContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MarketplaceOperationsController> _logger;
        private readonly IPrivateFileService _files;

        public MarketplaceOperationsController(
            CarBookContext context,
            IConfiguration configuration,
            ILogger<MarketplaceOperationsController> logger,
            IPrivateFileService files)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _files = files;
        }

        [HttpGet("reservations/{reservationId:int}/timeline")]
        public async Task<IActionResult> Timeline(int reservationId)
        {
            if (!await CanAccessReservation(reservationId)) return Forbid();
            var values = await _context.ReservationStatusHistories.AsNoTracking()
                .Where(x => x.ReservationID == reservationId)
                .OrderBy(x => x.ChangedDate)
                .ToListAsync();
            return Ok(values);
        }

        [Authorize(Roles = "Customer,VehiclePartner")]
        [HttpPost("reservations/{reservationId:int}/disputes")]
        public async Task<IActionResult> CreateDispute(int reservationId, CreateDisputeDto dto)
        {
            if (!await CanAccessReservation(reservationId)) return Forbid();
            if (string.IsNullOrWhiteSpace(dto.Description))
                return BadRequest("Vui lòng mô tả nội dung khiếu nại.");

            var reservation = await _context.Reservations.FindAsync(reservationId);
            if (reservation is null) return NotFound("Không tìm thấy đơn thuê.");
            if (reservation.Status is "Đã hủy" or "Hoàn thành")
                return Conflict("Không thể mở khiếu nại mới cho đơn đã đóng.");
            if (await _context.Settlements.AnyAsync(x => x.ReservationID == reservationId))
                return Conflict("Đơn đã lập đối soát. Không thể mở tranh chấp làm thay đổi số tiền đã chốt.");

            var hasOpenDispute = await _context.Disputes.AnyAsync(x =>
                x.ReservationID == reservationId &&
                x.Status != "Đã giải quyết" && x.Status != "Đã đóng");
            if (hasOpenDispute) return Conflict("Đơn thuê đang có khiếu nại chưa xử lý.");

            try
            {
                var files = await _files.ValidateForAttachmentAsync(dto.EvidenceFileIds, UserId(), "DisputeEvidence", reservationId, IsPrivileged(), HttpContext.RequestAborted);
                await using var transaction = await _context.Database.BeginTransactionAsync(HttpContext.RequestAborted);
                var oldStatus = reservation.Status;
                reservation.Status = "Đang tranh chấp";
                var item = new Dispute
                {
                    ReservationID = reservationId,
                    CreatedByAppUserID = UserId(),
                    Type = string.IsNullOrWhiteSpace(dto.Type) ? "Khác" : (dto.Type ?? string.Empty).Trim(),
                    Description = (dto.Description ?? string.Empty).Trim(),
                    EvidenceUrls = files.Count == 0 ? null : string.Join(',', files.Select(x => _files.BuildViewUrl(x.PrivateFileID))),
                    Status = "Mới tiếp nhận"
                };
                _context.Disputes.Add(item);
                AddHistory(reservationId, oldStatus, reservation.Status, "Người dùng mở khiếu nại.");
                await _context.SaveChangesAsync(HttpContext.RequestAborted);
                _files.MarkAttached(files, nameof(Dispute), item.DisputeID.ToString());
                AddAudit("Tạo khiếu nại", nameof(Dispute), item.DisputeID.ToString(), item.Description);
                await _context.SaveChangesAsync(HttpContext.RequestAborted);
                await transaction.CommitAsync(HttpContext.RequestAborted);
                return Ok(new { item.DisputeID, Message = "Đã tiếp nhận khiếu nại." });
            }
            catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("disputes/{id:int}/messages")]
        public async Task<IActionResult> AddDisputeMessage(int id, AddDisputeMessageDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Message)) return BadRequest("Vui lòng nhập nội dung trao đổi.");
            var dispute = await _context.Disputes.AsNoTracking().FirstOrDefaultAsync(x => x.DisputeID == id);
            if (dispute is null) return NotFound("Không tìm thấy khiếu nại.");
            if (!await CanAccessReservation(dispute.ReservationID)) return Forbid();
            if (dispute.Status is "Đã đóng") return Conflict("Khiếu nại đã đóng, không thể gửi thêm trao đổi.");
            try
            {
                var files = await _files.ValidateForAttachmentAsync(dto.EvidenceFileIds, UserId(), "DisputeEvidence", dispute.ReservationID, IsPrivileged(), HttpContext.RequestAborted);
                await using var transaction = await _context.Database.BeginTransactionAsync(HttpContext.RequestAborted);
                var item = new DisputeMessage
                {
                    DisputeID = id,
                    SenderAppUserID = UserId(),
                    Message = (dto.Message ?? string.Empty).Trim(),
                    EvidenceUrls = files.Count == 0 ? null : string.Join(',', files.Select(x => _files.BuildViewUrl(x.PrivateFileID)))
                };
                _context.DisputeMessages.Add(item);
                await _context.SaveChangesAsync(HttpContext.RequestAborted);
                _files.MarkAttached(files, nameof(DisputeMessage), item.DisputeMessageID.ToString());
                AddAudit("Trao đổi khiếu nại", nameof(DisputeMessage), item.DisputeMessageID.ToString(), item.Message);
                await _context.SaveChangesAsync(HttpContext.RequestAborted);
                await transaction.CommitAsync(HttpContext.RequestAborted);
                return Ok("Đã gửi trao đổi vào hồ sơ tranh chấp.");
            }
            catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("disputes/{id:int}/resolve")]
        public async Task<IActionResult> ResolveDispute(int id, ResolveDisputeDto dto)
        {
            var item = await _context.Disputes.Include(x => x.Reservation)
                .FirstOrDefaultAsync(x => x.DisputeID == id);
            if (item is null) return NotFound("Không tìm thấy khiếu nại.");
            if (item.Status is "Đã giải quyết" or "Đã đóng")
                return Conflict("Khiếu nại đã được xử lý trước đó.");
            if (await _context.Settlements.AnyAsync(x => x.ReservationID == item.ReservationID))
                return Conflict("Đơn đã lập đối soát. Không thể thay đổi kết luận tranh chấp.");
            if (string.IsNullOrWhiteSpace(dto.Resolution))
                return BadRequest("Vui lòng nhập kết luận xử lý khiếu nại.");

            item.Status = "Đã giải quyết";
            item.Resolution = (dto.Resolution ?? string.Empty).Trim();
            item.CompensationAmount = Math.Max(0, dto.CompensationAmount);
            item.AssignedStaffAppUserID = UserId();
            item.ResolvedDate = DateTime.UtcNow;

            var oldStatus = item.Reservation.Status;
            item.Reservation.Status = "Chờ đối soát";
            var disputeClaim = await _context.WorkItemClaims.FirstOrDefaultAsync(x => x.QueueType == "Tranh chấp" && x.EntityID == id && x.Status == "Đang xử lý");
            if (disputeClaim is not null) disputeClaim.Status = "Đã hoàn tất";
            AddHistory(item.ReservationID, oldStatus, item.Reservation.Status, item.Resolution);
            AddAudit("Giải quyết khiếu nại", nameof(Dispute), id.ToString(), item.Resolution);
            await _context.SaveChangesAsync();
            return Ok("Đã ghi nhận kết luận khiếu nại.");
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost("reservations/{reservationId:int}/payments/confirm")]
        public async Task<IActionResult> ConfirmPayment(int reservationId, ConfirmPaymentDto dto)
        {
            if (dto.PaymentID <= 0) return BadRequest("Không xác định được giao dịch cần đối chiếu.");
            if (dto.Amount <= 0) return BadRequest("Số tiền thực nhận phải lớn hơn 0.");
            var transactionCode = dto.TransactionCode?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(transactionCode) || transactionCode.Length > 100)
                return BadRequest("Nhân viên phải nhập mã giao dịch trên sao kê ngân hàng, tối đa 100 ký tự.");
            var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault()?.Trim();
            if (string.IsNullOrWhiteSpace(idempotencyKey)) idempotencyKey = $"payment-confirm:{dto.PaymentID}:{transactionCode}";
            if (idempotencyKey.Length > 100) return BadRequest("Idempotency-Key tối đa 100 ký tự.");

            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var reservation = await _context.Reservations.FirstOrDefaultAsync(x => x.ReservationID == reservationId);
                if (reservation is null) return NotFound("Không tìm thấy đơn thuê.");
                if (reservation.Status is "Đã hủy" or "Hoàn thành")
                    return Conflict("Không thể ghi nhận thanh toán cho đơn ở trạng thái hiện tại.");

                var payment = await _context.Payments.FirstOrDefaultAsync(x => x.PaymentID == dto.PaymentID && x.ReservationID == reservationId);
                if (payment is null) return NotFound("Không tìm thấy khoản thanh toán thuộc đơn này.");
                if (payment.Status != "Chờ xác nhận")
                {
                    if (payment.Status == "Thành công" && payment.IdempotencyKey == idempotencyKey)
                        return Ok(new { payment.PaymentID, ReservationStatus = reservation.Status, PaymentStatus = payment.Status, IdempotentReplay = true, Message = "Giao dịch đã được xác nhận ở yêu cầu trước." });
                    return Conflict("Khoản thanh toán không còn ở trạng thái chờ xác nhận.");
                }

                var paymentType = payment.PaymentType?.Trim() ?? string.Empty;
                if (!AllowedPaymentTypes.Contains(paymentType, StringComparer.Ordinal))
                    return BadRequest("Loại thanh toán trong dữ liệu không hợp lệ.");
                var requiredHoldPaymentType = GetHoldPaymentType(reservation);
                if ((reservation.Status is "Chờ nhân viên xác nhận cọc" or "Chờ nhân viên xác nhận thanh toán") && paymentType != requiredHoldPaymentType)
                    return BadRequest($"Đơn này yêu cầu đối chiếu '{requiredHoldPaymentType}' để xác nhận giữ chỗ.");
                if (await _context.Payments.AnyAsync(x => x.TransactionCode == transactionCode && x.PaymentID != payment.PaymentID && x.Status == "Thành công"))
                    return Conflict("Mã giao dịch đã được xác nhận cho một khoản thanh toán khác.");
                if (await _context.Payments.AnyAsync(x => x.IdempotencyKey == idempotencyKey && x.PaymentID != payment.PaymentID))
                    return Conflict("Yêu cầu xác nhận trùng đã được dùng cho khoản thanh toán khác.");

                var reportedAt = payment.CustomerReportedDate ?? payment.CreatedDate;
                var expiredButSubmittedOnTime = paymentType == requiredHoldPaymentType && reservation.Status == ReservationStatuses.PaymentExpired &&
                    reservation.HoldExpiresAt.HasValue && reportedAt <= reservation.HoldExpiresAt.Value;
                if (reservation.Status == ReservationStatuses.PaymentExpired && !expiredButSubmittedOnTime)
                    return Conflict("Khách báo chuyển khoản sau khi thời hạn giữ xe đã hết. Không được tự khôi phục đơn; cần hoàn tiền hoặc tạo đơn mới.");

                decimal remaining;
                if (paymentType == PaymentTypes.AdditionalCharge)
                {
                    var charge = await _context.AdditionalCharges.FirstOrDefaultAsync(x =>
                        x.ReservationID == reservationId && x.PaymentID == payment.PaymentID &&
                        (x.Status == AdditionalChargeStatuses.CustomerAccepted || x.Status == AdditionalChargeStatuses.Approved));
                    if (charge is null || payment.RelatedEntityType != nameof(AdditionalCharge) || payment.RelatedEntityID != charge.AdditionalChargeID)
                        return BadRequest("Giao dịch phụ phí không liên kết đúng với phụ phí đã được khách chấp nhận.");
                    remaining = charge.Amount;
                }
                else
                {
                    var expectedTotal = GetExpectedPaymentTotal(reservation, paymentType);
                    if (expectedTotal <= 0) return BadRequest($"Đơn thuê không có khoản {paymentType.ToLowerInvariant()} cần thu.");
                    var paidAmount = await _context.Payments
                        .Where(x => x.ReservationID == reservationId && x.PaymentType == paymentType && x.Status == "Thành công" && x.PaymentID != payment.PaymentID)
                        .SumAsync(x => (decimal?)x.Amount) ?? 0m;
                    if (paymentType == PaymentTypes.Rental)
                    {
                        var paidReservationDeposit = await GetPaidReservationDepositAsync(reservationId, reservation, payment.PaymentID);
                        remaining = expectedTotal - paidAmount - paidReservationDeposit;
                    }
                    else
                    {
                        remaining = expectedTotal - paidAmount;
                    }
                }
                if (remaining <= 0) return Conflict($"Khoản {paymentType.ToLowerInvariant()} của đơn đã được xác nhận đủ.");
                if (dto.Amount != remaining)
                    return BadRequest($"Số tiền thực nhận không khớp. Cần {remaining.ToString("#,0", CultureInfo.InvariantCulture)} đồng nhưng nhân viên nhập {dto.Amount.ToString("#,0", CultureInfo.InvariantCulture)} đồng.");

                payment.Amount = dto.Amount;
                payment.TransactionCode = transactionCode;
                payment.IdempotencyKey = idempotencyKey;
                // Luồng hiện tại là báo chuyển khoản thủ công, không tin tên nhà cung cấp từ client.
            payment.Provider = "Chuyển khoản ngân hàng thủ công";
                payment.VerificationNote = string.IsNullOrWhiteSpace(dto.VerificationNote) ? "Đã đối chiếu thủ công với sao kê ngân hàng." : (dto.VerificationNote ?? string.Empty).Trim();
                payment.IsSimulated = false;

                var confirmsReservationHold = await FinalizeSuccessfulPaymentAsync(reservation, payment, paymentType, expiredButSubmittedOnTime,
                    $"Nhân viên đã đối chiếu thủ công và xác nhận {paymentType.ToLowerInvariant()} hợp lệ.");
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { payment.PaymentID, ReservationStatus = reservation.Status, PaymentStatus = payment.Status, ConfirmsReservationHold = confirmsReservationHold, Message = "Đã xác nhận thanh toán." });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Xung đột đồng thời khi xác nhận thanh toán cho đơn {ReservationId}", reservationId);
                return Conflict("Khoản thanh toán đã được nhân viên khác xử lý. Vui lòng tải lại dữ liệu.");
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Xung đột khi xác nhận thanh toán cho đơn {ReservationId}", reservationId);
                return Conflict("Giao dịch hoặc yêu cầu xác nhận đã được ghi nhận bởi thao tác khác. Vui lòng tải lại dữ liệu.");
            }
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost("reservations/{reservationId:int}/payments/review")]
        public async Task<IActionResult> ReviewPayment(int reservationId, ReviewPaymentDto dto)
        {
            var allowed = new[] { "Chưa tìm thấy giao dịch", "Thanh toán thiếu", "Thanh toán thừa", "Sai nội dung", "Thanh toán muộn", "Bị từ chối" };
            var decision = dto.Decision?.Trim() ?? string.Empty;
            if (!allowed.Contains(decision, StringComparer.Ordinal)) return BadRequest("Kết quả đối chiếu không hợp lệ.");
            if (string.IsNullOrWhiteSpace(dto.Note)) return BadRequest("Phải nhập lý do hoặc hướng dẫn xử lý cho khách.");

            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var reservation = await _context.Reservations.FirstOrDefaultAsync(x => x.ReservationID == reservationId);
                if (reservation is null) return NotFound("Không tìm thấy đơn thuê.");
                var payment = await _context.Payments.FirstOrDefaultAsync(x => x.PaymentID == dto.PaymentID && x.ReservationID == reservationId && x.Status == "Chờ xác nhận");
                if (payment is null) return Conflict("Khoản thanh toán đã được nhân viên khác xử lý hoặc không còn chờ đối chiếu.");

                payment.Status = decision;
                payment.VerificationNote = (dto.Note ?? string.Empty).Trim();
                payment.ConfirmedDate = DateTime.UtcNow;
                var oldStatus = reservation.Status;
                var holdStillValid = reservation.HoldExpiresAt.HasValue && reservation.HoldExpiresAt.Value > DateTime.UtcNow;
                reservation.Status = decision == "Thanh toán muộn" || !holdStillValid
                    ? ReservationStatuses.PaymentExpired
                    : ReservationStatuses.PaymentPending;
                if (oldStatus != reservation.Status) AddHistory(reservationId, oldStatus, reservation.Status, $"Kết quả đối chiếu: {decision}. {payment.VerificationNote}");

                var claim = await _context.WorkItemClaims.FirstOrDefaultAsync(x => x.QueueType == "Thanh toán" && x.EntityID == payment.PaymentID && x.Status == "Đang xử lý");
                if (claim is not null) claim.Status = "Đã hoàn tất";
                _context.Notifications.Add(new Notification { AppUserID = reservation.CustomerAppUserID, Title = $"Kết quả đối chiếu thanh toán: {decision}", Message = payment.VerificationNote, Type = "Payment" });
                AddAudit("Đối chiếu thanh toán không thành công", nameof(Payment), payment.PaymentID.ToString(), $"{decision}: {payment.VerificationNote}");
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok("Đã lưu kết quả đối chiếu và thông báo cho khách.");
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                return Conflict("Khoản thanh toán đã được nhân viên khác xử lý. Vui lòng tải lại dữ liệu.");
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                return Conflict("Dữ liệu thanh toán đã thay đổi. Vui lòng tải lại dữ liệu.");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("reservations/{reservationId:int}/payments/simulate")]
        public async Task<IActionResult> SimulatePayment(int reservationId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var reservation = await _context.Reservations.FirstOrDefaultAsync(x => x.ReservationID == reservationId);
                if (reservation is null) return NotFound("Không tìm thấy đơn thuê.");
                if (reservation.Status is not (ReservationStatuses.PaymentPending or "Chờ khách đặt cọc" or "Chờ khách thanh toán giữ chỗ" or "Chờ nhân viên xác nhận cọc" or "Chờ nhân viên xác nhận thanh toán"))
                    return Conflict("Đơn không ở bước có thể giả lập thanh toán.");
                if (reservation.Status is ReservationStatuses.PaymentPending or "Chờ khách đặt cọc" or "Chờ khách thanh toán giữ chỗ")
                {
                    if (!reservation.HoldExpiresAt.HasValue || reservation.HoldExpiresAt.Value <= DateTime.UtcNow)
                        return Conflict("Thời gian giữ xe đã hết, không thể giả lập thanh toán cho đơn này.");
                }

                var paymentType = GetHoldPaymentType(reservation);
                var expectedTotal = GetExpectedPaymentTotal(reservation, paymentType);
                var paidAmount = await _context.Payments
                    .Where(x => x.ReservationID == reservationId && x.PaymentType == paymentType && x.Status == "Thành công")
                    .SumAsync(x => (decimal?)x.Amount) ?? 0m;
                var remaining = expectedTotal - paidAmount;
                if (remaining <= 0) return Conflict("Khoản thanh toán giữ chỗ đã được xác nhận đủ.");

                var payment = await _context.Payments.FirstOrDefaultAsync(x =>
                    x.ReservationID == reservationId && x.PaymentType == paymentType && x.Status == "Chờ xác nhận");
                if (payment is null)
                {
                    payment = new Payment
                    {
                        ReservationID = reservationId,
                        PaymentType = paymentType,
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.Payments.Add(payment);
                }

                payment.Amount = remaining;
                payment.TransactionCode = $"DEMO-{reservationId}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
                payment.Provider = "Giả lập ngân hàng";
                payment.TransferContent = $"SC{reservationId:D6}";
                payment.CustomerReportedDate = DateTime.UtcNow;
                payment.IsSimulated = true;
                payment.VerificationNote = "Giao dịch giả lập phục vụ trình diễn đồ án; không phát sinh tiền thật.";
                if (payment.PaymentID == 0)
                    await _context.SaveChangesAsync();

                await FinalizeSuccessfulPaymentAsync(
                    reservation,
                    payment,
                    paymentType,
                    false,
                    "Admin đã giả lập giao dịch ngân hàng thành công để trình diễn đồ án.");

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok("Đã tạo và đối chiếu giao dịch giả lập thành công. Đơn chuyển sang chờ giao xe.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi giả lập thanh toán cho đơn {ReservationId}", reservationId);
                return StatusCode(500, "Không thể giả lập thanh toán.");
            }
        }

        private static string GetHoldPaymentType(Reservation reservation)
        {
            if (reservation.ReservationDepositAmount > 0) return PaymentTypes.ReservationDeposit;
            if (reservation.ReservationDepositAmount <= 0 && reservation.SecurityDepositAmount <= 0 && reservation.DepositAmount > 0)
                return PaymentTypes.LegacyDeposit;
            return PaymentTypes.Rental;
        }

        private static decimal GetExpectedPaymentTotal(Reservation reservation, string paymentType)
            => paymentType switch
            {
                PaymentTypes.ReservationDeposit => reservation.ReservationDepositAmount,
                PaymentTypes.SecurityDeposit => reservation.SecurityDepositAmount,
                PaymentTypes.LegacyDeposit => reservation.DepositAmount,
                PaymentTypes.Rental => reservation.TotalPrice,
                _ => 0m
            };

        private async Task<decimal> GetPaidReservationDepositAsync(int reservationId, Reservation reservation, int excludedPaymentId = 0)
        {
            if (reservation.ReservationDepositAmount <= 0) return 0m;
            return await _context.Payments
                .Where(x => x.ReservationID == reservationId && x.PaymentType == PaymentTypes.ReservationDeposit &&
                            x.Status == "Thành công" && x.PaymentID != excludedPaymentId)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;
        }

        private async Task<bool> FinalizeSuccessfulPaymentAsync(
            Reservation reservation,
            Payment payment,
            string paymentType,
            bool expiredButSubmittedOnTime,
            string historyNote)
        {
            payment.Status = "Thành công";
            payment.ConfirmedDate = DateTime.UtcNow;
            payment.Provider = string.IsNullOrWhiteSpace(payment.Provider) ? "Chuyển khoản ngân hàng thủ công" : payment.Provider.Trim();
            // Chỉ callback đã xác thực từ cổng thanh toán mới được ghi phí nhà cung cấp.
            // SmartCar hiện dùng đối chiếu chuyển khoản thủ công nên phí này luôn bằng 0.
            payment.ProviderFeeAmount = 0m;
            payment.ProviderFeeVerified = false;
            payment.TransferContent ??= $"SC{reservation.ReservationID:D6}";

            var requiredHoldPaymentType = GetHoldPaymentType(reservation);
            var oldStatus = reservation.Status;
            var confirmsReservationHold = paymentType == requiredHoldPaymentType &&
                (reservation.Status is ReservationStatuses.PaymentPending or "Chờ khách đặt cọc" or "Chờ khách thanh toán giữ chỗ" or
                    "Chờ nhân viên xác nhận cọc" or "Chờ nhân viên xác nhận thanh toán" or
                    "Đã xác nhận" or "Đã đặt cọc" || expiredButSubmittedOnTime);

            if (paymentType == PaymentTypes.SecurityDeposit)
                reservation.DepositStatus = "Đã thanh toán cọc bảo đảm";

            if (confirmsReservationHold)
            {
                if (paymentType is PaymentTypes.LegacyDeposit or PaymentTypes.ReservationDeposit)
                    reservation.DepositStatus = "Đã thanh toán cọc giữ chỗ";
                var pickupUtc = VietnamTime.LocalToUtc(reservation.PickUpDate, reservation.PickUpTime);
                reservation.Status = pickupUtc <= DateTime.UtcNow.AddHours(24)
                    ? ReservationStatuses.HandoverPending
                    : ReservationStatuses.Confirmed;
                reservation.HoldExpiresAt = null;
                reservation.PaymentExpiresAt = null;
                reservation.StateVersion++;
            }

            if (confirmsReservationHold && reservation.Status is ReservationStatuses.HandoverPending or ReservationStatuses.Confirmed)
            {
                var confirmedStart = reservation.PickUpDate.Date.Add(reservation.PickUpTime);
                var confirmedEnd = reservation.DropOffDate.Date.Add(reservation.DropOffTime);
                var competingRequests = await _context.Reservations
                    .Where(x => x.ReservationID != reservation.ReservationID &&
                                x.CarID == reservation.CarID &&
                                x.Status == "Chờ chủ xe xác nhận")
                    .ToListAsync();
                foreach (var competing in competingRequests.Where(x =>
                             ReservationAvailabilityRules.OverlapsWithTurnaroundBuffer(
                                 x.PickUpDate.Date.Add(x.PickUpTime),
                                 x.DropOffDate.Date.Add(x.DropOffTime),
                                 confirmedStart,
                                 confirmedEnd)))
                {
                    competing.Status = "Bị từ chối";
                    competing.OwnerResponseDate = DateTime.UtcNow;
                    competing.OwnerNote = $"Xe đã được xác nhận cho đơn #{reservation.ReservationID} trùng thời gian.";
                    _context.ReservationStatusHistories.Add(new ReservationStatusHistory
                    {
                        ReservationID = competing.ReservationID,
                        OldStatus = "Chờ chủ xe xác nhận",
                        NewStatus = "Bị từ chối",
                        Note = competing.OwnerNote,
                        ChangedDate = DateTime.UtcNow
                    });
                    _context.Notifications.Add(new Notification
                    {
                        AppUserID = competing.CustomerAppUserID,
                        Title = "Yêu cầu thuê xe đã được đóng",
                        Message = "Xe đã được xác nhận cho một đơn khác trùng thời gian. Vui lòng chọn xe hoặc thời gian khác.",
                        Type = "Reservation"
                    });
                }
            }

            if (paymentType == PaymentTypes.AdditionalCharge && payment.RelatedEntityType == nameof(AdditionalCharge) && payment.RelatedEntityID.HasValue)
            {
                var paidCharge = await _context.AdditionalCharges.FirstOrDefaultAsync(x =>
                    x.AdditionalChargeID == payment.RelatedEntityID.Value && x.ReservationID == reservation.ReservationID);
                if (paidCharge is not null)
                {
                    paidCharge.Status = AdditionalChargeStatuses.Collected;
                    paidCharge.ResolvedDate = DateTime.UtcNow;
                    paidCharge.PaymentID = payment.PaymentID;
                }
            }

            if (oldStatus != reservation.Status)
                AddHistory(reservation.ReservationID, oldStatus, reservation.Status, historyNote);

            var claim = await _context.WorkItemClaims.FirstOrDefaultAsync(x =>
                x.QueueType == "Thanh toán" && x.EntityID == payment.PaymentID && x.Status == "Đang xử lý");
            if (claim is not null) claim.Status = "Đã hoàn tất";

            _context.Notifications.Add(new Notification
            {
                AppUserID = reservation.CustomerAppUserID,
                Title = confirmsReservationHold ? "Thanh toán giữ chỗ đã được xác nhận" : "Thanh toán đã được xác nhận",
                Message = confirmsReservationHold
                    ? $"SmartCar đã đối chiếu {paymentType.ToLowerInvariant()} {payment.Amount.ToString("#,0", CultureInfo.InvariantCulture)} đồng cho đơn #{reservation.ReservationID}. Lịch xe đã được khóa chính thức và đơn chuyển sang chờ giao xe."
                    : $"SmartCar đã đối chiếu khoản thanh toán {payment.Amount.ToString("#,0", CultureInfo.InvariantCulture)} đồng cho đơn #{reservation.ReservationID}.",
                Type = "Payment"
            });

            AddAudit(
                payment.IsSimulated ? "Giả lập thanh toán" : "Xác nhận thanh toán",
                nameof(Payment),
                payment.PaymentID > 0 ? payment.PaymentID.ToString() : null,
                $"{payment.TransactionCode} - {payment.Amount.ToString("#,0", CultureInfo.InvariantCulture)} - {paymentType} - {payment.VerificationNote}");
            return confirmsReservationHold;
        }

        [Authorize(Roles = "VehiclePartner,Admin,Staff")]
        [HttpPost("reservations/{reservationId:int}/handover")]
        public async Task<IActionResult> CreateHandover(int reservationId, CreateHandoverDto dto)
        {
            if (!await CanManageHandover(reservationId)) return Forbid();
            if (dto.ReportType is not ("Giao xe" or "Trả xe")) return BadRequest("Loại biên bản không hợp lệ.");
            if (dto.OdometerKm < 0) return BadRequest("Số kilomet không hợp lệ.");
            if (dto.FuelPercent is < 0 or > 100) return BadRequest("Mức nhiên liệu phải từ 0 đến 100%.");
            if (string.IsNullOrWhiteSpace(dto.LocationText)) return BadRequest("Phải nhập địa điểm giao nhận.");
            if (string.IsNullOrWhiteSpace(dto.ExistingDamage)) return BadRequest("Phải ghi tình trạng/vết xước hiện có, hoặc ghi 'Không có'.");
            if (string.IsNullOrWhiteSpace(dto.Accessories)) return BadRequest("Phải ghi danh sách phụ kiện và giấy tờ bàn giao.");
            if (dto.PhotoFileIds is null || dto.PhotoFileIds.Count < 3) return BadRequest("Phải có ít nhất 3 ảnh hợp lệ.");

            var reservation = await _context.Reservations.AsNoTracking()
                .Include(x => x.PartnerVehicle).ThenInclude(x => x.OwnerAppUser)
                .FirstOrDefaultAsync(x => x.ReservationID == reservationId);
            if (reservation is null) return NotFound("Không tìm thấy đơn thuê.");
            if (string.IsNullOrWhiteSpace(reservation.Email) || string.IsNullOrWhiteSpace(reservation.PartnerVehicle.OwnerAppUser.Email))
                return BadRequest("Đơn thuê chưa có đủ email khách và đối tác để gửi OTP độc lập.");

            if (dto.ReportType == "Giao xe")
            {
                var rentalPaid = await _context.Payments
                    .Where(x => x.ReservationID == reservationId && x.PaymentType == PaymentTypes.Rental && x.Status == "Thành công")
                    .SumAsync(x => (decimal?)x.Amount) ?? 0m;
                var reservationDepositPaid = await _context.Payments
                    .Where(x => x.ReservationID == reservationId && x.PaymentType == PaymentTypes.ReservationDeposit && x.Status == "Thành công")
                    .SumAsync(x => (decimal?)x.Amount) ?? 0m;
                var rentalCovered = rentalPaid + Math.Min(reservation.ReservationDepositAmount, reservationDepositPaid);
                if (rentalCovered < reservation.TotalPrice)
                    return Conflict($"Chưa thu đủ tiền thuê. Đã xác nhận/cấn trừ {rentalCovered.ToString("#,0", CultureInfo.InvariantCulture)}/{reservation.TotalPrice.ToString("#,0", CultureInfo.InvariantCulture)} đồng.");

                if (reservation.RentalMode == ServiceTypes.SelfDrive && reservation.SecurityDepositAmount > 0)
                {
                    var securityPaid = await _context.Payments
                        .Where(x => x.ReservationID == reservationId && x.PaymentType == PaymentTypes.SecurityDeposit && x.Status == "Thành công")
                        .SumAsync(x => (decimal?)x.Amount) ?? 0m;
                    if (securityPaid < reservation.SecurityDepositAmount)
                        return Conflict($"Chưa thu đủ cọc bảo đảm. Đã xác nhận {securityPaid.ToString("#,0", CultureInfo.InvariantCulture)}/{reservation.SecurityDepositAmount.ToString("#,0", CultureInfo.InvariantCulture)} đồng.");
                }
            }

            var allowedStatus = dto.ReportType == "Giao xe"
                ? reservation.Status is ReservationStatuses.Confirmed or ReservationStatuses.HandoverPending or "Đã đặt cọc"
                : reservation.Status is ReservationStatuses.InProgress or ReservationStatuses.ReturnPending;
            if (!allowedStatus) return Conflict("Trạng thái đơn không cho phép lập biên bản này.");

            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                if (await _context.HandoverReports.AnyAsync(x => x.ReservationID == reservationId && x.ReportType == dto.ReportType && !x.IsSuperseded))
                    return Conflict("Biên bản loại này đã tồn tại.");

                var files = await _files.ValidateForAttachmentAsync(dto.PhotoFileIds, UserId(), "HandoverEvidence", reservationId, IsPrivileged(), HttpContext.RequestAborted);
                if (files.Count < 3) return BadRequest("Phải có ít nhất 3 ảnh hợp lệ cho biên bản giao nhận.");

                var customerOtp = OtpSecurity.GenerateSixDigits();
                var partnerOtp = OtpSecurity.GenerateSixDigits();
                var now = DateTime.UtcNow;
                var report = new HandoverReport
                {
                    ReservationID = reservationId,
                    ReportType = dto.ReportType,
                    OdometerKm = dto.OdometerKm,
                    FuelPercent = dto.FuelPercent,
                    ExistingDamage = dto.ExistingDamage.Trim(),
                    Accessories = dto.Accessories.Trim(),
                    LocationText = dto.LocationText.Trim(),
                    PhotoUrls = string.Join(',', files.Select(x => _files.BuildViewUrl(x.PrivateFileID))),
                    CustomerOtpHash = HashHandoverOtp(reservationId, dto.ReportType, "Customer", reservation.Email, customerOtp),
                    PartnerOtpHash = HashHandoverOtp(reservationId, dto.ReportType, "Partner", reservation.PartnerVehicle.OwnerAppUser.Email, partnerOtp),
                    OtpHash = null,
                    OtpExpiresAt = now.AddMinutes(10),
                    CustomerOtpLastSentAt = now,
                    PartnerOtpLastSentAt = now,
                    OtpLastSentAt = now,
                    CreatedByAppUserID = UserId(),
                    CreatedDate = now
                };
                _context.HandoverReports.Add(report);
                _context.EmailOutboxes.Add(BuildHandoverOtpEmail(reservation.Email, customerOtp, reservationId, dto.ReportType, "khách thuê"));
                _context.EmailOutboxes.Add(BuildHandoverOtpEmail(reservation.PartnerVehicle.OwnerAppUser.Email, partnerOtp, reservationId, dto.ReportType, "đối tác/tài xế"));
                await _context.SaveChangesAsync(HttpContext.RequestAborted);
                _files.MarkAttached(files, nameof(HandoverReport), report.HandoverReportID.ToString());
                AddAudit("Lập biên bản hai OTP", nameof(HandoverReport), report.HandoverReportID.ToString(), $"{dto.ReportType}; gửi OTP độc lập cho khách và đối tác.");
                await _context.SaveChangesAsync(HttpContext.RequestAborted);
                await transaction.CommitAsync(HttpContext.RequestAborted);
                return Ok(new
                {
                    report.HandoverReportID,
                    ExpiresInMinutes = 10,
                    CustomerEmail = MaskEmail(reservation.Email),
                    PartnerEmail = MaskEmail(reservation.PartnerVehicle.OwnerAppUser.Email),
                    Message = "Biên bản đã tạo. Cả khách và đối tác/tài xế phải xác nhận OTP độc lập."
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                await transaction.RollbackAsync(HttpContext.RequestAborted);
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync(HttpContext.RequestAborted);
                return BadRequest(ex.Message);
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Xung đột khi tạo biên bản {ReportType} cho đơn {ReservationId}", dto.ReportType, reservationId);
                return Conflict("Biên bản đã được tạo bởi một thao tác khác. Vui lòng tải lại dữ liệu.");
            }
        }

        [Authorize(Roles = "Customer,VehiclePartner")]
        [HttpPost("handover/{id:int}/confirm")]
        public async Task<IActionResult> ConfirmHandover(int id, ConfirmOtpDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Otp) || dto.Otp.Length != 6 || !dto.Otp.All(char.IsDigit))
                return BadRequest("OTP phải gồm đúng 6 chữ số.");

            var report = await _context.HandoverReports
                .Include(x => x.Reservation).ThenInclude(x => x.PartnerVehicle).ThenInclude(x => x.OwnerAppUser)
                .FirstOrDefaultAsync(x => x.HandoverReportID == id && !x.IsSuperseded);
            if (report is null) return NotFound("Không tìm thấy biên bản.");
            if (report.IsLocked) return Conflict("Biên bản đã được hai bên xác nhận và khóa.");

            var userId = UserId();
            var party = report.Reservation.CustomerAppUserID == userId
                ? "Customer"
                : report.Reservation.PartnerVehicle.OwnerAppUserID == userId ? "Partner" : null;
            if (party is null) return Forbid();
            if (report.OtpExpiresAt < DateTime.UtcNow) return BadRequest("OTP đã hết hạn. Vui lòng yêu cầu gửi lại.");

            var isCustomer = party == "Customer";
            var lockedUntil = isCustomer ? report.CustomerOtpLockedUntil : report.PartnerOtpLockedUntil;
            var storedHash = isCustomer ? report.CustomerOtpHash : report.PartnerOtpHash;
            var targetEmail = isCustomer ? report.Reservation.Email : report.Reservation.PartnerVehicle.OwnerAppUser.Email;
            if (lockedUntil.HasValue && lockedUntil > DateTime.UtcNow)
                return StatusCode(StatusCodes.Status429TooManyRequests, "OTP của bên xác nhận đang tạm khóa do nhập sai quá nhiều lần.");
            if (string.IsNullOrWhiteSpace(storedHash))
                return Conflict(isCustomer && report.CustomerConfirmedDate.HasValue || !isCustomer && report.PartnerConfirmedDate.HasValue
                    ? "Bên này đã xác nhận trước đó."
                    : "OTP không còn hiệu lực.");

            if (!OtpMatches(report.ReservationID, report.ReportType, party, targetEmail, dto.Otp, storedHash))
            {
                if (isCustomer)
                {
                    report.CustomerOtpFailedAttempts++;
                    if (report.CustomerOtpFailedAttempts >= 5) report.CustomerOtpLockedUntil = DateTime.UtcNow.AddMinutes(15);
                }
                else
                {
                    report.PartnerOtpFailedAttempts++;
                    if (report.PartnerOtpFailedAttempts >= 5) report.PartnerOtpLockedUntil = DateTime.UtcNow.AddMinutes(15);
                }
                await _context.SaveChangesAsync();
                return BadRequest("OTP không đúng.");
            }

            if (report.ReportType == "Giao xe")
            {
                var rentalPaid = await _context.Payments
                    .Where(x => x.ReservationID == report.ReservationID && x.PaymentType == "Tiền thuê" && x.Status == "Thành công")
                    .SumAsync(x => (decimal?)x.Amount) ?? 0m;
                if (rentalPaid != report.Reservation.TotalPrice) return Conflict("Chưa thu đủ tiền thuê nên không thể xác nhận giao xe.");
            }

            if (isCustomer)
            {
                report.CustomerConfirmedDate = DateTime.UtcNow;
                report.CustomerOtpHash = null;
                report.CustomerOtpFailedAttempts = 0;
                report.CustomerOtpLockedUntil = null;
            }
            else
            {
                report.PartnerConfirmedDate = DateTime.UtcNow;
                report.PartnerOtpHash = null;
                report.PartnerOtpFailedAttempts = 0;
                report.PartnerOtpLockedUntil = null;
            }

            AddAudit("Xác nhận một bên biên bản", nameof(HandoverReport), id.ToString(), $"{report.ReportType}; Party={party}");
            var bothConfirmed = report.CustomerConfirmedDate.HasValue && report.PartnerConfirmedDate.HasValue;
            if (bothConfirmed)
            {
                report.IsLocked = true;
                report.ConfirmedDate = DateTime.UtcNow;
                var oldStatus = report.Reservation.Status;
                if (report.ReportType == "Giao xe")
                {
                    report.Reservation.Status = ReservationStatuses.InProgress;
                }
                else
                {
                    report.Reservation.Status = ReservationStatuses.SurchargeProposalPending;
                    report.Reservation.SurchargeProposalExpiresAt = DateTime.UtcNow.AddHours(24);
                }
                AddHistory(report.ReservationID, oldStatus, report.Reservation.Status,
                    $"Cả khách và đối tác/tài xế đã xác nhận biên bản {report.ReportType} bằng OTP độc lập.");
            }
            await _context.SaveChangesAsync();
            return Ok(bothConfirmed
                ? "Hai bên đã xác nhận; biên bản được khóa và đơn đã chuyển bước tiếp theo."
                : "Xác nhận của bạn thành công. Đang chờ bên còn lại xác nhận OTP.");
        }

        [HttpPost("handover/{id:int}/resend-otp")]
        public async Task<IActionResult> ResendHandoverOtp(int id)
        {
            var report = await _context.HandoverReports
                .Include(x => x.Reservation).ThenInclude(x => x.PartnerVehicle).ThenInclude(x => x.OwnerAppUser)
                .FirstOrDefaultAsync(x => x.HandoverReportID == id && !x.IsSuperseded);
            if (report is null) return NotFound("Không tìm thấy biên bản.");
            if (report.IsLocked) return Conflict("Biên bản đã được xác nhận.");

            var userId = UserId();
            var isCustomer = report.Reservation.CustomerAppUserID == userId;
            var isPartner = report.Reservation.PartnerVehicle.OwnerAppUserID == userId;
            if (!isCustomer && !isPartner) return Forbid();
            if (isCustomer && report.CustomerConfirmedDate.HasValue || isPartner && report.PartnerConfirmedDate.HasValue)
                return Conflict("Bên này đã xác nhận, không cần gửi lại OTP.");

            var now = DateTime.UtcNow;
            var lastSent = isCustomer ? report.CustomerOtpLastSentAt : report.PartnerOtpLastSentAt;
            if (lastSent.HasValue && lastSent.Value.AddMinutes(1) > now)
                return StatusCode(StatusCodes.Status429TooManyRequests, "Vui lòng chờ 60 giây trước khi gửi lại OTP.");

            var party = isCustomer ? "Customer" : "Partner";
            var email = isCustomer ? report.Reservation.Email : report.Reservation.PartnerVehicle.OwnerAppUser.Email;
            var otp = OtpSecurity.GenerateSixDigits();
            var hash = HashHandoverOtp(report.ReservationID, report.ReportType, party, email, otp);
            if (isCustomer)
            {
                report.CustomerOtpHash = hash;
                report.CustomerOtpLastSentAt = now;
                report.CustomerOtpFailedAttempts = 0;
                report.CustomerOtpLockedUntil = null;
            }
            else
            {
                report.PartnerOtpHash = hash;
                report.PartnerOtpLastSentAt = now;
                report.PartnerOtpFailedAttempts = 0;
                report.PartnerOtpLockedUntil = null;
            }
            report.OtpExpiresAt = now.AddMinutes(10);
            _context.EmailOutboxes.Add(BuildHandoverOtpEmail(email, otp, report.ReservationID, report.ReportType, isCustomer ? "khách thuê" : "đối tác/tài xế"));
            await _context.SaveChangesAsync();
            return Ok(new { Message = "OTP riêng của bạn đã được đưa vào hàng đợi gửi email.", ExpiresInMinutes = 10, MaskedEmail = MaskEmail(email) });
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost("handover/{id:int}/exception-confirm")]
        public async Task<IActionResult> ConfirmHandoverException(int id, HandoverExceptionConfirmDto dto)
        {
            if (dto.Party is not ("Customer" or "Partner")) return BadRequest("Party phải là Customer hoặc Partner.");
            if (string.IsNullOrWhiteSpace(dto.Reason)) return BadRequest("Bắt buộc nhập lý do xử lý ngoại lệ.");
            var report = await _context.HandoverReports.Include(x => x.Reservation).FirstOrDefaultAsync(x => x.HandoverReportID == id && !x.IsSuperseded);
            if (report is null) return NotFound("Không tìm thấy biên bản.");
            if (report.IsLocked) return Conflict("Biên bản đã khóa.");
            if (dto.Party == "Customer") report.CustomerConfirmedDate ??= DateTime.UtcNow;
            else report.PartnerConfirmedDate ??= DateTime.UtcNow;
            AddAudit("Xác nhận biên bản ngoại lệ", nameof(HandoverReport), id.ToString(), $"Party={dto.Party}; Reason={dto.Reason.Trim()}");
            if (report.CustomerConfirmedDate.HasValue && report.PartnerConfirmedDate.HasValue)
            {
                report.IsLocked = true;
                report.ConfirmedDate = DateTime.UtcNow;
                var old = report.Reservation.Status;
                report.Reservation.Status = report.ReportType == "Giao xe" ? ReservationStatuses.InProgress : ReservationStatuses.SurchargeProposalPending;
                if (report.ReportType == "Trả xe") report.Reservation.SurchargeProposalExpiresAt = DateTime.UtcNow.AddHours(24);
                AddHistory(report.ReservationID, old, report.Reservation.Status, $"Hoàn tất biên bản có xác nhận ngoại lệ: {dto.Reason.Trim()}");
            }
            await _context.SaveChangesAsync();
            return Ok("Đã ghi nhận xác nhận ngoại lệ và audit log.");
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("verifications/{id:int}/review")]
        public async Task<IActionResult> ReviewVerification(int id, ReviewVerificationDto dto)
        {
            if (dto.Status is not ("Đã xác minh" or "Bị từ chối" or "Yêu cầu bổ sung"))
                return BadRequest("Trạng thái duyệt không hợp lệ.");
            if ((dto.Status is "Bị từ chối" or "Yêu cầu bổ sung") && string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest("Phải nhập lý do hoặc nội dung cần bổ sung.");

            var item = await _context.UserVerifications.FindAsync(id);
            if (item is null) return NotFound("Không tìm thấy hồ sơ xác minh.");
            item.Status = dto.Status;
            item.RejectionReason = dto.Status == "Đã xác minh" ? null : dto.Reason?.Trim();
            item.ReviewedByAppUserID = UserId();
            item.ReviewedDate = DateTime.UtcNow;
            // Đóng mọi claim còn hoạt động của hồ sơ để tránh claim trùng/stale tiếp tục
            // xuất hiện ở hàng đợi sau khi nhân viên đã kết luận.
            var verificationClaims = await _context.WorkItemClaims
                .Where(x => x.QueueType == "Xác minh khách" && x.EntityID == id && x.Status != "Đã hoàn tất")
                .ToListAsync();
            foreach (var verificationClaim in verificationClaims)
                verificationClaim.Status = "Đã hoàn tất";
            _context.Notifications.Add(new Notification
            {
                AppUserID = item.AppUserID,
                Title = dto.Status == "Đã xác minh" ? "Hồ sơ đã được xác minh" : "Hồ sơ cần cập nhật",
                Message = dto.Status == "Đã xác minh" ? "Bạn đã đủ điều kiện đặt xe." : (dto.Reason?.Trim() ?? dto.Status),
                Type = "Verification"
            });
            AddAudit("Duyệt xác minh", nameof(UserVerification), id.ToString(), dto.Status + (string.IsNullOrWhiteSpace(dto.Reason) ? string.Empty : $" - {(dto.Reason ?? string.Empty).Trim()}"));
            await _context.SaveChangesAsync();
            return Ok("Đã cập nhật kết quả xác minh.");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("audit-logs")]
        public async Task<IActionResult> AuditLogs([FromQuery] int take = 100) =>
            Ok(await _context.AuditLogs.AsNoTracking()
                .OrderByDescending(x => x.CreatedDate)
                .Take(Math.Clamp(take, 1, 500))
                .ToListAsync());

        private static EmailOutbox BuildHandoverOtpEmail(string recipient, string otp, int reservationId, string reportType, string partyLabel) => new()
        {
            MessageKey = $"handover-otp:{reservationId}:{reportType}:{partyLabel}:{Guid.NewGuid():N}",
            RecipientEmail = recipient,
            Subject = $"[SmartCar] OTP {reportType} dành cho {partyLabel} - Đơn #{reservationId}",
            Body = $"<h2>Mã OTP SmartCar</h2><p>Đây là mã xác nhận riêng của <b>{WebUtility.HtmlEncode(partyLabel)}</b> cho biên bản {WebUtility.HtmlEncode(reportType)} đơn <b>#{reservationId}</b>:</p><p style='font-size:30px;font-weight:bold;letter-spacing:8px'>{otp}</p><p>Mã có hiệu lực 10 phút, chỉ dùng một lần. Hai bên phải xác nhận bằng hai OTP độc lập.</p>",
            Status = "Pending",
            CreatedDate = DateTime.UtcNow
        };

        private string HashHandoverOtp(int reservationId, string reportType, string party, string targetEmail, string otp)
            => OtpSecurity.Hash(OtpKey(), reservationId.ToString(), $"Handover:{reportType}:{party}", targetEmail, otp);

        private bool OtpMatches(int reservationId, string reportType, string party, string targetEmail, string otp, string storedHash)
            => OtpSecurity.Verify(OtpKey(), reservationId.ToString(), $"Handover:{reportType}:{party}", targetEmail, otp, storedHash);

        private string OtpKey()
            => _configuration["Security:OtpHmacKey"]
               ?? throw new InvalidOperationException("Thiếu Security:OtpHmacKey.");

        private static string MaskEmail(string email)
        {
            var parts = email.Split('@');
            if (parts.Length != 2 || parts[0].Length < 2) return "***";
            return $"{parts[0][0]}***{parts[0][^1]}@{parts[1]}";
        }

        private bool IsPrivileged() => User.IsInRole("Admin") || User.IsInRole("Staff");

        private int UserId() =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        private async Task<bool> CanAccessReservation(int id)
        {
            if (User.IsInRole("Admin") || User.IsInRole("Staff")) return true;
            var uid = UserId();
            return uid > 0 && await _context.Reservations.AnyAsync(x =>
                x.ReservationID == id &&
                (x.CustomerAppUserID == uid || x.PartnerVehicle.OwnerAppUserID == uid));
        }

        private async Task<bool> CanManageHandover(int id)
        {
            if (User.IsInRole("Admin") || User.IsInRole("Staff")) return true;
            if (!string.Equals(User.FindFirstValue("IsVehiclePartner"), "true", StringComparison.OrdinalIgnoreCase))
                return false;
            var uid = UserId();
            return uid > 0 && await _context.Reservations.AnyAsync(x =>
                x.ReservationID == id && x.PartnerVehicle.OwnerAppUserID == uid);
        }

        private void AddHistory(int reservationId, string oldStatus, string newStatus, string? note) =>
            _context.ReservationStatusHistories.Add(new ReservationStatusHistory
            {
                ReservationID = reservationId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedByAppUserID = UserId(),
                Note = note
            });

        private void AddAudit(string action, string entity, string? entityId, string? note) =>
            _context.AuditLogs.Add(new AuditLog
            {
                AppUserID = UserId(),
                Action = action,
                EntityName = entity,
                EntityID = entityId,
                Note = note,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
    }

    public record CreateDisputeDto(string? Type, string Description, IReadOnlyList<Guid>? EvidenceFileIds);
    public record AddDisputeMessageDto(string Message, IReadOnlyList<Guid>? EvidenceFileIds);
    public record ResolveDisputeDto(string? Resolution, decimal CompensationAmount);
    public record ConfirmPaymentDto(int PaymentID, decimal Amount, string? PaymentType, string? TransactionCode, string? Provider, string? VerificationNote);
    public record ReviewPaymentDto(int PaymentID, string? Decision, string? Note);
    public record CreateHandoverDto(string ReportType, int OdometerKm, int FuelPercent, string? ExistingDamage, string? Accessories, string? LocationText, IReadOnlyList<Guid>? PhotoFileIds);
    public record ConfirmOtpDto(string? Otp);
    public record HandoverExceptionConfirmDto(string Party, string Reason);
    public record ReviewVerificationDto(string Status, string? Reason);
}
