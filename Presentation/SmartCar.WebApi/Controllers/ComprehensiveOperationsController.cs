using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.Entities;
using SmartCar.Domain.BusinessRules;
using SmartCar.Domain.Policies;
using SmartCar.Domain.Time;
using SmartCar.Persistence.Context;
using SmartCar.WebApi.Services;
using System.Security.Claims;

namespace SmartCar.WebApi.Controllers
{
    [ApiController]
    [Route("api/comprehensive-operations")]
    [Authorize]
    public class ComprehensiveOperationsController : ControllerBase
    {
        private static readonly string[] AllowedDepositTypes = ["Thu cọc", "Hoàn cọc", "Khấu trừ"];
        private readonly CarBookContext _db;
        private readonly IPrivateFileService _files;
        public ComprehensiveOperationsController(CarBookContext db, IPrivateFileService files)
        {
            _db = db;
            _files = files;
        }
        private int UserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        private bool IsPrivileged() => User.IsInRole("Admin") || User.IsInRole("Staff");

        [Authorize(Roles = "VehiclePartner,Admin,Staff")]
        [HttpPost("vehicles/{partnerVehicleId:int}/documents")]
        public async Task<IActionResult> AddVehicleDocument(int partnerVehicleId, VehicleDocumentDto dto, CancellationToken cancellationToken)
        {
            if (!await CanManageVehicle(partnerVehicleId)) return Forbid();
            if (string.IsNullOrWhiteSpace(dto.DocumentType) || string.IsNullOrWhiteSpace(dto.DocumentNumber) || dto.FileId == Guid.Empty)
                return BadRequest("Thiếu thông tin giấy tờ.");
            if (dto.ExpiryDate.HasValue && dto.ExpiryDate.Value.Date <= VietnamTime.Today)
                return BadRequest("Giấy tờ đã hết hạn.");

            try
            {
                var documentCategory = ResolveVehicleDocumentCategory(dto.DocumentType);
                var files = await _files.ValidateForAttachmentAsync(
                    new[] { dto.FileId }, UserId(), documentCategory, null, IsPrivileged(), cancellationToken);
                await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
                var item = new VehicleDocument
                {
                    PartnerVehicleID = partnerVehicleId,
                    DocumentType = (dto.DocumentType ?? string.Empty).Trim(),
                    DocumentNumber = (dto.DocumentNumber ?? string.Empty).Trim(),
                    FileUrl = _files.BuildViewUrl(dto.FileId),
                    IssuedDate = dto.IssuedDate,
                    ExpiryDate = dto.ExpiryDate
                };
                _db.VehicleDocuments.Add(item);
                await _db.SaveChangesAsync(cancellationToken);
                _files.MarkAttached(files, nameof(VehicleDocument), item.VehicleDocumentID.ToString());
                Audit("Thêm giấy tờ xe", nameof(VehicleDocument), item.VehicleDocumentID.ToString(), item.DocumentType);
                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return Ok(item);
            }
            catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("vehicle-documents/{id:int}/review")]
        public async Task<IActionResult> ReviewVehicleDocument(int id, ReviewDocumentDto dto)
        {
            if (dto.Status is not ("Đã xác minh" or "Bị từ chối")) return BadRequest("Trạng thái không hợp lệ.");
            if (dto.Status == "Bị từ chối" && string.IsNullOrWhiteSpace(dto.Reason)) return BadRequest("Phải nhập lý do từ chối giấy tờ.");
            var item = await _db.VehicleDocuments.FindAsync(id); if (item is null) return NotFound();
            item.Status = dto.Status; item.RejectionReason = dto.Status == "Bị từ chối" ? dto.Reason?.Trim() : null; item.ReviewedByAppUserID = UserId(); item.ReviewedDate = DateTime.UtcNow;
            var documentClaim = await _db.WorkItemClaims.FirstOrDefaultAsync(x => x.QueueType == "Giấy tờ xe" && x.EntityID == id && x.Status == "Đang xử lý");
            if (documentClaim is not null) documentClaim.Status = "Đã hoàn tất";
            Audit("Duyệt giấy tờ xe", nameof(VehicleDocument), id.ToString(), dto.Status); await _db.SaveChangesAsync(); return Ok();
        }

        [Authorize(Roles = "VehiclePartner,Admin,Staff")]
        [HttpPost("vehicles/{partnerVehicleId:int}/maintenance")]
        public async Task<IActionResult> AddMaintenance(int partnerVehicleId, MaintenanceDto dto)
        {
            if (!await CanManageVehicle(partnerVehicleId)) return Forbid();
            if (string.IsNullOrWhiteSpace(dto.WorkPerformed)) return BadRequest("Vui lòng nhập nội dung bảo dưỡng.");
            if (dto.OdometerKm < 0 || dto.NextMaintenanceKm < 0 || dto.Cost < 0) return BadRequest("Số kilomet hoặc chi phí không hợp lệ.");
            if (dto.MaintenanceDate.Date > VietnamTime.Today) return BadRequest("Ngày bảo dưỡng không được ở tương lai.");
            var item = new MaintenanceRecord { PartnerVehicleID = partnerVehicleId, MaintenanceDate = dto.MaintenanceDate, OdometerKm = dto.OdometerKm, NextMaintenanceKm = dto.NextMaintenanceKm, NextMaintenanceDate = dto.NextMaintenanceDate, WorkPerformed = (dto.WorkPerformed ?? string.Empty).Trim(), Garage = dto.Garage?.Trim(), Cost = dto.Cost, HasUnresolvedSafetyIssue = dto.HasUnresolvedSafetyIssue, SafetyIssueNote = dto.SafetyIssueNote?.Trim() };
            _db.MaintenanceRecords.Add(item); Audit("Ghi bảo dưỡng", nameof(MaintenanceRecord), null, dto.WorkPerformed);
            await _db.SaveChangesAsync(); return Ok(item);
        }

