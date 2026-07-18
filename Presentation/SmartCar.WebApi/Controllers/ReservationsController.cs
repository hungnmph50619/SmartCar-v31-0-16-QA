using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Application.Features.Mediator.Commands.ReservationCommands;
using SmartCar.Application.Interfaces.ReservationInterfaces;
using SmartCar.Domain.BusinessRules;
using SmartCar.Domain.Entities;
using SmartCar.Domain.Policies;
using SmartCar.Domain.Time;
using SmartCar.Dto.ReservationDtos;
using SmartCar.Persistence.Context;
using SmartCar.WebApi.Services;
using System.Net.Mail;
using System.Security.Claims;
using System.Text.Json;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {

        private readonly IMediator _mediator;
        private readonly IReservationRepository _reservationRepository;
        private readonly CarBookContext _context;
        private readonly IReservationCancellationService _cancellationService;
        private readonly ISystemSettingService _systemSettings;

        public ReservationsController(
            IMediator mediator,
            IReservationRepository reservationRepository,
            CarBookContext context,
            IReservationCancellationService cancellationService,
            ISystemSettingService systemSettings)
        {
            _mediator = mediator;
            _reservationRepository = reservationRepository;
            _context = context;
            _cancellationService = cancellationService;
            _systemSettings = systemSettings;
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult> GetReservations()
        {
            var values = await _reservationRepository.GetAllWithDetailsAsync();
            return Ok(values.Select(MapReservation).ToList());
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetReservation(int id)
        {
            var reservation = await _reservationRepository.GetByIdWithDetailsAsync(id);
            return reservation is null ? NotFound("Không tìm thấy đơn đặt xe.") : Ok(MapReservation(reservation));
        }

        [Authorize(Roles = "Customer")]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyReservations()
        {
            var userId = GetCurrentUserId();
            var values = await _context.Reservations.AsNoTracking()
                .Include(x => x.Car).ThenInclude(x => x.Brand)
                .Include(x => x.PickUpLocation)
                .Include(x => x.DropOffLocation)
                .Include(x => x.PartnerVehicle).ThenInclude(x => x.OwnerAppUser)
                .Include(x => x.PartnerVehicle).ThenInclude(x => x.VehiclePartnerApplication)
                .Where(x => x.CustomerAppUserID == userId)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();
            return Ok(values.Select(MapReservation).ToList());
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        public async Task<IActionResult> CreateReservation(CreateReservationCommand command)
        {
            if (string.Equals(User.FindFirstValue("IsVehiclePartner"), "true", StringComparison.OrdinalIgnoreCase))
                return StatusCode(StatusCodes.Status403Forbidden, "Tài khoản chủ xe không được dùng để thuê xe.");

            var userId = GetCurrentUserId();
            var customer = await _context.AppUsers.AsNoTracking().FirstOrDefaultAsync(x => x.AppUserId == userId);
            if (customer is null) return Unauthorized("Phiên đăng nhập không hợp lệ.");
            if (!customer.EmailConfirmed)
                return BadRequest("Bạn cần xác minh email bằng OTP trước khi đặt xe.");
            if (string.IsNullOrWhiteSpace(customer.Phone))
                return BadRequest("Tài khoản chưa có số điện thoại. Vui lòng cập nhật thông tin liên hệ trước khi đặt xe.");

            var pickUpDateTime = VietnamTime.ComposeLocal(command.PickUpDate, command.PickUpTime);
            var dropOffDateTime = VietnamTime.ComposeLocal(command.DropOffDate, command.DropOffTime);
            var pickUpUtc = VietnamTime.LocalToUtc(pickUpDateTime);
            if (pickUpUtc < DateTime.UtcNow.AddMinutes(-1)) return BadRequest("Thời gian nhận xe không được ở trong quá khứ.");
            if (dropOffDateTime <= pickUpDateTime) return BadRequest("Thời gian trả xe phải sau thời gian nhận xe.");

            var selectedMode = command.RentalMode?.Trim() ?? string.Empty;
            if (selectedMode is not (ServiceTypes.SelfDrive or ServiceTypes.WithDriver))
                return BadRequest("Vui lòng chọn Tự lái hoặc Có tài xế.");
            var minimumHours = await _systemSettings.GetIntAsync(
                selectedMode == ServiceTypes.SelfDrive ? SmartCarSettingKeys.SelfDriveMinHours : SmartCarSettingKeys.DriverServiceMinHours,
                selectedMode == ServiceTypes.SelfDrive ? 4 : 2);
            if ((dropOffDateTime - pickUpDateTime).TotalHours < minimumHours)
                return BadRequest($"Thời gian thuê tối thiểu cho hình thức {selectedMode.ToLowerInvariant()} là {minimumHours} giờ.");
            var maxAdvanceDays = await _systemSettings.GetIntAsync(SmartCarSettingKeys.MaxAdvanceBookingDays, 90);
            if (pickUpDateTime > VietnamTime.UtcToLocal(DateTime.UtcNow).AddDays(maxAdvanceDays))
                return BadRequest($"Chỉ được đặt xe trước tối đa {maxAdvanceDays} ngày.");

            var partnerVehicle = await _context.PartnerVehicles
                .Include(x => x.VehiclePartnerApplication)
                .FirstOrDefaultAsync(x => x.CarID == command.CarID && x.IsActive &&
                    x.ApprovalStatus == VehicleApprovalStatuses.Approved &&
                    x.OperationStatus == VehicleOperationStatuses.Active &&
                    x.VehiclePartnerApplication.Status == "Đã duyệt");
            if (partnerVehicle is null) return BadRequest("Xe không còn được đối tác cho thuê trên hệ thống.");
            if (partnerVehicle.OwnerAppUserID == userId) return BadRequest("Bạn không thể tự đặt thuê xe thuộc tài khoản của mình.");

            var offeredMode = partnerVehicle.VehiclePartnerApplication.RentalMode?.Trim() ?? ServiceTypes.SelfDrive;
            if (offeredMode != "Tự lái hoặc có tài xế" && offeredMode != selectedMode)
                return BadRequest($"Xe này chỉ hỗ trợ hình thức '{offeredMode}'.");

            var offeredDelivery = partnerVehicle.VehiclePartnerApplication.DeliveryMethod?.Trim() ?? "Nhận tại điểm giao xe";
            var selectedDelivery = command.DeliveryMethod?.Trim() ?? string.Empty;
            if (selectedDelivery is not ("Nhận tại điểm giao xe" or "Giao xe tận nơi"))
                return BadRequest("Hình thức giao nhận không hợp lệ.");
            if (offeredDelivery == "Nhận tại điểm giao xe" && selectedDelivery != "Nhận tại điểm giao xe")
                return BadRequest("Xe này chỉ hỗ trợ nhận và trả tại điểm giao xe.");
            if (offeredDelivery == "Giao xe tận nơi" && selectedDelivery != "Giao xe tận nơi")
                return BadRequest("Xe này chỉ hỗ trợ giao xe tận nơi.");

            var verification = await _context.UserVerifications.AsNoTracking().FirstOrDefaultAsync(x =>
                x.AppUserID == userId && x.VerificationType == "Khách thuê");

            if (selectedMode == "Tự lái")
            {
                if (verification is null || verification.Status != "Đã xác minh")
                    return BadRequest("Thuê xe tự lái yêu cầu hồ sơ CCCD và giấy phép lái xe đã được xác minh.");
                if (string.IsNullOrWhiteSpace(verification.LegalFullName))
                    return BadRequest("Hồ sơ xác minh còn thiếu họ tên pháp lý theo CCCD.");
                if (verification.DateOfBirth is null || verification.DriverLicenseIssuedDate is null || verification.DriverLicenseExpiry is null)
                    return BadRequest("Hồ sơ xác minh còn thiếu ngày sinh hoặc thông tin thời hạn bằng lái.");
                if (verification.CitizenIdExpiryDate.HasValue && verification.CitizenIdExpiryDate.Value.Date < dropOffDateTime.Date)
                    return BadRequest($"CCCD phải còn hạn đến hết ngày trả xe {dropOffDateTime:dd/MM/yyyy}.");
                if (verification.DriverLicenseExpiry.Value.Date < dropOffDateTime.Date)
                    return BadRequest($"Giấy phép lái xe phải còn hạn đến hết ngày trả xe {dropOffDateTime:dd/MM/yyyy}.");

                command.Name = string.Empty;
                command.Surname = verification.LegalFullName.Trim();
                command.BookingHolderDateOfBirth = verification.DateOfBirth.Value.Date;
                command.Age = CalculateYears(verification.DateOfBirth.Value, pickUpDateTime.Date);
                command.DriverLicenseYear = CalculateYears(verification.DriverLicenseIssuedDate.Value, pickUpDateTime.Date);
                if (command.Age < 18) return BadRequest("Khách tự lái phải từ đủ 18 tuổi tại ngày bắt đầu chuyến.");
                if (command.DriverLicenseYear < 1) return BadRequest("GPLX phải được cấp ít nhất 12 tháng tính đến ngày bắt đầu chuyến.");
            }
            else
            {
                // Xe có tài xế: khách không trực tiếp điều khiển xe nên không bắt buộc CCCD/GPLX.
                // Hệ thống vẫn yêu cầu tài khoản, email OTP và số điện thoại liên hệ hợp lệ.
                command.Name = customer.Name?.Trim() ?? string.Empty;
                command.Surname = customer.Surname?.Trim() ?? string.Empty;
                command.DriverLicenseYear = 0;
                var bookingHolderDateOfBirth = verification?.DateOfBirth?.Date ?? command.BookingHolderDateOfBirth?.Date;
                if (!bookingHolderDateOfBirth.HasValue)
                    return BadRequest("Vui lòng nhập ngày sinh của người đứng tên đặt chuyến.");
                command.BookingHolderDateOfBirth = bookingHolderDateOfBirth.Value;
                command.Age = CalculateYears(bookingHolderDateOfBirth.Value, pickUpDateTime.Date);
                if (command.Age < 18)
                    return BadRequest("Người đứng tên đặt xe có tài xế phải từ đủ 18 tuổi tại ngày bắt đầu chuyến.");
                if (command.PassengerCount < 1)
                    return BadRequest("Vui lòng nhập số hành khách.");
                if (string.IsNullOrWhiteSpace(command.PickUpAddressText) || string.IsNullOrWhiteSpace(command.DropOffAddressText))
                    return BadRequest("Xe có tài xế yêu cầu địa chỉ đón và trả cụ thể.");
                if (string.IsNullOrWhiteSpace(command.Itinerary))
                    return BadRequest("Vui lòng nhập lịch trình hoặc điểm dừng dự kiến.");
            }

            command.Email = customer.Email;
            command.Phone = customer.Phone;
            command.RentalMode = selectedMode;
            command.DeliveryMethod = selectedDelivery;

            var validationMessage = ValidateCommand(command);
            if (validationMessage is not null) return BadRequest(validationMessage);

            var canRentAtLocation = await _reservationRepository.CanRentAtLocationAsync(command.CarID, command.PickUpLocationID, command.DropOffLocationID);
            if (!canRentAtLocation) return BadRequest("Xe không có sẵn tại địa điểm nhận hoặc địa điểm đã chọn không hợp lệ.");

            // Theo đặc tả, yêu cầu chờ chủ xe phản hồi giữ lịch tối đa 2 giờ.
            var isAvailable = await _reservationRepository.IsCarAvailableAsync(command.CarID, pickUpDateTime, dropOffDateTime, serviceType: selectedMode);
            if (!isAvailable) return Conflict("Xe đang có yêu cầu/đơn hoặc lịch khóa trùng thời gian, bao gồm khoảng đệm giao nhận theo loại dịch vụ.");

            var customerReservations = await _context.Reservations.AsNoTracking()
                .Where(x => x.CustomerAppUserID == userId)
                .Select(x => new { x.Status, x.HoldExpiresAt, x.PartnerResponseExpiresAt, x.PaymentExpiresAt, x.PickUpDate, x.DropOffDate, x.PickUpTime, x.DropOffTime })
                .ToListAsync();
            if (customerReservations.Any(x =>
                ReservationAvailabilityRules.IsBlocking(x.Status, x.HoldExpiresAt, x.PartnerResponseExpiresAt, x.PaymentExpiresAt, DateTime.UtcNow) &&
                x.PickUpDate.Date.Add(x.PickUpTime) < dropOffDateTime &&
                x.DropOffDate.Date.Add(x.DropOffTime) > pickUpDateTime))
                return Conflict("Tài khoản khách đang có một yêu cầu hoặc đơn thuê khác trùng thời gian.");

            var pricingPlan = await _context.VehiclePricingPlans.AsNoTracking()
                .Where(x => x.PartnerVehicleID == partnerVehicle.PartnerVehicleID &&
                            x.ServiceType == selectedMode && x.IsActive &&
                            x.EffectiveFromUtc <= DateTime.UtcNow &&
                            (!x.EffectiveToUtc.HasValue || x.EffectiveToUtc > DateTime.UtcNow))
                .OrderByDescending(x => x.EffectiveFromUtc)
                .FirstOrDefaultAsync();
            var legacyDailyPrice = await _reservationRepository.GetDailyPriceAsync(command.CarID);
            if (pricingPlan is null && legacyDailyPrice <= 0)
                return BadRequest($"Xe chưa được thiết lập bảng giá cho hình thức {selectedMode.ToLowerInvariant()}.");

            var setting = await GetSettingAsync();
            var totalHours = (decimal)(dropOffDateTime - pickUpDateTime).TotalHours;
            var requiredMinimumHours = Math.Max(minimumHours, pricingPlan?.MinimumHours ?? 0);
            if (totalHours < requiredMinimumHours)
                return BadRequest($"Thời gian thuê tối thiểu cho hình thức {selectedMode.ToLowerInvariant()} là {requiredMinimumHours} giờ.");
            var rentalDays = Math.Max(Math.Max(1, pricingPlan?.MinimumDays ?? 0), (int)Math.Ceiling((double)(totalHours / 24m)));
            var hourlyPrice = pricingPlan?.HourlyRate.HasValue == true
                ? pricingPlan.HourlyRate.Value * Math.Ceiling(totalHours * 2m) / 2m
                : decimal.MaxValue;
            var dailyUnitPrice = pricingPlan?.DailyRate ?? legacyDailyPrice;
            var dailyPriceTotal = dailyUnitPrice > 0 ? dailyUnitPrice * rentalDays : decimal.MaxValue;
            var baseRentalAmount = Math.Min(hourlyPrice, dailyPriceTotal);
            if (baseRentalAmount == decimal.MaxValue && pricingPlan?.TripRate is > 0)
                baseRentalAmount = pricingPlan.TripRate.Value;
            if (baseRentalAmount == decimal.MaxValue || baseRentalAmount <= 0)
                return BadRequest("Không tính được giá thuê từ bảng giá đang hoạt động.");

            var driverFee = selectedMode == ServiceTypes.WithDriver ? Math.Max(0, pricingPlan?.DriverFee ?? 0) : 0;
            var distanceFee = selectedMode == ServiceTypes.WithDriver && command.EstimatedDistanceKm.GetValueOrDefault() > 0 && pricingPlan?.PerKilometerRate is > 0
                ? command.EstimatedDistanceKm.Value * pricingPlan.PerKilometerRate.Value
                : 0;
            var deliveryFee = selectedDelivery == "Nhận tại điểm giao xe"
                ? 0
                : Math.Max(0, pricingPlan?.DeliveryFee ?? partnerVehicle.VehiclePartnerApplication.DeliveryFee);
            var rate = partnerVehicle.CommissionRateOverride ?? setting.VehiclePartnerCommissionPercent;
            command.CustomerAppUserID = userId;
            command.PartnerVehicleID = partnerVehicle.PartnerVehicleID;
            command.VehiclePricingPlanID = pricingPlan?.VehiclePricingPlanID;
            var rentalAmount = baseRentalAmount + driverFee + distanceFee;
            command.TotalPrice = rentalAmount + deliveryFee;
            command.CommissionRateSnapshot = rate;
            command.PlatformFeeAmount = decimal.Round(command.TotalPrice * rate / 100m, 0);
            command.PartnerReceivableAmount = command.TotalPrice - command.PlatformFeeAmount;
            command.ReservationDepositAmount = Math.Max(0, pricingPlan?.ReservationDepositAmount ?? 0);
            command.SecurityDepositAmount = selectedMode == ServiceTypes.SelfDrive
                ? Math.Max(0, pricingPlan?.SecurityDepositAmount ?? partnerVehicle.DepositAmount)
                : Math.Max(0, pricingPlan?.SecurityDepositAmount ?? 0);
            command.DepositAmount = command.ReservationDepositAmount + command.SecurityDepositAmount;
            command.BufferMinutesSnapshot = await _systemSettings.GetIntAsync(
                selectedMode == ServiceTypes.SelfDrive ? SmartCarSettingKeys.SelfDriveBufferMinutes : SmartCarSettingKeys.DriverServiceBufferMinutes,
                ReservationAvailabilityRules.GetBufferMinutes(selectedMode));
            var ownerResponseMinutes = await _systemSettings.GetIntAsync(SmartCarSettingKeys.PartnerResponseMinutes, ReservationAvailabilityRules.OwnerResponseMinutes);
            command.HoldExpiresAt = DateTime.UtcNow.AddMinutes(await _systemSettings.GetIntAsync(SmartCarSettingKeys.BookingHoldMinutes, 15));
            command.PartnerResponseExpiresAt = DateTime.UtcNow.AddMinutes(ownerResponseMinutes);
            command.PaymentExpiresAt = null;

            await using var bookingTransaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var availableInsideTransaction = await _reservationRepository.IsCarAvailableAsync(command.CarID, pickUpDateTime, dropOffDateTime, serviceType: selectedMode);
            if (!availableInsideTransaction)
            {
                await bookingTransaction.RollbackAsync();
                return Conflict("Xe vừa được giữ chỗ hoặc xác nhận cho đơn khác. Vui lòng chọn thời gian khác.");
            }

            var reservationId = await _mediator.Send(command);
            var createdReservation = await _context.Reservations.FirstAsync(x => x.ReservationID == reservationId);
            createdReservation.HoldExpiresAt = command.HoldExpiresAt;
            createdReservation.PartnerResponseExpiresAt = command.PartnerResponseExpiresAt;
            createdReservation.CancellationPolicyVersion = ReservationCancellationPolicy.Version;
            createdReservation.TermsVersion = "v1-2026";
            createdReservation.PriceSnapshotJson = JsonSerializer.Serialize(new
            {
                RentalMode = selectedMode,
                DeliveryMethod = selectedDelivery,
                PricingPlanId = pricingPlan?.VehiclePricingPlanID,
                HourlyRate = pricingPlan?.HourlyRate,
                DailyRate = dailyUnitPrice,
                TripRate = pricingPlan?.TripRate,
                PerKilometerRate = pricingPlan?.PerKilometerRate,
                RentalDays = rentalDays,
                RentalHours = totalHours,
                RentalAmount = rentalAmount,
                DriverFee = driverFee,
                EstimatedDistanceFee = distanceFee,
                DeliveryFee = deliveryFee,
                TotalPrice = command.TotalPrice,
                ReservationDepositAmount = command.ReservationDepositAmount,
                SecurityDepositAmount = command.SecurityDepositAmount,
                CommissionRate = command.CommissionRateSnapshot,
                PlatformFee = command.PlatformFeeAmount,
                PartnerReceivable = command.PartnerReceivableAmount,
                TurnaroundBufferMinutes = command.BufferMinutesSnapshot,
                OwnerResponseExpiresAt = command.PartnerResponseExpiresAt,
                PaymentHoldMinutes = ReservationAvailabilityRules.PaymentHoldMinutes,
                CancellationPolicy = new
                {
                    Version = ReservationCancellationPolicy.Version,
                    FeeFrom168HoursPercent = 0,
                    FeeFrom72ToBelow168HoursPercent = 10,
                    FeeFrom24ToBelow72HoursPercent = 30,
                    FeeBelow24HoursPercent = 70,
                    NoStandardCancellationFromPickupTime = true
                }
            });
            _context.ReservationStatusHistories.Add(new ReservationStatusHistory
            {
                ReservationID = reservationId,
                OldStatus = string.Empty,
                NewStatus = "Chờ chủ xe xác nhận",
                ChangedByAppUserID = userId,
                Note = $"Khách gửi yêu cầu {selectedMode.ToLowerInvariant()}. Yêu cầu giữ lịch đến {command.PartnerResponseExpiresAt:dd/MM/yyyy HH:mm}; chủ xe phải phản hồi trong thời hạn cấu hình.",
                ChangedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            await bookingTransaction.CommitAsync();

            return Ok(new CreateReservationResponseDto
            {
                ReservationID = reservationId,
                Message = "Đã gửi yêu cầu và giữ lịch xe trong thời gian chờ chủ xe xác nhận.",
                TotalPrice = command.TotalPrice,
                RentalDays = rentalDays
            });
        }

        [Authorize(Roles = "Customer")]
        [HttpGet("{id:int}/cancellation-preview")]
        public async Task<IActionResult> CancellationPreview(int id, CancellationToken cancellationToken)
        {
            var result = await _cancellationService.PreviewAsync(id, GetCurrentUserId(), false, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }

        [Authorize(Roles = "Customer")]
        [HttpPut("{id:int}/cancel")]
        public async Task<IActionResult> CancelMyReservation(int id, CancelReservationDto? dto, CancellationToken cancellationToken)
        {
            var reason = string.IsNullOrWhiteSpace(dto?.Reason) ? "Khách hàng chủ động hủy đơn." : (dto.Reason ?? string.Empty).Trim();
            var result = await _cancellationService.CancelAsync(id, GetCurrentUserId(), false, reason, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateReservationStatusDto dto)
        {
            var status = dto.Status?.Trim() ?? string.Empty;
            if (status.Equals("Đã hủy", StringComparison.OrdinalIgnoreCase))
            {
                var reason = string.IsNullOrWhiteSpace(dto.Note)
                    ? "Nhân viên/Quản trị viên hủy đơn."
                    : (dto.Note ?? string.Empty).Trim();
                var cancellation = await _cancellationService.CancelAsync(
                    id,
                    GetCurrentUserId(),
                    true,
                    reason,
                    HttpContext.RequestAborted);
                return StatusCode(cancellation.StatusCode, cancellation);
            }

            return Conflict(new
            {
                message = "Đã khóa API cập nhật trạng thái tổng quát. Thanh toán, giao nhận, sự cố, tranh chấp và đối soát phải dùng endpoint nghiệp vụ chuyên biệt.",
                requestedStatus = status
            });
        }


        private async Task<PlatformFeeSetting> GetSettingAsync()
        {
            return await _context.PlatformFeeSettings.AsNoTracking()
                .OrderBy(x => x.PlatformFeeSettingID)
                .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("Chưa cấu hình phí nền tảng. Vui lòng chạy seed/migration đúng phiên bản.");
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }


        private static int CalculateYears(DateTime start, DateTime end)
        {
            var years = end.Year - start.Year;
            if (start.Date > end.AddYears(-years).Date) years--;
            return Math.Max(0, years);
        }

        private static string? ValidateCommand(CreateReservationCommand command)
        {
            if (command.CarID <= 0 || command.PickUpLocationID <= 0 || command.DropOffLocationID <= 0) return "Thông tin xe hoặc địa điểm không hợp lệ.";
            if (string.IsNullOrWhiteSpace(command.Name) || string.IsNullOrWhiteSpace(command.Surname) || string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Phone)) return "Vui lòng nhập đầy đủ họ tên, email và số điện thoại.";
            try { _ = new MailAddress(command.Email); } catch (FormatException) { return "Email không hợp lệ."; }
            if (command.RentalMode is not ("Tự lái" or "Có tài xế")) return "Hình thức thuê không hợp lệ.";
            if (command.DeliveryMethod is not ("Nhận tại điểm giao xe" or "Giao xe tận nơi")) return "Hình thức giao nhận không hợp lệ.";
            if (command.Age is < 18 or > 100) return "Người đứng tên đơn phải từ đủ 18 tuổi.";
            if (command.RentalMode == ServiceTypes.SelfDrive)
            {
                if (command.DriverLicenseYear is < 1 or > 60) return "GPLX phải được cấp ít nhất 12 tháng.";
            }
            else
            {
                if (command.PassengerCount < 1) return "Vui lòng nhập số hành khách.";
                if (string.IsNullOrWhiteSpace(command.PickUpAddressText) || string.IsNullOrWhiteSpace(command.DropOffAddressText)) return "Vui lòng nhập địa chỉ đón và trả.";
                if (string.IsNullOrWhiteSpace(command.Itinerary)) return "Vui lòng nhập lịch trình dự kiến.";
            }
            return null;
        }

        private static ResultReservationDto MapReservation(Reservation x) => new()
        {
            ReservationID = x.ReservationID,
            CustomerAppUserID = x.CustomerAppUserID,
            PartnerVehicleID = x.PartnerVehicleID,
            CustomerName = $"{x.Surname} {x.Name}".Trim(),
            Email = x.Email,
            Phone = x.Phone,
            CarID = x.CarID,
            CarName = $"{x.Car?.Brand?.Name} {x.Car?.Model}".Trim(),
            OwnerName = x.PartnerVehicle?.OwnerAppUser is null ? string.Empty : $"{x.PartnerVehicle.OwnerAppUser.Surname} {x.PartnerVehicle.OwnerAppUser.Name}".Trim(),
            OwnerPhone = x.PartnerVehicle?.VehiclePartnerApplication?.Phone ?? string.Empty,
            PickUpLocation = x.PickUpLocation?.Name ?? string.Empty,
            DropOffLocation = x.DropOffLocation?.Name ?? string.Empty,
            RentalMode = x.RentalMode,
            DeliveryMethod = x.DeliveryMethod,
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
    }
    public record CancelReservationDto(string? Reason);

}
