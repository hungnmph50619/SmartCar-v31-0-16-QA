using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.BusinessRules;
using SmartCar.Domain.Entities;
using SmartCar.Domain.Time;
using SmartCar.Dto.MarketplaceDtos;
using SmartCar.Dto.ReservationDtos;
using SmartCar.Persistence.Context;
using System.Security.Claims;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PartnerVehiclesController : ControllerBase
    {
        private readonly CarBookContext _context;
        public PartnerVehiclesController(CarBookContext context) => _context = context;

        [Authorize(Roles = "VehiclePartner")]
        [HttpGet("me/dashboard")]
        public async Task<IActionResult> GetMyDashboard()
        {
            if (!IsVehiclePartnerAccount()) return Forbid();
            var userId = GetCurrentUserId();
            var setting = await GetSettingAsync();

            var partnerProfile = await _context.VehiclePartnerProfiles.AsNoTracking()
                .FirstOrDefaultAsync(x => x.AppUserID == userId);

            var applications = await _context.VehiclePartnerApplications.AsNoTracking()
                .Include(x => x.Location)
                .Where(x => x.AppUserID == userId)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            var vehicles = await _context.PartnerVehicles.AsNoTracking()
                .Include(x => x.Car).ThenInclude(x => x.Brand)
                .Include(x => x.VehiclePartnerApplication).ThenInclude(x => x.Location)
                .Where(x => x.OwnerAppUserID == userId)
                .OrderByDescending(x => x.ListedDate)
                .ToListAsync();

            var vehicleIds = vehicles.Select(x => x.PartnerVehicleID).ToList();
            var reservations = await _context.Reservations.AsNoTracking()
                .Include(x => x.Car).ThenInclude(x => x.Brand)
                .Include(x => x.PickUpLocation)
                .Include(x => x.DropOffLocation)
                .Include(x => x.PartnerVehicle).ThenInclude(x => x.OwnerAppUser)
                .Include(x => x.PartnerVehicle).ThenInclude(x => x.VehiclePartnerApplication)
                .Where(x => vehicleIds.Contains(x.PartnerVehicleID))
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            var transactions = await _context.CommissionTransactions.AsNoTracking()
                .Include(x => x.PartnerAppUser)
                .Include(x => x.PartnerVehicle).ThenInclude(x => x.Car).ThenInclude(x => x.Brand)
                .Where(x => x.PartnerAppUserID == userId)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            var documents = await _context.VehicleDocuments.AsNoTracking()
                .Where(x => vehicleIds.Contains(x.PartnerVehicleID) && x.Status == "Đã xác minh")
                .ToListAsync();
            var maintenance = await _context.MaintenanceRecords.AsNoTracking()
                .Where(x => vehicleIds.Contains(x.PartnerVehicleID))
                .OrderByDescending(x => x.MaintenanceDate)
                .ToListAsync();
            var reservationIds = reservations.Select(x => x.ReservationID).ToList();
            var openIncidentCount = await _context.Incidents.CountAsync(x => reservationIds.Contains(x.ReservationID) && x.Status != "Đã xử lý");
            var openDisputeCount = await _context.Disputes.CountAsync(x => reservationIds.Contains(x.ReservationID) && x.Status != "Đã giải quyết" && x.Status != "Đã đóng");
            var now = DateTime.UtcNow;
            var localToday = VietnamTime.UtcToLocal(now).Date;
            var warningDate = localToday.AddDays(30);

            var resultVehicles = new List<ResultPartnerVehicleDto>();
            foreach (var vehicle in vehicles)
            {
                var carTransactions = transactions.Where(x => x.PartnerVehicleID == vehicle.PartnerVehicleID && x.Status != "Đã hủy").ToList();
                var dailyPrice = await _context.CarPricings.AsNoTracking()
                    .Where(x => x.CarID == vehicle.CarID && x.Pricing.Name == "Theo ngày")
                    .Select(x => x.Amount)
                    .FirstOrDefaultAsync();

                var vehicleDocuments = documents.Where(x => x.PartnerVehicleID == vehicle.PartnerVehicleID).ToList();
                var nearestExpiry = vehicleDocuments.Where(x => x.ExpiryDate.HasValue).Select(x => x.ExpiryDate).OrderBy(x => x).FirstOrDefault();
                var latestMaintenance = maintenance.FirstOrDefault(x => x.PartnerVehicleID == vehicle.PartnerVehicleID);
                var hasExpiredDocument = vehicleDocuments.Any(x => x.ExpiryDate.HasValue && x.ExpiryDate.Value.Date < localToday);
                var hasSafetyIssue = latestMaintenance?.HasUnresolvedSafetyIssue == true;
                var isMaintenanceDue = latestMaintenance?.NextMaintenanceDate.HasValue == true && latestMaintenance.NextMaintenanceDate.Value.Date <= localToday;
                var operationalStatus = hasExpiredDocument ? "Tạm khóa" : hasSafetyIssue ? "Đang bảo dưỡng" : !vehicle.IsActive ? "Ngừng cho thuê" : "Đang hoạt động";
                var warning = hasExpiredDocument
                    ? $"Giấy tờ đã hết hạn ngày {nearestExpiry.Value:dd/MM/yyyy}. Xe không đủ điều kiện nhận đơn mới."
                    : hasSafetyIssue ? latestMaintenance?.SafetyIssueNote
                    : isMaintenanceDue ? $"Xe đến hạn bảo dưỡng ngày {latestMaintenance!.NextMaintenanceDate!.Value:dd/MM/yyyy}."
                    : nearestExpiry.HasValue && nearestExpiry.Value.Date <= warningDate ? $"Giấy tờ sắp hết hạn ngày {nearestExpiry.Value:dd/MM/yyyy}." : null;

                resultVehicles.Add(new ResultPartnerVehicleDto
                {
                    PartnerVehicleID = vehicle.PartnerVehicleID,
                    CarID = vehicle.CarID,
                    CarName = $"{vehicle.Car.Brand?.Name} {vehicle.Car.Model}".Trim(),
                    LicensePlate = vehicle.VehiclePartnerApplication.LicensePlate,
                    CoverImageUrl = vehicle.Car.CoverImageUrl,
                    LocationName = vehicle.VehiclePartnerApplication.Location?.Name ?? string.Empty,
                    DailyPrice = dailyPrice,
                    DepositAmount = vehicle.DepositAmount,
                    AppliedCommissionRate = vehicle.CommissionRateOverride ?? setting.VehiclePartnerCommissionPercent,
                    IsActive = vehicle.IsActive && !hasExpiredDocument && !hasSafetyIssue,
                    OperationalStatus = operationalStatus,
                    WarningText = warning,
                    NearestDocumentExpiry = nearestExpiry,
                    NextMaintenanceDate = latestMaintenance?.NextMaintenanceDate,
                    ListedDate = vehicle.ListedDate,
                    CompletedReservations = reservations.Count(x => x.PartnerVehicleID == vehicle.PartnerVehicleID && x.Status == "Hoàn thành"),
                    GrossRevenue = carTransactions.Sum(x => x.GrossAmount),
                    PlatformCommission = carTransactions.Sum(x => x.CommissionAmount),
                    OwnerNetRevenue = carTransactions.Sum(x => x.PartnerNetAmount)
                });
            }

            var activeTransactions = transactions.Where(x => x.Status != "Đã hủy").ToList();
            return Ok(new PartnerVehicleDashboardDto
            {
                GlobalCommissionRate = setting.VehiclePartnerCommissionPercent,
                PartnerProfile = partnerProfile is null ? null : MapProfile(partnerProfile),
                TotalGrossRevenue = activeTransactions.Sum(x => x.GrossAmount),
                TotalPlatformCommission = activeTransactions.Sum(x => x.CommissionAmount),
                TotalOwnerNetRevenue = activeTransactions.Sum(x => x.PartnerNetAmount),
                PendingPayout = activeTransactions.Where(x => x.Status != "Đã thanh toán").Sum(x => x.PartnerNetAmount),
                PendingOwnerConfirmations = reservations.Count(x => x.Status == "Chờ chủ xe xác nhận"),
                ActiveRentals = reservations.Count(x => x.Status == "Đang thuê"),
                UpcomingDeliveries = reservations.Count(x => (x.Status is "Đã đặt cọc" or "Chờ giao xe") && x.PickUpDate.Date <= localToday.AddDays(2)),
                UpcomingReturns = reservations.Count(x => (x.Status is "Đang thuê" or "Chờ trả xe") && x.DropOffDate.Date <= localToday.AddDays(2)),
                ExpiringDocuments = documents.Count(x => x.ExpiryDate.HasValue && x.ExpiryDate.Value.Date <= warningDate),
                DueMaintenanceVehicles = maintenance.GroupBy(x => x.PartnerVehicleID).Select(x => x.OrderByDescending(y => y.MaintenanceDate).First()).Count(x => x.HasUnresolvedSafetyIssue || (x.NextMaintenanceDate.HasValue && x.NextMaintenanceDate.Value.Date <= localToday)),
                OpenIncidents = openIncidentCount,
                OpenDisputes = openDisputeCount,
                Applications = applications.Select(MapApplication).ToList(),
                Vehicles = resultVehicles,
                Reservations = reservations.Select(MapReservation).ToList(),
                Transactions = activeTransactions.Select(MapTransaction).ToList()
            });
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpGet("me/{id:int}/operations")]
        public async Task<IActionResult> GetMyVehicleOperations(int id)
        {
            if (!IsVehiclePartnerAccount()) return Forbid();
            var userId = GetCurrentUserId();
            var setting = await GetSettingAsync();
            var localToday = VietnamTime.UtcToLocal(DateTime.UtcNow).Date;
            var vehicle = await _context.PartnerVehicles.AsNoTracking()
                .Include(x => x.Car).ThenInclude(x => x.Brand)
                .Include(x => x.VehiclePartnerApplication).ThenInclude(x => x.Location)
                .FirstOrDefaultAsync(x => x.PartnerVehicleID == id && x.OwnerAppUserID == userId);
            if (vehicle is null) return NotFound("Không tìm thấy xe thuộc tài khoản chủ xe này.");

            var documents = await _context.VehicleDocuments.AsNoTracking()
                .Where(x => x.PartnerVehicleID == id).OrderBy(x => x.ExpiryDate).ToListAsync();
            var maintenance = await _context.MaintenanceRecords.AsNoTracking()
                .Where(x => x.PartnerVehicleID == id).OrderByDescending(x => x.MaintenanceDate).ToListAsync();
            var reservations = await _context.Reservations.AsNoTracking()
                .Where(x => x.PartnerVehicleID == id && x.Status != "Bị từ chối" && x.Status != "Đã hủy" && x.Status != "Hết hạn thanh toán" && x.Status != "Hết hạn chủ xe xác nhận")
                .OrderByDescending(x => x.PickUpDate).ThenByDescending(x => x.PickUpTime).ToListAsync();
            var transactions = await _context.CommissionTransactions.AsNoTracking()
                .Where(x => x.PartnerVehicleID == id && x.Status != "Đã hủy").ToListAsync();
            var dailyPrice = await _context.CarPricings.AsNoTracking()
                .Where(x => x.CarID == vehicle.CarID && x.Pricing.Name == "Theo ngày")
                .Select(x => x.Amount).FirstOrDefaultAsync();

            var verifiedDocuments = documents.Where(x => x.Status == "Đã xác minh").ToList();
            var nearestExpiry = verifiedDocuments.Where(x => x.ExpiryDate.HasValue).Select(x => x.ExpiryDate).OrderBy(x => x).FirstOrDefault();
            var latestMaintenance = maintenance.FirstOrDefault();
            var hasExpiredDocument = verifiedDocuments.Any(x => x.ExpiryDate.HasValue && x.ExpiryDate.Value.Date < localToday);
            var hasSafetyIssue = latestMaintenance?.HasUnresolvedSafetyIssue == true;
            var maintenanceDue = latestMaintenance?.NextMaintenanceDate.HasValue == true && latestMaintenance.NextMaintenanceDate.Value.Date <= localToday;
            var operationalStatus = hasExpiredDocument ? "Tạm khóa" : hasSafetyIssue ? "Đang bảo dưỡng" : !vehicle.IsActive ? "Ngừng cho thuê" : "Đang hoạt động";
            var warning = hasExpiredDocument && nearestExpiry.HasValue
                ? $"Đăng kiểm/bảo hiểm đã hết hạn ngày {nearestExpiry.Value:dd/MM/yyyy}. Xe bị ẩn khỏi kết quả tìm kiếm."
                : hasSafetyIssue ? latestMaintenance?.SafetyIssueNote
                : maintenanceDue ? $"Xe đến hạn bảo dưỡng ngày {latestMaintenance!.NextMaintenanceDate!.Value:dd/MM/yyyy}."
                : nearestExpiry.HasValue && nearestExpiry.Value.Date <= localToday.AddDays(30) ? $"Giấy tờ sắp hết hạn ngày {nearestExpiry.Value:dd/MM/yyyy}." : null;

            return Ok(new PartnerVehicleOperationsDto
            {
                Vehicle = new ResultPartnerVehicleDto
                {
                    PartnerVehicleID = vehicle.PartnerVehicleID,
                    CarID = vehicle.CarID,
                    CarName = $"{vehicle.Car.Brand?.Name} {vehicle.Car.Model}".Trim(),
                    LicensePlate = vehicle.VehiclePartnerApplication.LicensePlate,
                    CoverImageUrl = vehicle.Car.CoverImageUrl,
                    LocationName = vehicle.VehiclePartnerApplication.Location?.Name ?? string.Empty,
                    DailyPrice = dailyPrice,
                    DepositAmount = vehicle.DepositAmount,
                    AppliedCommissionRate = vehicle.CommissionRateOverride ?? setting.VehiclePartnerCommissionPercent,
                    IsActive = vehicle.IsActive && !hasExpiredDocument && !hasSafetyIssue,
                    OperationalStatus = operationalStatus,
                    WarningText = warning,
                    NearestDocumentExpiry = nearestExpiry,
                    NextMaintenanceDate = latestMaintenance?.NextMaintenanceDate,
                    ListedDate = vehicle.ListedDate,
                    CompletedReservations = reservations.Count(x => x.Status == "Hoàn thành"),
                    GrossRevenue = transactions.Sum(x => x.GrossAmount),
                    PlatformCommission = transactions.Sum(x => x.CommissionAmount),
                    OwnerNetRevenue = transactions.Sum(x => x.PartnerNetAmount)
                },
                RentalMode = vehicle.VehiclePartnerApplication.RentalMode,
                DeliveryMethod = vehicle.VehiclePartnerApplication.DeliveryMethod,
                KmLimitPerDay = vehicle.VehiclePartnerApplication.KmLimitPerDay,
                ExtraKmFee = vehicle.VehiclePartnerApplication.ExtraKmFee,
                PauseReason = vehicle.PauseReason,
                UpcomingReservationCount = reservations.Count(x => x.PickUpDate.Date >= localToday && x.Status is not ("Hoàn thành" or "Đã hủy")),
                ActiveReservationCount = reservations.Count(x => x.Status is "Đang thuê" or "Chờ trả xe" or "Đang xử lý sự cố"),
                Calendar = reservations.Select(x => new PartnerVehicleCalendarItemDto
                {
                    ReservationID = x.ReservationID,
                    Start = x.PickUpDate.Date + x.PickUpTime,
                    End = x.DropOffDate.Date + x.DropOffTime,
                    Status = x.Status,
                    CustomerName = $"{x.Surname} {x.Name}".Trim(),
                    OwnerReceivableAmount = x.PartnerReceivableAmount
                }).ToList(),
                Documents = documents.Select(x => new PartnerVehicleDocumentDto
                {
                    VehicleDocumentID = x.VehicleDocumentID,
                    DocumentType = x.DocumentType,
                    DocumentNumber = x.DocumentNumber,
                    FileUrl = x.FileUrl,
                    IssuedDate = x.IssuedDate,
                    ExpiryDate = x.ExpiryDate,
                    Status = x.Status,
                    RejectionReason = x.RejectionReason
                }).ToList(),
                MaintenanceRecords = maintenance.Select(x => new PartnerVehicleMaintenanceDto
                {
                    MaintenanceRecordID = x.MaintenanceRecordID,
                    MaintenanceDate = x.MaintenanceDate,
                    OdometerKm = x.OdometerKm,
                    NextMaintenanceKm = x.NextMaintenanceKm,
                    NextMaintenanceDate = x.NextMaintenanceDate,
                    WorkPerformed = x.WorkPerformed,
                    Garage = x.Garage,
                    Cost = x.Cost,
                    HasUnresolvedSafetyIssue = x.HasUnresolvedSafetyIssue,
                    SafetyIssueNote = x.SafetyIssueNote
                }).ToList()
            });
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpPut("reservations/{id:int}/decision")]
        public async Task<IActionResult> DecideReservation(int id, OwnerReservationDecisionDto dto)
        {
            if (!IsVehiclePartnerAccount()) return Forbid();
            var userId = GetCurrentUserId();
            var decision = dto.Decision?.Trim() ?? string.Empty;
            if (decision is not ("Chấp nhận" or "Từ chối"))
                return BadRequest("Quyết định của chủ xe không hợp lệ.");

            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var reservation = await _context.Reservations
                    .Include(x => x.PartnerVehicle)
                    .FirstOrDefaultAsync(x => x.ReservationID == id);
                if (reservation is null) return NotFound("Không tìm thấy yêu cầu thuê xe.");
                if (reservation.PartnerVehicle is null || reservation.PartnerVehicle.OwnerAppUserID != userId)
                    return Forbid();
                if (reservation.Status != "Chờ chủ xe xác nhận")
                    return BadRequest($"Yêu cầu này không còn ở trạng thái chờ chủ xe xác nhận. Trạng thái hiện tại: {reservation.Status}.");

                var now = DateTime.UtcNow;
                if (ReservationAvailabilityRules.IsOwnerResponseExpired(reservation.CreatedDate, now))
                {
                    reservation.Status = ReservationStatuses.Cancelled;
                    reservation.OwnerResponseDate = now;
                    reservation.OwnerNote = "Hệ thống đóng yêu cầu vì chủ xe không phản hồi trong 120 phút.";
                    _context.ReservationStatusHistories.Add(new ReservationStatusHistory
                    {
                        ReservationID = reservation.ReservationID,
                        OldStatus = "Chờ chủ xe xác nhận",
                        NewStatus = reservation.Status,
                        ChangedByAppUserID = userId,
                        Note = reservation.OwnerNote,
                        ChangedDate = now
                    });
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return Conflict("Yêu cầu đã hết thời hạn phản hồi 120 phút và không thể chấp nhận nữa.");
                }

                var oldStatus = reservation.Status;
                if (decision == "Chấp nhận")
                {
                    var schedules = await _context.Reservations
                        .Where(x => x.CarID == reservation.CarID && x.ReservationID != reservation.ReservationID)
                        .Select(x => new { x.Status, x.HoldExpiresAt, x.PartnerResponseExpiresAt, x.PaymentExpiresAt, x.BufferMinutesSnapshot, x.RentalMode, x.PickUpDate, x.DropOffDate, x.PickUpTime, x.DropOffTime })
                        .ToListAsync();
                    var requestedStart = reservation.PickUpDate.Date.Add(reservation.PickUpTime);
                    var requestedEnd = reservation.DropOffDate.Date.Add(reservation.DropOffTime);
                    var conflict = schedules
                        .Where(x => ReservationAvailabilityRules.IsBlocking(x.Status, x.HoldExpiresAt, x.PartnerResponseExpiresAt, x.PaymentExpiresAt, now))
                        .Any(x => ReservationAvailabilityRules.OverlapsWithTurnaroundBuffer(
                            x.PickUpDate.Date.Add(x.PickUpTime),
                            x.DropOffDate.Date.Add(x.DropOffTime),
                            requestedStart,
                            requestedEnd,
                            x.BufferMinutesSnapshot > 0 ? x.BufferMinutesSnapshot : ReservationAvailabilityRules.GetBufferMinutes(x.RentalMode)));
                    if (conflict)
                        return Conflict("Xe vừa được giữ cho một yêu cầu hoặc đơn khác trùng thời gian, bao gồm khoảng đệm giao nhận theo loại dịch vụ.");

                    reservation.Status = ReservationStatuses.PaymentPending;
                    reservation.PaymentExpiresAt = now.AddMinutes(ReservationAvailabilityRules.PaymentHoldMinutes);
                    reservation.HoldExpiresAt = reservation.PaymentExpiresAt;
                }
                else
                {
                    reservation.Status = ReservationStatuses.Cancelled;
                    reservation.HoldExpiresAt = null;
                    reservation.PaymentExpiresAt = null;
                }

                reservation.OwnerNote = string.IsNullOrWhiteSpace(dto.Note) ? null : (dto.Note ?? string.Empty).Trim();
                reservation.OwnerResponseDate = now;
                reservation.StateVersion++;
                _context.ReservationStatusHistories.Add(new ReservationStatusHistory
                {
                    ReservationID = reservation.ReservationID,
                    OldStatus = oldStatus,
                    NewStatus = reservation.Status,
                    ChangedByAppUserID = userId,
                    Note = reservation.OwnerNote,
                    ChangedDate = now
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(decision == "Chấp nhận"
                    ? $"Đã chấp nhận yêu cầu. Xe được giữ {ReservationAvailabilityRules.PaymentHoldMinutes} phút để khách thanh toán giữ chỗ."
                    : "Đã từ chối yêu cầu thuê xe.");
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Không thể lưu quyết định của chủ xe vào cơ sở dữ liệu. Hãy kiểm tra cấu trúc CSDL và thử lại.");
            }
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpPut("{id:int}/availability")]
        public async Task<IActionResult> UpdateAvailability(int id, UpdatePartnerVehicleAvailabilityDto dto)
        {
            if (!IsVehiclePartnerAccount()) return Forbid();
            var userId = GetCurrentUserId();
            var vehicle = await _context.PartnerVehicles.Include(x => x.Car)
                .FirstOrDefaultAsync(x => x.PartnerVehicleID == id && x.OwnerAppUserID == userId);
            if (vehicle is null) return NotFound("Không tìm thấy xe đối tác thuộc tài khoản này.");

            var now = DateTime.UtcNow;
            var localToday = VietnamTime.UtcToLocal(now).Date;
            var schedules = await _context.Reservations
                .Where(x => x.PartnerVehicleID == vehicle.PartnerVehicleID)
                .ToListAsync();
            var hasActiveRental = schedules.Any(x => ReservationAvailabilityRules.IsBlocking(x.Status, x.HoldExpiresAt, now));
            if (!dto.IsActive && hasActiveRental)
                return BadRequest("Không thể tạm ẩn xe khi đang có lượt giữ thanh toán hoặc đơn đã xác nhận chưa hoàn thành.");

            if (!dto.IsActive)
            {
                foreach (var pending in schedules.Where(x => x.Status == "Chờ chủ xe xác nhận"))
                {
                    pending.Status = "Bị từ chối";
                    pending.OwnerResponseDate = now;
                    pending.OwnerNote = "Chủ xe tạm ngừng cho thuê xe trước khi xác nhận yêu cầu.";
                    _context.ReservationStatusHistories.Add(new ReservationStatusHistory
                    {
                        ReservationID = pending.ReservationID,
                        OldStatus = "Chờ chủ xe xác nhận",
                        NewStatus = "Bị từ chối",
                        ChangedByAppUserID = userId,
                        Note = pending.OwnerNote,
                        ChangedDate = now
                    });
                    _context.Notifications.Add(new Notification
                    {
                        AppUserID = pending.CustomerAppUserID,
                        Title = "Yêu cầu thuê xe đã được đóng",
                        Message = "Chủ xe đã tạm ngừng cho thuê xe trước khi xác nhận. Vui lòng chọn xe khác.",
                        Type = "Reservation"
                    });
                }
            }

            if (dto.IsActive)
            {
                var hasExpiredDocument = await _context.VehicleDocuments.AnyAsync(x =>
                    x.PartnerVehicleID == id && x.Status == "Đã xác minh" &&
                    x.ExpiryDate.HasValue && x.ExpiryDate.Value.Date < localToday);
                if (hasExpiredDocument)
                    return BadRequest("Không thể mở lại xe: đăng kiểm hoặc bảo hiểm đã hết hạn. Hãy gửi giấy tờ mới để nhân viên duyệt.");

                var latestMaintenance = await _context.MaintenanceRecords
                    .Where(x => x.PartnerVehicleID == id)
                    .OrderByDescending(x => x.MaintenanceDate)
                    .ThenByDescending(x => x.MaintenanceRecordID)
                    .FirstOrDefaultAsync();
                if (latestMaintenance?.HasUnresolvedSafetyIssue == true)
                    return BadRequest("Không thể mở lại xe: vẫn còn vấn đề an toàn chưa được xác nhận đã xử lý.");
            }

            vehicle.IsActive = dto.IsActive;
            vehicle.PauseReason = dto.IsActive ? null : (string.IsNullOrWhiteSpace(dto.Reason) ? "Chủ xe tạm ngừng cho thuê." : (dto.Reason ?? string.Empty).Trim());
            var rentCars = await _context.RentACars.Where(x => x.CarID == vehicle.CarID).ToListAsync();
            foreach (var rentCar in rentCars) rentCar.Available = dto.IsActive;
            await _context.SaveChangesAsync();
            return Ok(dto.IsActive ? "Đã mở lại xe trên sàn." : "Đã tạm ẩn xe khỏi danh sách cho thuê.");
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var setting = await GetSettingAsync();
            var localToday = VietnamTime.UtcToLocal(DateTime.UtcNow).Date;
            var vehicles = await _context.PartnerVehicles.AsNoTracking()
                .Include(x => x.Car).ThenInclude(x => x.Brand)
                .Include(x => x.OwnerAppUser)
                .Include(x => x.VehiclePartnerApplication).ThenInclude(x => x.Location)
                .OrderByDescending(x => x.ListedDate)
                .ToListAsync();

            var result = new List<ResultPartnerVehicleDto>();
            foreach (var vehicle in vehicles)
            {
                var tx = await _context.CommissionTransactions.AsNoTracking()
                    .Where(x => x.PartnerVehicleID == vehicle.PartnerVehicleID && x.Status != "Đã hủy")
                    .ToListAsync();
                var dailyPrice = await _context.CarPricings.AsNoTracking()
                    .Where(x => x.CarID == vehicle.CarID && x.Pricing.Name == "Theo ngày")
                    .Select(x => x.Amount).FirstOrDefaultAsync();
                var vehicleDocuments = await _context.VehicleDocuments.AsNoTracking()
                    .Where(x => x.PartnerVehicleID == vehicle.PartnerVehicleID && x.Status == "Đã xác minh")
                    .ToListAsync();
                var nearestExpiry = vehicleDocuments.Where(x => x.ExpiryDate.HasValue)
                    .Select(x => x.ExpiryDate).OrderBy(x => x).FirstOrDefault();
                var latestMaintenance = await _context.MaintenanceRecords.AsNoTracking()
                    .Where(x => x.PartnerVehicleID == vehicle.PartnerVehicleID)
                    .OrderByDescending(x => x.MaintenanceDate).FirstOrDefaultAsync();
                var hasExpiredDocument = vehicleDocuments.Any(x => x.ExpiryDate.HasValue && x.ExpiryDate.Value.Date < localToday);
                var hasSafetyIssue = latestMaintenance?.HasUnresolvedSafetyIssue == true;
                var isMaintenanceDue = latestMaintenance?.NextMaintenanceDate.HasValue == true && latestMaintenance.NextMaintenanceDate.Value.Date <= localToday;
                var operationalStatus = hasExpiredDocument ? "Tạm khóa" : hasSafetyIssue ? "Đang bảo dưỡng" : !vehicle.IsActive ? "Ngừng cho thuê" : "Đang hoạt động";
                var warning = hasExpiredDocument && nearestExpiry.HasValue
                    ? $"Giấy tờ đã hết hạn ngày {nearestExpiry.Value:dd/MM/yyyy}."
                    : hasSafetyIssue ? latestMaintenance?.SafetyIssueNote
                    : isMaintenanceDue ? $"Xe đến hạn bảo dưỡng ngày {latestMaintenance!.NextMaintenanceDate!.Value:dd/MM/yyyy}."
                    : nearestExpiry.HasValue && nearestExpiry.Value.Date <= localToday.AddDays(30) ? $"Giấy tờ sắp hết hạn ngày {nearestExpiry.Value:dd/MM/yyyy}." : null;
                result.Add(new ResultPartnerVehicleDto
                {
                    PartnerVehicleID = vehicle.PartnerVehicleID,
                    CarID = vehicle.CarID,
                    CarName = $"{vehicle.Car.Brand?.Name} {vehicle.Car.Model}".Trim(),
                    LicensePlate = vehicle.VehiclePartnerApplication.LicensePlate,
                    CoverImageUrl = vehicle.Car.CoverImageUrl,
                    LocationName = vehicle.VehiclePartnerApplication.Location?.Name ?? string.Empty,
                    DailyPrice = dailyPrice,
                    DepositAmount = vehicle.DepositAmount,
                    AppliedCommissionRate = vehicle.CommissionRateOverride ?? setting.VehiclePartnerCommissionPercent,
                    IsActive = vehicle.IsActive && !hasExpiredDocument && !hasSafetyIssue,
                    OperationalStatus = operationalStatus,
                    WarningText = warning,
                    NearestDocumentExpiry = nearestExpiry,
                    NextMaintenanceDate = latestMaintenance?.NextMaintenanceDate,
                    ListedDate = vehicle.ListedDate,
                    CompletedReservations = await _context.Reservations.CountAsync(x => x.PartnerVehicleID == vehicle.PartnerVehicleID && x.Status == "Hoàn thành"),
                    GrossRevenue = tx.Sum(x => x.GrossAmount),
                    PlatformCommission = tx.Sum(x => x.CommissionAmount),
                    OwnerNetRevenue = tx.Sum(x => x.PartnerNetAmount)
                });
            }
            return Ok(result);
        }

        private async Task<PlatformFeeSetting> GetSettingAsync()
        {
            var setting = await _context.PlatformFeeSettings.OrderBy(x => x.PlatformFeeSettingID).FirstOrDefaultAsync();
            if (setting is not null) return setting;
            setting = new PlatformFeeSetting { VehiclePartnerCommissionPercent = 20m, UpdatedDate = DateTime.UtcNow };
            _context.PlatformFeeSettings.Add(setting);
            await _context.SaveChangesAsync();
            return setting;
        }

        private bool IsVehiclePartnerAccount()
            => string.Equals(User.FindFirstValue("IsVehiclePartner"), "true", StringComparison.OrdinalIgnoreCase);

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }


        private static VehiclePartnerProfileDto MapProfile(VehiclePartnerProfile x) => new()
        {
            VehiclePartnerProfileID = x.VehiclePartnerProfileID,
            AppUserID = x.AppUserID,
            PartnerType = x.PartnerType,
            FullName = x.FullName,
            Phone = x.Phone,
            Email = x.Email,
            Address = x.Address,
            CitizenIdentityNumber = x.CitizenIdentityNumber,
            CitizenIssuedDate = x.CitizenIssuedDate,
            CitizenExpiryDate = x.CitizenExpiryDate,
            PermanentAddress = x.PermanentAddress,
            CurrentAddress = x.CurrentAddress,
            CitizenFrontImageUrl = x.CitizenFrontImageUrl,
            CitizenBackImageUrl = x.CitizenBackImageUrl,
            PortraitImageUrl = x.PortraitImageUrl,
            BusinessName = x.BusinessName,
            TaxCode = x.TaxCode,
            BusinessRegistrationNumber = x.BusinessRegistrationNumber,
            HeadquartersAddress = x.HeadquartersAddress,
            RepresentativeName = x.RepresentativeName,
            RepresentativeTitle = x.RepresentativeTitle,
            BusinessLicenseImageUrl = x.BusinessLicenseImageUrl,
            AuthorizationDocumentUrl = x.AuthorizationDocumentUrl,
            BankName = x.BankName,
            BankAccountNumber = x.BankAccountNumber,
            BankAccountHolder = x.BankAccountHolder,
            BankBranch = x.BankBranch,
            Status = x.Status,
            ReviewNote = x.ReviewNote,
            CreatedDate = x.CreatedDate,
            SubmittedDate = x.SubmittedDate,
            ReviewedDate = x.ReviewedDate
        };

        private static ResultVehiclePartnerApplicationDto MapApplication(VehiclePartnerApplication x) => new()
        {
            VehiclePartnerApplicationID = x.VehiclePartnerApplicationID,
            AppUserID = x.AppUserID,
            OwnerFullName = x.OwnerFullName,
            Email = x.Email,
            Phone = x.Phone,
            Address = x.Address,
            CitizenIdentityNumber = x.CitizenIdentityNumber,
            BankName = x.BankName,
            BankAccountNumber = x.BankAccountNumber,
            BankAccountHolder = x.BankAccountHolder,
            BrandName = x.BrandName,
            Model = x.Model,
            VehicleVersion = x.VehicleVersion,
            ChassisNumber = x.ChassisNumber,
            EngineNumber = x.EngineNumber,
            ManufactureYear = x.ManufactureYear,
            LicensePlate = x.LicensePlate,
            Color = x.Color,
            Transmission = x.Transmission,
            Fuel = x.Fuel,
            Seat = x.Seat,
            Km = x.Km,
            LocationID = x.LocationID,
            LocationName = x.Location?.Name ?? string.Empty,
            ProposedDailyPrice = x.ProposedDailyPrice,
            ProposedDepositAmount = x.ProposedDepositAmount,
            RentalMode = x.RentalMode,
            DeliveryMethod = x.DeliveryMethod,
            DeliveryAddress = x.DeliveryAddress,
            KmLimitPerDay = x.KmLimitPerDay,
            ExtraKmFee = x.ExtraKmFee,
            DeliveryFee = x.DeliveryFee,
            Amenities = x.Amenities,
            Accessories = x.Accessories,
            RentalConditions = x.RentalConditions,
            CancellationPolicy = x.CancellationPolicy,
            VehicleImageUrl = x.VehicleImageUrl,
            FrontImageUrl = x.FrontImageUrl,
            RearImageUrl = x.RearImageUrl,
            LeftImageUrl = x.LeftImageUrl,
            RightImageUrl = x.RightImageUrl,
            InteriorImageUrl = x.InteriorImageUrl,
            DashboardImageUrl = x.DashboardImageUrl,
            RegistrationImageUrl = x.RegistrationImageUrl,
            InspectionImageUrl = x.InspectionImageUrl,
            InsuranceImageUrl = x.InsuranceImageUrl,
            Status = x.Status,
            AdminNote = x.AdminNote,
            CreatedDate = x.CreatedDate,
            ReviewedDate = x.ReviewedDate,
            ApprovedCarID = x.ApprovedCarID
        };

        private static ResultReservationDto MapReservation(Reservation x) => new()
        {
            ReservationID = x.ReservationID,
            CustomerAppUserID = x.CustomerAppUserID,
            PartnerVehicleID = x.PartnerVehicleID,
            CustomerName = $"{x.Surname} {x.Name}".Trim(),
            Email = x.Email,
            Phone = x.Phone,
            CarID = x.CarID,
            CarName = $"{x.Car.Brand?.Name} {x.Car.Model}".Trim(),
            OwnerName = x.PartnerVehicle.OwnerAppUser is null ? string.Empty : $"{x.PartnerVehicle.OwnerAppUser.Surname} {x.PartnerVehicle.OwnerAppUser.Name}".Trim(),
            OwnerPhone = x.PartnerVehicle.VehiclePartnerApplication?.Phone ?? string.Empty,
            PickUpLocation = x.PickUpLocation?.Name ?? string.Empty,
            DropOffLocation = x.DropOffLocation?.Name ?? string.Empty,
            PickUpDate = x.PickUpDate,
            DropOffDate = x.DropOffDate,
            PickUpTime = x.PickUpTime,
            DropOffTime = x.DropOffTime,
            TotalPrice = x.TotalPrice,
            CommissionRateSnapshot = x.CommissionRateSnapshot,
            PlatformFeeAmount = x.PlatformFeeAmount,
            PartnerReceivableAmount = x.PartnerReceivableAmount,
            DepositAmount = x.DepositAmount,
            DepositStatus = x.DepositStatus,
            Status = x.Status,
            CreatedDate = x.CreatedDate,
            Description = x.Description,
            OwnerNote = x.OwnerNote
        };

        private static ResultCommissionTransactionDto MapTransaction(CommissionTransaction x) => new()
        {
            CommissionTransactionID = x.CommissionTransactionID,
            PartnerName = $"{x.PartnerAppUser.Surname} {x.PartnerAppUser.Name}".Trim(),
            Reference = $"{x.PartnerVehicle.Car.Brand?.Name} {x.PartnerVehicle.Car.Model} - Đơn #{x.ReservationID}",
            GrossAmount = x.GrossAmount,
            CommissionRate = x.CommissionRate,
            CommissionAmount = x.CommissionAmount,
            PartnerNetAmount = x.PartnerNetAmount,
            Status = x.Status,
            CreatedDate = x.CreatedDate,
            PaidDate = x.PaidDate,
            BankReference = x.BankReference,
            Note = x.Note
        };
    }
}