        [Authorize(Roles = "Customer,VehiclePartner")]
        [HttpPost("reservations/{reservationId:int}/incidents")]
        public async Task<IActionResult> ReportIncident(int reservationId, IncidentDto dto)
        {
            if (!await CanAccessReservation(reservationId)) return Forbid();
            if (string.IsNullOrWhiteSpace(dto.Type) || string.IsNullOrWhiteSpace(dto.Description)) return BadRequest("Vui lòng nhập loại và mô tả sự cố.");
            var reservation = await _db.Reservations.FindAsync(reservationId); if (reservation is null) return NotFound();
            if (reservation.Status is not ("Đang thuê" or "Chờ trả xe")) return Conflict("Chỉ báo sự cố khi đơn đang thuê.");
            var rentalStartUtc = VietnamTime.LocalToUtc(reservation.PickUpDate, reservation.PickUpTime);
            var rentalEndUtc = VietnamTime.LocalToUtc(reservation.DropOffDate, reservation.DropOffTime);
            if (!VietnamTime.TryNormalizeUtcInput(dto.OccurredAt, out var occurredAtUtc))
                return BadRequest("Thời điểm sự cố phải có múi giờ hoặc hậu tố Z (UTC).");
            if (occurredAtUtc < rentalStartUtc.AddHours(-1) || occurredAtUtc > DateTime.UtcNow.AddMinutes(5) || occurredAtUtc > rentalEndUtc.AddHours(24))
                return BadRequest("Thời điểm xảy ra sự cố không hợp lệ.");
            try
            {
                var files = await _files.ValidateForAttachmentAsync(dto.EvidenceFileIds, UserId(), "IncidentEvidence", reservationId, IsPrivileged(), HttpContext.RequestAborted);
                await using var transaction = await _db.Database.BeginTransactionAsync(HttpContext.RequestAborted);
                var old = reservation.Status;
                reservation.Status = "Đang xử lý sự cố";
                var item = new Incident
                {
                    ReservationID = reservationId,
                    ReportedByAppUserID = UserId(),
                    Type = (dto.Type ?? string.Empty).Trim(),
                    Description = (dto.Description ?? string.Empty).Trim(),
                    LocationText = dto.LocationText?.Trim(),
                    EvidenceUrls = files.Count == 0 ? null : string.Join(',', files.Select(x => _files.BuildViewUrl(x.PrivateFileID))),
                    VehicleImmobilized = dto.VehicleImmobilized,
                    PoliceInvolved = dto.PoliceInvolved,
                    InsuranceNotified = dto.InsuranceNotified,
                    EstimatedDamage = Math.Max(0, dto.EstimatedDamage),
                    OccurredAt = occurredAtUtc
                };
                _db.Incidents.Add(item);
                History(reservationId, old, reservation.Status, "Báo sự cố xe.");
                await _db.SaveChangesAsync(HttpContext.RequestAborted);
                _files.MarkAttached(files, nameof(Incident), item.IncidentID.ToString());
                Audit("Báo sự cố", nameof(Incident), item.IncidentID.ToString(), dto.Type);
                await _db.SaveChangesAsync(HttpContext.RequestAborted);
                await transaction.CommitAsync(HttpContext.RequestAborted);
                return Ok(item);
            }
            catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("incidents/{id:int}/resolve")]
        public async Task<IActionResult> ResolveIncident(int id, ResolveIncidentDto dto)
        {
            var item = await _db.Incidents.Include(x => x.Reservation).FirstOrDefaultAsync(x => x.IncidentID == id); if (item is null) return NotFound();
            item.Status = "Đã giải quyết"; item.CustomerLiability = Math.Max(0, dto.CustomerLiability); item.ResolvedDate = DateTime.UtcNow;
            var old = item.Reservation.Status; item.Reservation.Status = dto.OpenDispute ? "Đang tranh chấp" : "Chờ trả xe";
            var incidentClaim = await _db.WorkItemClaims.FirstOrDefaultAsync(x => x.QueueType == "Sự cố" && x.EntityID == id && x.Status == "Đang xử lý");
            if (incidentClaim is not null) incidentClaim.Status = "Đã hoàn tất";
            History(item.ReservationID, old, item.Reservation.Status, dto.Note); Audit("Giải quyết sự cố", nameof(Incident), id.ToString(), dto.Note);
            await _db.SaveChangesAsync(); return Ok();
        }

        [Authorize(Roles = "VehiclePartner,Admin,Staff")]
        [HttpPost("reservations/{reservationId:int}/traffic-fines")]
        public async Task<IActionResult> AddTrafficFine(int reservationId, TrafficFineDto dto)
        {
            if (!await CanManageReservationAsOwner(reservationId)) return Forbid();
            if (string.IsNullOrWhiteSpace(dto.Violation) || dto.Amount <= 0) return BadRequest("Vui lòng nhập lỗi vi phạm và số tiền phạt hợp lệ.");
            var reservation = await _db.Reservations.FindAsync(reservationId); if (reservation is null) return NotFound();
            var startUtc = VietnamTime.LocalToUtc(reservation.PickUpDate, reservation.PickUpTime);
            var endUtc = VietnamTime.LocalToUtc(reservation.DropOffDate, reservation.DropOffTime);
            if (!VietnamTime.TryNormalizeUtcInput(dto.ViolationAt, out var violationAtUtc))
                return BadRequest("Thời điểm vi phạm phải có múi giờ hoặc hậu tố Z (UTC).");
            if (violationAtUtc < startUtc || violationAtUtc > endUtc) return BadRequest("Thời điểm vi phạm không nằm trong thời gian thuê.");
            if (!string.IsNullOrWhiteSpace(dto.NoticeNumber) && await _db.TrafficFines.AnyAsync(x => x.NoticeNumber == dto.NoticeNumber)) return Conflict("Thông báo phạt đã tồn tại.");
            try
            {
                var ids = dto.EvidenceFileId.HasValue ? new[] { dto.EvidenceFileId.Value } : Array.Empty<Guid>();
                var files = await _files.ValidateForAttachmentAsync(ids, UserId(), "TrafficFineEvidence", reservationId, IsPrivileged(), HttpContext.RequestAborted);
                await using var transaction = await _db.Database.BeginTransactionAsync(HttpContext.RequestAborted);
                var item = new TrafficFine
                {
                    ReservationID = reservationId,
                    ViolationAt = violationAtUtc,
                    Violation = (dto.Violation ?? string.Empty).Trim(),
                    LocationText = dto.LocationText?.Trim(),
                    Amount = Math.Max(0, dto.Amount),
                    NoticeNumber = dto.NoticeNumber?.Trim(),
                    EvidenceUrl = files.Count == 0 ? null : _files.BuildViewUrl(files[0].PrivateFileID),
                    DueDate = dto.DueDate
                };
                _db.TrafficFines.Add(item);
                Notify(reservation.CustomerAppUserID, "Phạt nguội mới", $"Đơn #{reservationId} có khoản phạt {item.Amount.ToString("#,0", CultureInfo.InvariantCulture)} đồng.", "TrafficFine");
                await _db.SaveChangesAsync(HttpContext.RequestAborted);
                _files.MarkAttached(files, nameof(TrafficFine), item.TrafficFineID.ToString());
                Audit("Nhập phạt nguội", nameof(TrafficFine), item.TrafficFineID.ToString(), dto.NoticeNumber);
                await _db.SaveChangesAsync(HttpContext.RequestAborted);
                await transaction.CommitAsync(HttpContext.RequestAborted);
                return Ok(item);
            }
            catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }

        [Authorize(Roles = "VehiclePartner,Admin,Staff")]
        [HttpPost("reservations/{reservationId:int}/additional-charges")]
        public async Task<IActionResult> AddCharge(int reservationId, ChargeDto dto)
        {
            if (!await CanManageReservationAsOwner(reservationId)) return Forbid();
            if (dto.Amount <= 0) return BadRequest("Phụ phí phải lớn hơn 0.");
            if (string.IsNullOrWhiteSpace(dto.ChargeType) || string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest("Phải nhập loại và lý do phụ phí.");

            var reservation = await _db.Reservations.FindAsync(reservationId);
            if (reservation is null) return NotFound();
            if (reservation.Status is ReservationStatuses.Cancelled or ReservationStatuses.Completed)
                return Conflict("Đơn đã đóng, không thể tạo phụ phí.");
            if (reservation.Status is not (ReservationStatuses.SurchargeProposalPending or ReservationStatuses.SurchargeResponsePending or ReservationStatuses.Disputed))
                return Conflict("Chỉ được đề xuất phụ phí sau khi hoàn tất biên bản trả xe.");
            if (reservation.SurchargeProposalExpiresAt.HasValue && reservation.SurchargeProposalExpiresAt.Value < DateTime.UtcNow)
                return Conflict("Đã hết thời hạn 24 giờ để đề xuất phụ phí.");
            if (await _db.Settlements.AnyAsync(x => x.ReservationID == reservationId))
                return Conflict("Đơn đã lập đối soát, không thể tạo thêm phụ phí.");

            try
            {
                var files = await _files.ValidateForAttachmentAsync(dto.EvidenceFileIds, UserId(), "AdditionalChargeEvidence", reservationId, IsPrivileged(), HttpContext.RequestAborted);
                await using var transaction = await _db.Database.BeginTransactionAsync(HttpContext.RequestAborted);
                var now = DateTime.UtcNow;
                var item = new AdditionalCharge
                {
                    ReservationID = reservationId,
                    ChargeType = (dto.ChargeType ?? string.Empty).Trim(),
                    Amount = dto.Amount,
                    Reason = (dto.Reason ?? string.Empty).Trim(),
                    EvidenceUrls = files.Count == 0 ? null : string.Join(',', files.Select(x => _files.BuildViewUrl(x.PrivateFileID))),
                    CreatedByAppUserID = UserId(),
                    Status = AdditionalChargeStatuses.Submitted,
                    SubmittedDate = now,
                    CreatedDate = now
                };
                _db.AdditionalCharges.Add(item);
                var oldStatus = reservation.Status;
                reservation.Status = ReservationStatuses.SurchargeResponsePending;
                reservation.SurchargeResponseExpiresAt = now.AddHours(24);
                reservation.StateVersion++;
                if (oldStatus != reservation.Status)
                    History(reservationId, oldStatus, reservation.Status, "Đối tác đã gửi đề xuất phụ phí; khách có 24 giờ phản hồi.");
                Notify(reservation.CustomerAppUserID, "Yêu cầu phản hồi phụ phí", $"Phụ phí {dto.ChargeType}: {dto.Amount.ToString("#,#", CultureInfo.InvariantCulture)} đồng. Vui lòng phản hồi trong 24 giờ.", "Charge");
                await _db.SaveChangesAsync(HttpContext.RequestAborted);
                _files.MarkAttached(files, nameof(AdditionalCharge), item.AdditionalChargeID.ToString());
                Audit("Gửi đề xuất phụ phí", nameof(AdditionalCharge), item.AdditionalChargeID.ToString(), dto.ChargeType);
                await _db.SaveChangesAsync(HttpContext.RequestAborted);
                await transaction.CommitAsync(HttpContext.RequestAborted);
                return Ok(item);
            }
            catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }

        [Authorize(Roles = "Customer")]
        [HttpPut("additional-charges/{id:int}/respond")]
        public async Task<IActionResult> RespondCharge(int id, ChargeResponseDto dto)
        {
            var item = await _db.AdditionalCharges.Include(x => x.Reservation).ThenInclude(x => x.PartnerVehicle)
                .FirstOrDefaultAsync(x => x.AdditionalChargeID == id);
            if (item is null) return NotFound();
            if (item.Reservation.CustomerAppUserID != UserId()) return Forbid();
            if (await _db.Settlements.AnyAsync(x => x.ReservationID == item.ReservationID))
                return Conflict("Đơn đã lập đối soát, không thể thay đổi phụ phí.");
            if (item.Status != AdditionalChargeStatuses.Submitted)
                return Conflict("Phụ phí đã được phản hồi hoặc chuyển xử lý.");
            if (item.Reservation.SurchargeResponseExpiresAt.HasValue && item.Reservation.SurchargeResponseExpiresAt.Value < DateTime.UtcNow)
                return Conflict("Đã hết thời hạn phản hồi; phụ phí đã được chuyển nhân viên kiểm tra.");

            var now = DateTime.UtcNow;
            item.CustomerResponseDate = now;
            if (dto.Accept)
            {
                item.Status = AdditionalChargeStatuses.CustomerAccepted;
                item.ResolvedDate = now;
                item.ResolvedByAppUserID = UserId();
                Notify(item.Reservation.PartnerVehicle.OwnerAppUserID, "Khách đã chấp nhận phụ phí", $"Phụ phí #{id} đã được khách chấp nhận.", "Charge");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.Note)) return BadRequest("Bắt buộc nhập lý do không đồng ý phụ phí.");
                item.Status = AdditionalChargeStatuses.CustomerRejected;
                var old = item.Reservation.Status;
                item.Reservation.Status = ReservationStatuses.Disputed;
                item.Reservation.StateVersion++;
                History(item.ReservationID, old, item.Reservation.Status, dto.Note?.Trim());
                Notify(item.Reservation.PartnerVehicle.OwnerAppUserID, "Khách không đồng ý phụ phí", dto.Note?.Trim() ?? "Khách yêu cầu SmartCar kiểm tra.", "Charge");
            }
            Audit("Phản hồi phụ phí", nameof(AdditionalCharge), id.ToString(), item.Status);
            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("additional-charges/{id:int}/review")]
        public async Task<IActionResult> ReviewCharge(int id, ReviewChargeDto dto)
        {
            var item = await _db.AdditionalCharges.Include(x => x.Reservation)
                .FirstOrDefaultAsync(x => x.AdditionalChargeID == id);
            if (item is null) return NotFound();
            if (item.Status is not (AdditionalChargeStatuses.CustomerRejected or AdditionalChargeStatuses.StaffReview))
                return Conflict("Phụ phí không ở trạng thái cần nhân viên kiểm tra.");
            if (string.IsNullOrWhiteSpace(dto.Reason)) return BadRequest("Bắt buộc nhập kết luận và căn cứ xử lý.");
            if (item.Amount > 2_000_000m && !User.IsInRole("Admin"))
                return StatusCode(StatusCodes.Status403Forbidden, "Phụ phí trên 2.000.000 đồng phải do Admin phê duyệt.");

            item.Status = dto.Approve ? AdditionalChargeStatuses.Approved : AdditionalChargeStatuses.Rejected;
            item.ResolvedByAppUserID = UserId();
            item.ResolvedDate = DateTime.UtcNow;
            item.Reason = $"{item.Reason} | Kết luận: {dto.Reason.Trim()}";

            var stillOpen = await _db.AdditionalCharges.AnyAsync(x => x.ReservationID == item.ReservationID && x.AdditionalChargeID != id &&
                (x.Status == AdditionalChargeStatuses.Submitted || x.Status == AdditionalChargeStatuses.CustomerRejected || x.Status == AdditionalChargeStatuses.StaffReview));
            if (!stillOpen)
            {
                var old = item.Reservation.Status;
                item.Reservation.Status = ReservationStatuses.SettlementPending;
                item.Reservation.StateVersion++;
                History(item.ReservationID, old, item.Reservation.Status, "Đã xử lý xong toàn bộ phụ phí.");
            }
            Audit(dto.Approve ? "Duyệt phụ phí" : "Từ chối phụ phí", nameof(AdditionalCharge), id.ToString(), dto.Reason.Trim());
            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost("reservations/{reservationId:int}/deposits")]
        public async Task<IActionResult> DepositAction(int reservationId, DepositDto dto)
        {
            var reservation = await _db.Reservations.FindAsync(reservationId); if (reservation is null) return NotFound();
            var depositType = dto.Type?.Trim() ?? string.Empty;
            if (!AllowedDepositTypes.Contains(depositType, StringComparer.Ordinal)) return BadRequest("Loại giao dịch cọc không hợp lệ.");
            // DepositTransaction chỉ quản lý cọc bảo đảm tài sản. Cọc giữ chỗ được ghi nhận
            // bằng Payment và cấn trừ vào tiền thuê, không hoàn/khấu trừ qua luồng này.
            var securityDepositAmount = reservation.SecurityDepositAmount > 0
                ? reservation.SecurityDepositAmount
                : reservation.ReservationDepositAmount <= 0 ? reservation.DepositAmount : 0m;
            if (securityDepositAmount <= 0) return Conflict("Đơn không có cọc bảo đảm tài sản cần xử lý.");
            if (dto.Amount <= 0 || dto.Amount > securityDepositAmount) return BadRequest("Số tiền cọc bảo đảm không hợp lệ.");
            if (depositType == "Thu cọc" && dto.Amount != securityDepositAmount) return BadRequest($"Số tiền thu cọc bảo đảm phải bằng {securityDepositAmount.ToString("#,0", CultureInfo.InvariantCulture)} đồng.");
            if (depositType != "Thu cọc" && reservation.DepositStatus == "Chưa đặt cọc") return Conflict("Chưa thể hoàn hoặc khấu trừ khi đơn chưa thu cọc bảo đảm.");
            if (depositType == "Khấu trừ" && string.IsNullOrWhiteSpace(dto.Reason)) return BadRequest("Phải nhập lý do khấu trừ tiền cọc.");
            var transactionCode = string.IsNullOrWhiteSpace(dto.TransactionCode) ? null : (dto.TransactionCode ?? string.Empty).Trim();
            if (transactionCode is not null && await _db.DepositTransactions.AnyAsync(x => x.TransactionCode == transactionCode)) return Conflict("Mã giao dịch cọc đã tồn tại.");
            if (await _db.DepositTransactions.AnyAsync(x => x.ReservationID == reservationId && x.Type == depositType && x.Status == "Hoàn thành")) return Conflict("Loại giao dịch cọc này đã được xử lý cho đơn.");
            var item = new DepositTransaction { ReservationID = reservationId, Type = depositType, Amount = dto.Amount, Status = "Hoàn thành", Reason = dto.Reason?.Trim(), TransactionCode = transactionCode, CreatedByAppUserID = UserId(), CompletedDate = DateTime.UtcNow };
            _db.DepositTransactions.Add(item);
            reservation.DepositStatus = depositType switch { "Thu cọc" => "Đã đặt cọc", "Hoàn cọc" => "Đã hoàn cọc", "Khấu trừ" => "Đã khấu trừ", _ => reservation.DepositStatus };
            Audit("Xử lý tiền cọc", nameof(DepositTransaction), null, depositType); await _db.SaveChangesAsync(); return Ok(item);
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost("reservations/{reservationId:int}/settlement")]
        public async Task<IActionResult> CreateSettlement(int reservationId)
        {
            var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault()?.Trim();
            if (string.IsNullOrWhiteSpace(idempotencyKey)) idempotencyKey = $"settlement-create:{reservationId}";
            if (idempotencyKey.Length > 100) return BadRequest("Idempotency-Key tối đa 100 ký tự.");

            await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var existing = await _db.Settlements.FirstOrDefaultAsync(x => x.ReservationID == reservationId);
                if (existing is not null)
                {
                    if (existing.CreationIdempotencyKey == idempotencyKey) return Ok(existing);
                    return Conflict("Đơn đã có đối soát.");
                }

                var r = await _db.Reservations.FirstOrDefaultAsync(x => x.ReservationID == reservationId);
                if (r is null) return NotFound();
                if (r.Status != "Chờ đối soát") return Conflict("Đơn chưa ở trạng thái chờ đối soát.");

                var hasConfirmedReturn = await _db.HandoverReports.AnyAsync(x =>
                    x.ReservationID == reservationId &&
                    x.ReportType == "Trả xe" &&
                    x.IsLocked &&
                    !x.IsSuperseded &&
                    x.ConfirmedDate != null);
                if (!hasConfirmedReturn)
                    return Conflict("Chưa có biên bản trả xe đã được khách xác nhận bằng OTP.");

                var hasOpenIncident = await _db.Incidents.AnyAsync(x =>
                    x.ReservationID == reservationId && x.Status != "Đã giải quyết");
                if (hasOpenIncident)
                    return Conflict("Đơn còn sự cố chưa được giải quyết.");

                var hasOpenDispute = await _db.Disputes.AnyAsync(x =>
                    x.ReservationID == reservationId && x.Status != "Đã giải quyết" && x.Status != "Đã đóng");
                if (hasOpenDispute)
                    return Conflict("Đơn còn tranh chấp chưa được giải quyết.");

                var hasUnresolvedCharge = await _db.AdditionalCharges.AnyAsync(x =>
                    x.ReservationID == reservationId &&
                    (x.Status == AdditionalChargeStatuses.Submitted || x.Status == AdditionalChargeStatuses.CustomerRejected || x.Status == AdditionalChargeStatuses.StaffReview));
                if (hasUnresolvedCharge)
                    return Conflict("Đơn còn phụ phí chưa được khách xác nhận hoặc đang tranh chấp.");

                var hasPendingRefund = await _db.Payments.AnyAsync(x =>
                    x.ReservationID == reservationId &&
                    x.PaymentType == "Hoàn tiền" &&
                    x.Status == "Chờ hoàn tiền");
                if (hasPendingRefund)
                    return Conflict("Đơn còn khoản hoàn tiền đang chờ xử lý. Hãy hoàn tất giao dịch hoàn tiền trước khi lập đối soát.");

                var rentalCollected = await _db.Payments
                    .Where(x => x.ReservationID == reservationId && x.PaymentType == "Tiền thuê" && x.Status == "Thành công")
                    .SumAsync(x => (decimal?)x.Amount) ?? 0m;
                if (rentalCollected != r.TotalPrice)
                    return Conflict($"Chưa thu đủ tiền thuê thực tế. Đã thu {rentalCollected.ToString("#,0", CultureInfo.InvariantCulture)}/{r.TotalPrice.ToString("#,0", CultureInfo.InvariantCulture)} đồng.");

                var acceptedCharges = await _db.AdditionalCharges
                    .Where(x => x.ReservationID == reservationId &&
                                (x.Status == AdditionalChargeStatuses.CustomerAccepted || x.Status == AdditionalChargeStatuses.Approved || x.Status == AdditionalChargeStatuses.Collected))
                    .ToListAsync();
                var chargePaymentIds = acceptedCharges.Where(x => x.PaymentID.HasValue).Select(x => x.PaymentID!.Value).ToArray();
                var chargePayments = await _db.Payments
                    .Where(x => chargePaymentIds.Contains(x.PaymentID))
                    .ToDictionaryAsync(x => x.PaymentID);
                var unpaidChargeIds = acceptedCharges
                    .Where(charge => !charge.PaymentID.HasValue ||
                        !chargePayments.TryGetValue(charge.PaymentID.Value, out var payment) ||
                        payment.ReservationID != reservationId ||
                        payment.PaymentType != "Phụ phí" ||
                        payment.RelatedEntityType != nameof(AdditionalCharge) ||
                        payment.RelatedEntityID != charge.AdditionalChargeID ||
                        payment.Status != "Thành công" ||
                        payment.Amount != charge.Amount)
                    .Select(x => x.AdditionalChargeID)
                    .ToArray();
                if (unpaidChargeIds.Length > 0)
                    return Conflict($"Còn phụ phí đã chấp nhận nhưng chưa thanh toán đúng giao dịch: {string.Join(", ", unpaidChargeIds)}.");

                var chargeCollected = acceptedCharges.Sum(x => x.Amount);
                var grossCollected = rentalCollected + chargeCollected;
                var compensation = await _db.Disputes
                    .Where(x => x.ReservationID == reservationId && x.Status == "Đã giải quyết")
                    .SumAsync(x => (decimal?)x.CompensationAmount) ?? 0m;

                // Chỉ tính phí nhà cung cấp trên khoản doanh thu thực thu (tiền thuê/phụ phí),
                // tuyệt đối không tính tiền cọc là doanh thu.
                var paymentGatewayFee = await _db.Payments
                    .Where(x => x.ReservationID == reservationId &&
                                (x.PaymentType == "Tiền thuê" || x.PaymentType == "Phụ phí") &&
                                x.Status == "Thành công" &&
                                x.ProviderFeeVerified)
                    .SumAsync(x => (decimal?)x.ProviderFeeAmount) ?? 0m;
                var refundAmount = await _db.Payments
                    .Where(x => x.ReservationID == reservationId &&
                                x.PaymentType == "Hoàn tiền" &&
                                (x.Status == "Thành công" || x.Status == "Đã hoàn tiền" || x.RefundedDate != null))
                    .SumAsync(x => (decimal?)x.Amount) ?? 0m;

                var calculation = SettlementCalculationPolicy.Calculate(
                    grossCollected,
                    r.PlatformFeeAmount,
                    paymentGatewayFee,
                    refundAmount,
                    compensation);
                var item = new Settlement
                {
                    ReservationID = reservationId,
                    GrossRental = calculation.GrossRental,
                    PlatformFee = calculation.PlatformFee,
                    PaymentGatewayFee = calculation.PaymentGatewayFee,
                    RefundAmount = calculation.RefundAmount,
                    CompensationAmount = calculation.CompensationAmount,
                    OwnerPayout = calculation.OwnerPayout,
                    CreatedByAppUserID = UserId(),
                    CreationIdempotencyKey = idempotencyKey,
                    Status = SettlementStatuses.PartnerReview,
                    PartnerReviewDueDate = AddBusinessDays(DateTime.UtcNow, 3),
                    CreatedDate = DateTime.UtcNow
                };
                _db.Settlements.Add(item);
                var settlementClaim = await _db.WorkItemClaims.FirstOrDefaultAsync(x => x.QueueType == "Đối soát" && x.EntityID == reservationId && x.Status == "Đang xử lý");
                if (settlementClaim is not null) settlementClaim.Status = "Đã hoàn tất";
                Audit("Tạo đối soát tự động từ giao dịch", nameof(Settlement), null,
                    $"Gross={calculation.GrossRental}; GatewayFee={calculation.PaymentGatewayFee}; Refund={calculation.RefundAmount}; Compensation={calculation.CompensationAmount}; OwnerPayout={calculation.OwnerPayout}");
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(item);
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                return Conflict("Đơn hoặc đối soát đã được người khác thay đổi. Vui lòng tải lại.");
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                var existing = await _db.Settlements.AsNoTracking().FirstOrDefaultAsync(x => x.ReservationID == reservationId);
                if (existing?.CreationIdempotencyKey == idempotencyKey) return Ok(existing);
                return Conflict("Đơn đã có đối soát hoặc yêu cầu đã được xử lý.");
            }
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpPut("settlements/{id:int}/partner-response")]
        public async Task<IActionResult> PartnerRespondSettlement(int id, PartnerSettlementResponseDto dto)
        {
            var settlement = await _db.Settlements.Include(x => x.Reservation).ThenInclude(x => x.PartnerVehicle)
                .FirstOrDefaultAsync(x => x.SettlementID == id);
            if (settlement is null) return NotFound();
            if (settlement.Reservation.PartnerVehicle.OwnerAppUserID != UserId()) return Forbid();
            if (settlement.Status != SettlementStatuses.PartnerReview)
                return Conflict("Bảng đối soát không còn chờ đối tác phản hồi.");
            if (settlement.PartnerReviewDueDate.HasValue && settlement.PartnerReviewDueDate.Value < DateTime.UtcNow)
                return Conflict("Đã hết thời hạn phản hồi; hệ thống đã hoặc sẽ tự chốt bảng đối soát.");

            if (dto.Accept)
            {
                settlement.Status = SettlementStatuses.AwaitingApproval;
                settlement.PartnerConfirmedDate = DateTime.UtcNow;
                settlement.PartnerDisputeReason = null;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.Reason)) return BadRequest("Bắt buộc nhập lý do khiếu nại đối soát.");
                settlement.Status = SettlementStatuses.PartnerDisputed;
                settlement.PartnerDisputeReason = dto.Reason.Trim();
            }
            Audit(dto.Accept ? "Đối tác xác nhận đối soát" : "Đối tác khiếu nại đối soát", nameof(Settlement), id.ToString(), dto.Reason);
            await _db.SaveChangesAsync();
            return Ok(settlement);
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("settlements/{id:int}/resolve-partner-dispute")]
        public async Task<IActionResult> ResolveSettlementDispute(int id, ResolveSettlementDisputeDto dto)
        {
            var settlement = await _db.Settlements.FirstOrDefaultAsync(x => x.SettlementID == id);
            if (settlement is null) return NotFound();
            if (settlement.Status != SettlementStatuses.PartnerDisputed) return Conflict("Đối soát không có khiếu nại đang chờ xử lý.");
            if (string.IsNullOrWhiteSpace(dto.Resolution)) return BadRequest("Bắt buộc nhập kết luận xử lý.");
            settlement.Status = dto.ApproveForPayout ? SettlementStatuses.AwaitingApproval : SettlementStatuses.PartnerReview;
            settlement.PartnerDisputeReason = $"{settlement.PartnerDisputeReason} | Kết luận: {dto.Resolution.Trim()}";
            if (!dto.ApproveForPayout) settlement.PartnerReviewDueDate = AddBusinessDays(DateTime.UtcNow, 3);
            Audit("Xử lý khiếu nại đối soát", nameof(Settlement), id.ToString(), dto.Resolution.Trim());
            await _db.SaveChangesAsync();
            return Ok(settlement);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("settlements/{id:int}/pay")]
        public async Task<IActionResult> PaySettlement(int id, PaySettlementDto dto)
        {
            var transactionCode = dto.TransactionCode?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(transactionCode) || transactionCode.Length > 100) return BadRequest("Mã giao dịch chi trả không hợp lệ.");
            var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault()?.Trim();
            if (string.IsNullOrWhiteSpace(idempotencyKey)) idempotencyKey = $"settlement-pay:{id}:{transactionCode}";
            if (idempotencyKey.Length > 100) return BadRequest("Idempotency-Key tối đa 100 ký tự.");

            await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var item = await _db.Settlements.Include(x => x.Reservation).ThenInclude(x => x.PartnerVehicle).FirstOrDefaultAsync(x => x.SettlementID == id);
                if (item is null) return NotFound();
                if (item.Status == SettlementStatuses.Paid && item.PayoutIdempotencyKey == idempotencyKey)
                    return Ok(new { Message = "Đối soát đã được chi trả ở yêu cầu trước.", IdempotentReplay = true });
                if (item.Status != SettlementStatuses.AwaitingApproval)
                    return Conflict("Đối soát chưa được đối tác xác nhận/hết thời hạn phản hồi hoặc đã xử lý.");
                if (item.Reservation.Status != "Chờ đối soát")
                    return Conflict("Đơn đã thay đổi trạng thái sau khi lập đối soát. Không được chi trả từ bản đối soát này.");
                if (item.CreatedByAppUserID.HasValue && item.CreatedByAppUserID.Value == UserId())
                    return Conflict("Người lập đối soát không được tự phê duyệt và chi trả. Hãy dùng một tài khoản Admin khác.");
                var partnerProfile = await _db.VehiclePartnerProfiles.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.AppUserID == item.Reservation.PartnerVehicle.OwnerAppUserID);
                if (partnerProfile?.IsPayoutPaused == true)
                    return Conflict($"Đang tạm dừng chi trả đối soát: {partnerProfile.PayoutPauseReason ?? "tài khoản ngân hàng đang chờ xác minh"}.");
                if (await _db.Settlements.AnyAsync(x => x.SettlementID != id && x.PayoutTransactionCode == transactionCode))
                    return Conflict("Mã giao dịch chi trả đã được sử dụng.");
                if (await _db.Settlements.AnyAsync(x => x.SettlementID != id && x.PayoutIdempotencyKey == idempotencyKey))
                    return Conflict("Idempotency-Key đã được dùng cho đối soát khác.");

                item.Status = SettlementStatuses.Paid;
                item.PayoutTransactionCode = transactionCode;
                item.PayoutIdempotencyKey = idempotencyKey;
                item.ApprovedByAppUserID = UserId();
                item.PaidDate = DateTime.UtcNow;
                var old = item.Reservation.Status;
                item.Reservation.Status = "Hoàn thành";
                item.Reservation.CompletedDate = DateTime.UtcNow;
                History(item.ReservationID, old, "Hoàn thành", "Đã chi trả chủ xe và đóng đối soát.");

                var commission = await _db.CommissionTransactions.FirstOrDefaultAsync(x => x.ReservationID == item.ReservationID);
                if (commission is null)
                {
                    commission = new CommissionTransaction
                    {
                        ReservationID = item.ReservationID,
                        SettlementID = item.SettlementID,
                        PartnerVehicleID = item.Reservation.PartnerVehicleID,
                        PartnerAppUserID = item.Reservation.PartnerVehicle.OwnerAppUserID,
                        GrossAmount = item.GrossRental,
                        CommissionRate = item.Reservation.CommissionRateSnapshot,
                        CommissionAmount = item.PlatformFee,
                        PartnerNetAmount = item.OwnerPayout,
                        CreatedDate = DateTime.UtcNow
                    };
                    _db.CommissionTransactions.Add(commission);
                }
                commission.SettlementID = item.SettlementID;
                commission.GrossAmount = item.GrossRental;
                commission.CommissionRate = item.Reservation.CommissionRateSnapshot;
                commission.CommissionAmount = item.PlatformFee;
                commission.PartnerNetAmount = item.OwnerPayout;
                commission.Status = "Đã thanh toán";
                commission.BankReference = transactionCode;
                commission.ReconciledDate ??= DateTime.UtcNow;
                commission.PaidDate = DateTime.UtcNow;
                commission.Note = $"Đã chi trả theo đối soát #{item.SettlementID}.";

                Audit("Chi trả đối soát", nameof(Settlement), id.ToString(), transactionCode);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok();
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                return Conflict("Đối soát đã được tài khoản khác xử lý. Vui lòng tải lại dữ liệu.");
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                var existing = await _db.Settlements.AsNoTracking().FirstOrDefaultAsync(x => x.SettlementID == id);
                if (existing?.Status == SettlementStatuses.Paid && existing.PayoutIdempotencyKey == idempotencyKey)
                    return Ok(new { Message = "Đối soát đã được chi trả ở yêu cầu trước.", IdempotentReplay = true });
                return Conflict("Mã giao dịch hoặc yêu cầu chi trả đã được xử lý.");
            }
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> Notifications() => Ok(await _db.Notifications.AsNoTracking().Where(x => x.AppUserID == UserId()).OrderByDescending(x => x.CreatedDate).Take(100).ToListAsync());

        [HttpGet("notifications/unread-count")]
        public async Task<IActionResult> UnreadNotificationCount() =>
            Ok(await _db.Notifications.AsNoTracking().CountAsync(x => x.AppUserID == UserId() && !x.IsRead));

        [HttpPut("notifications/{id:int}/read")]
        public async Task<IActionResult> ReadNotification(int id)
        {
            var item = await _db.Notifications.FirstOrDefaultAsync(x => x.NotificationID == id && x.AppUserID == UserId());
            if (item is null) return NotFound();
            if (!item.IsRead)
            {
                item.IsRead = true;
                item.ReadDate = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPut("notifications/read-all")]
        public async Task<IActionResult> ReadAllNotifications()
        {
            var unreadItems = await _db.Notifications
                .Where(x => x.AppUserID == UserId() && !x.IsRead)
                .ToListAsync();

            if (unreadItems.Count == 0) return Ok(new { updated = 0 });

            var readDate = DateTime.UtcNow;
            foreach (var item in unreadItems)
            {
                item.IsRead = true;
                item.ReadDate = readDate;
            }

            await _db.SaveChangesAsync();
            return Ok(new { updated = unreadItems.Count });
        }

        private static string ResolveVehicleDocumentCategory(string? documentType)
        {
            var value = (documentType ?? string.Empty).Trim();
            if (value.Contains("đăng ký", StringComparison.OrdinalIgnoreCase)) return "VehicleRegistration";
            if (value.Contains("đăng kiểm", StringComparison.OrdinalIgnoreCase)) return "VehicleInspection";
            if (value.Contains("bảo hiểm", StringComparison.OrdinalIgnoreCase)) return "VehicleInsurance";
            if (value.Contains("lái xe", StringComparison.OrdinalIgnoreCase) || value.Contains("GPLX", StringComparison.OrdinalIgnoreCase)) return "VehicleDriverLicense";
            throw new InvalidOperationException("Loại giấy tờ xe không hợp lệ.");
        }

        private async Task<bool> CanManageVehicle(int id) =>
            User.IsInRole("Admin") || User.IsInRole("Staff") ||
            (IsVehiclePartnerAccount() && await _db.PartnerVehicles.AnyAsync(x => x.PartnerVehicleID == id && x.OwnerAppUserID == UserId()));

        private async Task<bool> CanManageReservationAsOwner(int id) =>
            User.IsInRole("Admin") || User.IsInRole("Staff") ||
            (IsVehiclePartnerAccount() && await _db.Reservations.AnyAsync(x => x.ReservationID == id && x.PartnerVehicle.OwnerAppUserID == UserId()));

        private async Task<bool> CanAccessReservation(int id) =>
            User.IsInRole("Admin") || User.IsInRole("Staff") ||
            await _db.Reservations.AnyAsync(x => x.ReservationID == id && (x.CustomerAppUserID == UserId() || x.PartnerVehicle.OwnerAppUserID == UserId()));

        private bool IsVehiclePartnerAccount() =>
            string.Equals(User.FindFirstValue("IsVehiclePartner"), "true", StringComparison.OrdinalIgnoreCase);
        private static DateTime AddBusinessDays(DateTime startUtc, int businessDays)
        {
            var current = startUtc;
            var added = 0;
            while (added < businessDays)
            {
                current = current.AddDays(1);
                if (current.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                    added++;
            }
            return current;
        }

        private void History(int id, string oldStatus, string newStatus, string? note) => _db.ReservationStatusHistories.Add(new ReservationStatusHistory { ReservationID = id, OldStatus = oldStatus, NewStatus = newStatus, ChangedByAppUserID = UserId(), Note = note });
        private void Audit(string action, string entity, string? id, string? note) => _db.AuditLogs.Add(new AuditLog { AppUserID = UserId(), Action = action, EntityName = entity, EntityID = id, Note = note, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() });
        private void Notify(int userId, string title, string message, string type) => _db.Notifications.Add(new Notification { AppUserID = userId, Title = title, Message = message, Type = type });
    }

    public record VehicleDocumentDto(string DocumentType, string DocumentNumber, Guid FileId, DateTime? IssuedDate, DateTime? ExpiryDate);
    public record ReviewDocumentDto(string Status, string? Reason);
    public record MaintenanceDto(DateTime MaintenanceDate, int OdometerKm, int? NextMaintenanceKm, DateTime? NextMaintenanceDate, string WorkPerformed, string? Garage, decimal Cost, bool HasUnresolvedSafetyIssue, string? SafetyIssueNote);
    public record IncidentDto(string Type, string Description, string? LocationText, IReadOnlyList<Guid>? EvidenceFileIds, bool VehicleImmobilized, bool PoliceInvolved, bool InsuranceNotified, decimal EstimatedDamage, DateTime OccurredAt);
    public record ResolveIncidentDto(decimal CustomerLiability, bool OpenDispute, string? Note);
    public record TrafficFineDto(DateTime ViolationAt, string Violation, string? LocationText, decimal Amount, string? NoticeNumber, Guid? EvidenceFileId, DateTime? DueDate);
    public record ChargeDto(string ChargeType, decimal Amount, string Reason, IReadOnlyList<Guid>? EvidenceFileIds);
    public record ChargeResponseDto(bool Accept, string? Note);
    public record DepositDto(string Type, decimal Amount, string? Reason, string? TransactionCode);
    public record ReviewChargeDto(bool Approve, string Reason);
    public record PartnerSettlementResponseDto(bool Accept, string? Reason);
    public record ResolveSettlementDisputeDto(bool ApproveForPayout, string Resolution);
    public record PaySettlementDto(string TransactionCode);
}
