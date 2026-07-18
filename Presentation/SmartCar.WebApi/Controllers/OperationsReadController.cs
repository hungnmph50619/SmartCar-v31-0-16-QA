using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Application.Interfaces.ReservationInterfaces;
using SmartCar.Domain.BusinessRules;
using SmartCar.Domain.Entities;
using SmartCar.Domain.Security;
using SmartCar.Domain.Time;
using SmartCar.Dto.AdminDashboardDtos;
using SmartCar.Dto.RentACarDtos;
using SmartCar.Dto.ReservationDtos;
using SmartCar.Dto.StaffDtos;
using SmartCar.Persistence.Context;
using SmartCar.WebApi.Services;
using System.Security.Claims;
using System.Text.Json;

namespace SmartCar.WebApi.Controllers
{
    [ApiController]
    [Route("api/operations")]
    public class OperationsReadController : ControllerBase
    {
        private readonly CarBookContext _context;
        private readonly IConfiguration _configuration;
        private readonly IPrivateFileService _files;
        private readonly IReservationRepository _reservationRepository;
        private readonly ISystemSettingService _systemSettings;

        public OperationsReadController(
            CarBookContext context,
            IConfiguration configuration,
            IPrivateFileService files,
            IReservationRepository reservationRepository,
            ISystemSettingService systemSettings)
        {
            _context = context;
            _configuration = configuration;
            _files = files;
            _reservationRepository = reservationRepository;
            _systemSettings = systemSettings;
        }

        [AllowAnonymous]
        [HttpGet("cars/{id:int}/detail")]
        public async Task<IActionResult> GetEnhancedCarDetail(int id)
        {
            var car = await _context.Cars.AsNoTracking()
                .Include(x => x.Brand)
                .Include(x => x.CarPricings).ThenInclude(x => x.Pricing)
                .Include(x => x.CarFeatures).ThenInclude(x => x.Feature)
                .Include(x => x.Reviews)
                .FirstOrDefaultAsync(x => x.CarID == id);
            if (car is null) return NotFound("Không tìm thấy xe.");

            var partner = await _context.PartnerVehicles.AsNoTracking()
                .Include(x => x.VehiclePartnerApplication).ThenInclude(x => x.Location)
                .FirstOrDefaultAsync(x => x.CarID == id);
            var now = DateTime.UtcNow;
            var busyEntities = await _context.Reservations.AsNoTracking()
                .Where(x => x.CarID == id)
                .OrderBy(x => x.PickUpDate)
                .Select(x => new
                {
                    x.Status, x.HoldExpiresAt, x.PartnerResponseExpiresAt, x.PaymentExpiresAt,
                    x.RentalMode, x.BufferMinutesSnapshot, x.PickUpDate, x.DropOffDate, x.PickUpTime, x.DropOffTime
                })
                .ToListAsync();
            var busy = busyEntities
                .Where(x => ReservationAvailabilityRules.IsBlocking(x.Status, x.HoldExpiresAt, x.PartnerResponseExpiresAt, x.PaymentExpiresAt, now))
                .Select(x =>
                {
                    var bufferMinutes = x.BufferMinutesSnapshot > 0
                        ? x.BufferMinutesSnapshot
                        : ReservationAvailabilityRules.GetBufferMinutes(x.RentalMode);
                    var buffer = TimeSpan.FromMinutes(bufferMinutes);
                    return new CarBusyPeriodDto
                    {
                        Start = x.PickUpDate.Date.Add(x.PickUpTime).Subtract(buffer),
                        End = x.DropOffDate.Date.Add(x.DropOffTime).Add(buffer),
                        Label = $"Đã giữ lịch ({bufferMinutes} phút đệm giao nhận)"
                    };
                }).ToList();
            var reviews = car.Reviews?.Where(x => !x.IsDeleted).ToList() ?? new List<Review>();
            var galleryCandidates = new List<CarGalleryImageDto>
            {
                new() { Url = car.BigImageUrl ?? string.Empty, Label = "Ảnh chính" },
                new() { Url = car.CoverImageUrl ?? string.Empty, Label = "Ngoại thất" },
                new() { Url = partner?.VehiclePartnerApplication.VehicleImageUrl ?? string.Empty, Label = "Toàn cảnh xe" },
                new() { Url = partner?.VehiclePartnerApplication.FrontImageUrl ?? string.Empty, Label = "Mặt trước" },
                new() { Url = partner?.VehiclePartnerApplication.RearImageUrl ?? string.Empty, Label = "Mặt sau" },
                new() { Url = partner?.VehiclePartnerApplication.LeftImageUrl ?? string.Empty, Label = "Bên trái" },
                new() { Url = partner?.VehiclePartnerApplication.RightImageUrl ?? string.Empty, Label = "Bên phải" },
                new() { Url = partner?.VehiclePartnerApplication.InteriorImageUrl ?? string.Empty, Label = "Nội thất" },
                new() { Url = partner?.VehiclePartnerApplication.DashboardImageUrl ?? string.Empty, Label = "Bảng điều khiển" }
            };
            var seenImageUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var galleryItems = galleryCandidates
                .Where(x => !string.IsNullOrWhiteSpace(x.Url))
                .Select(x => new CarGalleryImageDto { Url = x.Url.Trim(), Label = x.Label })
                .Where(x => seenImageUrls.Add(x.Url))
                .ToList();
            var gallery = galleryItems.Select(x => x.Url).ToList();
            var plate = partner?.VehiclePartnerApplication.LicensePlate ?? string.Empty;
            var maskedPlate = plate.Length <= 4 ? "***" : plate[..Math.Min(3, plate.Length)] + "***" + plate[^2..];

            return Ok(new EnhancedCarDetailDto
            {
                CarID = car.CarID,
                Brand = car.Brand?.Name ?? string.Empty,
                Model = car.Model,
                ManufactureYear = partner?.VehiclePartnerApplication.ManufactureYear ?? 0,
                MaskedLicensePlate = maskedPlate,
                CoverImageUrl = car.CoverImageUrl,
                BigImageUrl = car.BigImageUrl,
                GalleryImages = gallery,
                GalleryItems = galleryItems,
                Seat = car.Seat,
                Transmission = car.Transmission,
                Fuel = car.Fuel,
                Km = car.Km,
                LocationName = partner?.VehiclePartnerApplication.Location?.Name ?? string.Empty,
                DailyPrice = car.CarPricings.Where(x => x.Pricing != null && x.Pricing.Name == "Theo ngày").Select(x => x.Amount).FirstOrDefault(),
                DepositAmount = partner?.DepositAmount ?? 0,
                KilometerLimitPerDay = partner?.VehiclePartnerApplication.KmLimitPerDay ?? 300,
                ExcessKilometerFee = partner?.VehiclePartnerApplication.ExtraKmFee ?? 0,
                DeliveryFee = partner?.VehiclePartnerApplication.DeliveryMethod == "Nhận tại điểm giao xe" ? 0 : partner?.VehiclePartnerApplication.DeliveryFee ?? 0,
                RentalMode = partner?.VehiclePartnerApplication.RentalMode ?? "Tự lái",
                DeliveryMethod = partner?.VehiclePartnerApplication.DeliveryMethod ?? "Nhận tại điểm giao xe",
                RentalConditions = partner?.VehiclePartnerApplication.RentalConditions ??
                    (partner?.VehiclePartnerApplication.RentalMode == "Có tài xế"
                        ? "Khách cần email đã xác minh và số điện thoại liên hệ; không bắt buộc CCCD hoặc giấy phép lái xe của khách."
                        : partner?.VehiclePartnerApplication.RentalMode == "Tự lái hoặc có tài xế"
                            ? "Tự lái yêu cầu CCCD và GPLX đã xác minh; có tài xế chỉ yêu cầu email đã xác minh và số điện thoại liên hệ."
                            : "Tự lái yêu cầu CCCD và GPLX phù hợp còn hiệu lực, đã được SmartCar xác minh."),
                CancellationPolicy = partner?.VehiclePartnerApplication.CancellationPolicy ?? "Phí hủy phụ thuộc thời điểm hủy và trạng thái đặt cọc; số tiền được hiển thị trước khi xác nhận.",
                Rating = reviews.Count == 0 ? 0 : Math.Round((decimal)reviews.Average(x => x.RaytingValue), 1),
                RatingCount = reviews.Count,
                Features = car.CarFeatures?.Where(x => x.Available && x.Feature != null).Select(x => x.Feature.Name).ToList() ?? new(),
                BusyPeriods = busy,
                IsActive = partner?.IsActive == true &&
                           partner.ApprovalStatus == VehicleApprovalStatuses.Approved &&
                           partner.OperationStatus == VehicleOperationStatuses.Active &&
                           partner.VehiclePartnerApplication.Status == "Đã duyệt"
            });
        }

        [AllowAnonymous]
        [HttpGet("cars/{id:int}/quote")]
        public async Task<IActionResult> GetQuote(
            int id,
            DateTime pickUpDate,
            DateTime dropOffDate,
            TimeSpan pickUpTime,
            TimeSpan dropOffTime,
            string? rentalMode = null,
            string? deliveryMethod = null,
            decimal? estimatedDistanceKm = null)
        {
            var start = VietnamTime.ComposeLocal(pickUpDate, pickUpTime);
            var end = VietnamTime.ComposeLocal(dropOffDate, dropOffTime);
            if (VietnamTime.LocalToUtc(start) < DateTime.UtcNow.AddMinutes(-1))
                return BadRequest("Thời gian nhận xe không được ở trong quá khứ.");
            if (end <= start) return BadRequest("Thời gian trả xe phải sau thời gian nhận xe.");
            if (estimatedDistanceKm is < 0 or > 100000) return BadRequest("Quãng đường dự kiến không hợp lệ.");

            var car = await _context.Cars.AsNoTracking()
                .Include(x => x.Brand)
                .Include(x => x.CarPricings).ThenInclude(x => x.Pricing)
                .FirstOrDefaultAsync(x => x.CarID == id);
            var partner = await _context.PartnerVehicles.AsNoTracking()
                .Include(x => x.VehiclePartnerApplication)
                .FirstOrDefaultAsync(x => x.CarID == id && x.IsActive &&
                    x.ApprovalStatus == VehicleApprovalStatuses.Approved &&
                    x.OperationStatus == VehicleOperationStatuses.Active &&
                    x.VehiclePartnerApplication.Status == "Đã duyệt");
            if (car is null || partner is null) return NotFound("Xe không còn hoạt động trên sàn.");

            var offeredMode = partner.VehiclePartnerApplication.RentalMode?.Trim() ?? ServiceTypes.SelfDrive;
            var selectedMode = string.IsNullOrWhiteSpace(rentalMode)
                ? (offeredMode == ServiceTypes.WithDriver ? ServiceTypes.WithDriver : ServiceTypes.SelfDrive)
                : rentalMode.Trim();
            if (selectedMode is not (ServiceTypes.SelfDrive or ServiceTypes.WithDriver))
                return BadRequest("Hình thức thuê không hợp lệ.");
            if (offeredMode != "Tự lái hoặc có tài xế" && selectedMode != offeredMode)
                return BadRequest($"Xe này chỉ hỗ trợ hình thức '{offeredMode}'.");

            var offeredDelivery = partner.VehiclePartnerApplication.DeliveryMethod?.Trim() ?? "Nhận tại điểm giao xe";
            var selectedDelivery = string.IsNullOrWhiteSpace(deliveryMethod)
                ? (offeredDelivery == "Giao xe tận nơi" ? "Giao xe tận nơi" : "Nhận tại điểm giao xe")
                : deliveryMethod.Trim();
            if (selectedDelivery is not ("Nhận tại điểm giao xe" or "Giao xe tận nơi"))
                return BadRequest("Hình thức giao nhận không hợp lệ.");
            if (offeredDelivery == "Nhận tại điểm giao xe" && selectedDelivery != "Nhận tại điểm giao xe")
                return BadRequest("Xe này chỉ hỗ trợ nhận tại điểm giao xe.");
            if (offeredDelivery == "Giao xe tận nơi" && selectedDelivery != "Giao xe tận nơi")
                return BadRequest("Xe này chỉ hỗ trợ giao xe tận nơi.");

            var minimumHours = await _systemSettings.GetIntAsync(
                selectedMode == ServiceTypes.SelfDrive ? SmartCarSettingKeys.SelfDriveMinHours : SmartCarSettingKeys.DriverServiceMinHours,
                selectedMode == ServiceTypes.SelfDrive ? 4 : 2);
            var maxAdvanceDays = await _systemSettings.GetIntAsync(SmartCarSettingKeys.MaxAdvanceBookingDays, 90);
            if (start > VietnamTime.UtcToLocal(DateTime.UtcNow).AddDays(maxAdvanceDays))
                return BadRequest($"Chỉ được đặt xe trước tối đa {maxAdvanceDays} ngày.");

            var pricingPlan = await _context.VehiclePricingPlans.AsNoTracking()
                .Where(x => x.PartnerVehicleID == partner.PartnerVehicleID &&
                            x.ServiceType == selectedMode && x.IsActive &&
                            x.EffectiveFromUtc <= DateTime.UtcNow &&
                            (!x.EffectiveToUtc.HasValue || x.EffectiveToUtc > DateTime.UtcNow))
                .OrderByDescending(x => x.EffectiveFromUtc)
                .FirstOrDefaultAsync();
            var legacyDailyPrice = car.CarPricings
                .Where(x => x.Pricing != null && x.Pricing.Name == "Theo ngày")
                .Select(x => x.Amount)
                .FirstOrDefault();
            if (pricingPlan is null && legacyDailyPrice <= 0)
                return BadRequest($"Xe chưa được thiết lập bảng giá cho hình thức {selectedMode.ToLowerInvariant()}.");

            var requiredMinimumHours = Math.Max(minimumHours, pricingPlan?.MinimumHours ?? 0);
            var totalHours = (decimal)(end - start).TotalHours;
            if (totalHours < requiredMinimumHours)
                return BadRequest($"Thời gian thuê tối thiểu cho hình thức {selectedMode.ToLowerInvariant()} là {requiredMinimumHours} giờ.");

            var available = await _reservationRepository.IsCarAvailableAsync(id, start, end, serviceType: selectedMode);
            var rentalDays = Math.Max(Math.Max(1, pricingPlan?.MinimumDays ?? 0), (int)Math.Ceiling((double)(totalHours / 24m)));
            var hourlyTotal = pricingPlan?.HourlyRate is > 0
                ? pricingPlan.HourlyRate.Value * Math.Ceiling(totalHours * 2m) / 2m
                : decimal.MaxValue;
            var dailyUnitPrice = pricingPlan?.DailyRate ?? legacyDailyPrice;
            var dailyTotal = dailyUnitPrice > 0 ? dailyUnitPrice * rentalDays : decimal.MaxValue;
            var baseRentalAmount = Math.Min(hourlyTotal, dailyTotal);
            if (baseRentalAmount == decimal.MaxValue && pricingPlan?.TripRate is > 0)
                baseRentalAmount = pricingPlan.TripRate.Value;
            if (baseRentalAmount == decimal.MaxValue || baseRentalAmount <= 0)
                return BadRequest("Không tính được giá thuê từ bảng giá đang hoạt động.");

            var driverFee = selectedMode == ServiceTypes.WithDriver ? Math.Max(0, pricingPlan?.DriverFee ?? 0) : 0;
            var distanceFee = selectedMode == ServiceTypes.WithDriver && estimatedDistanceKm.GetValueOrDefault() > 0 && pricingPlan?.PerKilometerRate is > 0
                ? estimatedDistanceKm.Value * pricingPlan.PerKilometerRate.Value
                : 0;
            var deliveryFee = selectedDelivery == "Nhận tại điểm giao xe"
                ? 0
                : Math.Max(0, pricingPlan?.DeliveryFee ?? partner.VehiclePartnerApplication.DeliveryFee);
            var rentalAmount = baseRentalAmount + driverFee + distanceFee;
            var reservationDeposit = Math.Max(0, pricingPlan?.ReservationDepositAmount ?? 0);
            var securityDeposit = selectedMode == ServiceTypes.SelfDrive
                ? Math.Max(0, pricingPlan?.SecurityDepositAmount ?? partner.DepositAmount)
                : Math.Max(0, pricingPlan?.SecurityDepositAmount ?? 0);
            var bufferMinutes = await _systemSettings.GetIntAsync(
                selectedMode == ServiceTypes.SelfDrive ? SmartCarSettingKeys.SelfDriveBufferMinutes : SmartCarSettingKeys.DriverServiceBufferMinutes,
                ReservationAvailabilityRules.GetBufferMinutes(selectedMode));

            return Ok(new ReservationQuoteDto
            {
                CarID = id,
                CarName = $"{car.Brand?.Name} {car.Model}".Trim(),
                PickUpDateTime = start,
                DropOffDateTime = end,
                IsAvailable = available,
                AvailabilityMessage = available
                    ? $"Xe còn trống; khi gửi yêu cầu, lịch được giữ tối đa 120 phút chờ chủ xe phản hồi. Khoảng đệm áp dụng: {bufferMinutes} phút."
                    : $"Xe đã có yêu cầu, đơn thuê hoặc lịch khóa trùng thời gian, gồm khoảng đệm {bufferMinutes} phút.",
                RentalMode = selectedMode,
                DeliveryMethod = selectedDelivery,
                Price = BuildPrice(
                    dailyUnitPrice,
                    rentalDays,
                    rentalAmount,
                    reservationDeposit + securityDeposit,
                    deliveryFee,
                    reservationDeposit: reservationDeposit,
                    securityDeposit: securityDeposit)
            });
        }

        [Authorize(Roles = "Customer")]
        [HttpGet("customer/readiness")]
        public async Task<IActionResult> GetCustomerReadiness()
        {
            var userId = UserId();
            var user = await _context.AppUsers.AsNoTracking().FirstOrDefaultAsync(x => x.AppUserId == userId);
            if (user is null) return Unauthorized();
            var verification = await _context.UserVerifications.AsNoTracking()
                .FirstOrDefaultAsync(x => x.AppUserID == userId && x.VerificationType == "Khách thuê");
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(user.Phone)) missing.Add("Số điện thoại");
            if (verification is null || string.IsNullOrWhiteSpace(verification.LegalFullName)) missing.Add("Họ tên pháp lý theo CCCD");
            if (verification is null || string.IsNullOrWhiteSpace(verification.Gender)) missing.Add("Giới tính");
            if (verification is null || string.IsNullOrWhiteSpace(verification.CitizenIdAddress)) missing.Add("Địa chỉ theo giấy tờ");
            if (verification is null || string.IsNullOrWhiteSpace(verification.PermanentProvince)) missing.Add("Tỉnh/thành phố thường trú");
            if (verification is null || string.IsNullOrWhiteSpace(verification.PermanentWard)) missing.Add("Xã/phường/đặc khu thường trú");
            if (verification is null || string.IsNullOrWhiteSpace(verification.PermanentDetail)) missing.Add("Địa chỉ chi tiết thường trú");
            if (verification is null || string.IsNullOrWhiteSpace(verification.CurrentProvince)) missing.Add("Tỉnh/thành phố hiện tại");
            if (verification is null || string.IsNullOrWhiteSpace(verification.CurrentWard)) missing.Add("Xã/phường/đặc khu hiện tại");
            if (verification is null || string.IsNullOrWhiteSpace(verification.CurrentDetail)) missing.Add("Địa chỉ chi tiết hiện tại");
            if (verification is null || verification.CitizenIdIssuedDate is null) missing.Add("Ngày cấp CCCD");
            if (verification is null || verification.CitizenIdExpiryDate is null) missing.Add("Ngày hết hạn CCCD");
            if (verification?.CitizenIdExpiryDate < SmartCar.Domain.Time.VietnamTime.Today) missing.Add("CCCD đã hết hạn");
            if (verification is null || string.IsNullOrWhiteSpace(verification.DriverLicenseNumber)) missing.Add("Số giấy phép lái xe");
            if (verification is null || string.IsNullOrWhiteSpace(verification.DriverLicenseClass)) missing.Add("Hạng giấy phép lái xe");
            if (verification is null || (!verification.CitizenIdFrontFileID.HasValue && string.IsNullOrWhiteSpace(verification.CitizenIdFrontUrl))) missing.Add("CCCD mặt trước");
            if (verification is null || (!verification.CitizenIdBackFileID.HasValue && string.IsNullOrWhiteSpace(verification.CitizenIdBackUrl))) missing.Add("CCCD mặt sau");
            if (verification is null || (!verification.DriverLicenseFileID.HasValue && string.IsNullOrWhiteSpace(verification.DriverLicenseUrl))) missing.Add("Bằng lái xe");
            if (verification is null || (!verification.PortraitFileID.HasValue && string.IsNullOrWhiteSpace(verification.PortraitUrl))) missing.Add("Ảnh chân dung");
            if (verification?.DateOfBirth is null) missing.Add("Ngày sinh");
            if (verification?.DriverLicenseIssuedDate is null) missing.Add("Ngày cấp bằng lái");
            if (verification?.DriverLicenseExpiry is null) missing.Add("Ngày hết hạn bằng lái");
            if (verification?.DriverLicenseExpiry < SmartCar.Domain.Time.VietnamTime.Today) missing.Add("Bằng lái đã hết hạn");
            return Ok(new CustomerReadinessDto
            {
                Name = user.Name,
                Surname = user.Surname,
                FullName = $"{user.Surname} {user.Name}".Trim(),
                Email = user.Email,
                Phone = user.Phone ?? string.Empty,
                LegalFullName = verification?.LegalFullName,
                Gender = verification?.Gender,
                CitizenIdAddress = verification?.CitizenIdAddress,
                PermanentProvinceCode = verification?.PermanentProvinceCode,
                PermanentWardCode = verification?.PermanentWardCode,
                PermanentProvince = verification?.PermanentProvince,
                PermanentWard = verification?.PermanentWard,
                PermanentDetail = verification?.PermanentDetail,
                PermanentAddress = verification?.PermanentAddress,
                CurrentAddressSameAsPermanent = verification?.CurrentAddressSameAsPermanent ?? false,
                CurrentProvinceCode = verification?.CurrentProvinceCode,
                CurrentWardCode = verification?.CurrentWardCode,
                CurrentProvince = verification?.CurrentProvince,
                CurrentWard = verification?.CurrentWard,
                CurrentDetail = verification?.CurrentDetail,
                CurrentAddress = verification?.CurrentAddress,
                DriverLicenseNumber = verification?.DriverLicenseNumber,
                DriverLicenseClass = verification?.DriverLicenseClass,
                CitizenIdIssuedDate = verification?.CitizenIdIssuedDate,
                CitizenIdExpiryDate = verification?.CitizenIdExpiryDate,
                VerificationStatus = verification?.Status ?? "Chưa xác minh",
                EmailConfirmed = user.EmailConfirmed,
                ContactReady = user.EmailConfirmed && !string.IsNullOrWhiteSpace(user.Phone),
                VerificationNote = verification?.RejectionReason,
                CitizenIdMasked = verification?.CitizenIdMasked ?? string.Empty,
                HasCitizenIdFront = verification?.CitizenIdFrontFileID.HasValue == true || !string.IsNullOrWhiteSpace(verification?.CitizenIdFrontUrl),
                HasCitizenIdBack = verification?.CitizenIdBackFileID.HasValue == true || !string.IsNullOrWhiteSpace(verification?.CitizenIdBackUrl),
                HasDriverLicense = verification?.DriverLicenseFileID.HasValue == true || !string.IsNullOrWhiteSpace(verification?.DriverLicenseUrl),
                HasPortrait = verification?.PortraitFileID.HasValue == true || !string.IsNullOrWhiteSpace(verification?.PortraitUrl),
                DateOfBirth = verification?.DateOfBirth,
                DriverLicenseIssuedDate = verification?.DriverLicenseIssuedDate,
                DriverLicenseExpiry = verification?.DriverLicenseExpiry,
                CanBook = user.EmailConfirmed && verification?.Status == "Đã xác minh" && missing.Count == 0,
                CanBookWithDriver = user.EmailConfirmed && !string.IsNullOrWhiteSpace(user.Phone),
                VerificationSubmittedDate = verification?.CreatedDate,
                VerificationReviewedDate = verification?.ReviewedDate,
                MissingItems = missing.Distinct().ToList()
            });
        }

        [Authorize(Roles = "Customer")]
        [HttpPost("customer/verification")]
        public async Task<IActionResult> SubmitVerification(SubmitVerificationDto dto, CancellationToken cancellationToken)
        {
            if (IsVehiclePartnerAccount()) return Forbid();
            if (string.IsNullOrWhiteSpace(dto.LegalFullName)) return BadRequest("Vui lòng nhập họ tên pháp lý theo CCCD.");
            if (string.IsNullOrWhiteSpace(dto.Gender)) return BadRequest("Vui lòng chọn giới tính.");
            if (string.IsNullOrWhiteSpace(dto.CitizenIdAddress)) return BadRequest("Vui lòng nhập địa chỉ theo giấy tờ.");
            var permanentProvinceCode = (dto.PermanentProvinceCode ?? string.Empty).Trim();
            var permanentWardCode = (dto.PermanentWardCode ?? string.Empty).Trim();
            var permanentDetail = (dto.PermanentDetail ?? string.Empty).Trim();
            var currentProvinceCode = dto.CurrentAddressSameAsPermanent
                ? permanentProvinceCode
                : (dto.CurrentProvinceCode ?? string.Empty).Trim();
            var currentWardCode = dto.CurrentAddressSameAsPermanent
                ? permanentWardCode
                : (dto.CurrentWardCode ?? string.Empty).Trim();
            var currentDetail = dto.CurrentAddressSameAsPermanent
                ? permanentDetail
                : (dto.CurrentDetail ?? string.Empty).Trim();

            if (permanentProvinceCode.Length != 2) return BadRequest("Vui lòng chọn tỉnh/thành phố thường trú hợp lệ.");
            if (permanentWardCode.Length != 5) return BadRequest("Vui lòng chọn xã/phường/đặc khu thường trú hợp lệ.");
            if (string.IsNullOrWhiteSpace(permanentDetail)) return BadRequest("Vui lòng nhập địa chỉ chi tiết thường trú.");
            if (currentProvinceCode.Length != 2) return BadRequest("Vui lòng chọn tỉnh/thành phố hiện tại hợp lệ.");
            if (currentWardCode.Length != 5) return BadRequest("Vui lòng chọn xã/phường/đặc khu hiện tại hợp lệ.");
            if (string.IsNullOrWhiteSpace(currentDetail)) return BadRequest("Vui lòng nhập địa chỉ chi tiết hiện tại.");
            if (string.IsNullOrWhiteSpace(dto.DriverLicenseNumber)) return BadRequest("Vui lòng nhập số giấy phép lái xe.");
            if (string.IsNullOrWhiteSpace(dto.DriverLicenseClass)) return BadRequest("Vui lòng nhập hạng giấy phép lái xe.");
            if (dto.DateOfBirth > SmartCar.Domain.Time.VietnamTime.Today.AddYears(-18)) return BadRequest("Khách thuê phải đủ 18 tuổi.");
            if (dto.CitizenIdIssuedDate > SmartCar.Domain.Time.VietnamTime.Today) return BadRequest("Ngày cấp CCCD không hợp lệ.");
            if (dto.CitizenIdExpiryDate <= SmartCar.Domain.Time.VietnamTime.Today) return BadRequest("CCCD phải còn hiệu lực.");
            if (dto.CitizenIdExpiryDate <= dto.CitizenIdIssuedDate) return BadRequest("Ngày hết hạn CCCD phải sau ngày cấp.");
            if (dto.DriverLicenseIssuedDate > SmartCar.Domain.Time.VietnamTime.Today) return BadRequest("Ngày cấp bằng lái không hợp lệ.");
            if (dto.DriverLicenseExpiry <= SmartCar.Domain.Time.VietnamTime.Today) return BadRequest("Bằng lái phải còn hiệu lực.");
            if (dto.DriverLicenseExpiry <= dto.DriverLicenseIssuedDate) return BadRequest("Ngày hết hạn bằng lái phải sau ngày cấp.");

            var citizenNumber = (dto.CitizenIdentityNumber ?? string.Empty).Trim();
            if (citizenNumber.Length != 12 || citizenNumber.Any(c => !char.IsDigit(c)))
                return BadRequest("CCCD phải gồm đúng 12 chữ số.");

            var userId = UserId();
            var citizenIdFingerprint = IdentityFingerprintSecurity.Compute(IdentityKey(), citizenNumber);

            // SQL Server được cấu hình EnableRetryOnFailure trong Program.cs. Mọi transaction
            // do ứng dụng tự mở phải nằm trong execution strategy, nếu không EF Core sẽ trả lỗi 500.
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                // Nếu execution strategy chạy lại sau lỗi SQL tạm thời, không tái sử dụng entity
                // đang được theo dõi từ lần thử trước.
                _context.ChangeTracker.Clear();

                await using var transaction = await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable,
                    cancellationToken);
                try
                {
                    var user = await _context.AppUsers
                        .FirstOrDefaultAsync(x => x.AppUserId == userId, cancellationToken);
                    if (user is null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return Unauthorized();
                    }

                    var provinceCodes = new[] { permanentProvinceCode, currentProvinceCode }.Distinct().ToArray();
                    var wardCodes = new[] { permanentWardCode, currentWardCode }.Distinct().ToArray();
                    var provinces = await _context.AdministrativeProvinces
                        .AsNoTracking()
                        .Where(x => provinceCodes.Contains(x.ProvinceCode) && x.IsActive)
                        .ToDictionaryAsync(x => x.ProvinceCode, cancellationToken);
                    var wards = await _context.AdministrativeWards
                        .AsNoTracking()
                        .Where(x => wardCodes.Contains(x.WardCode) && x.IsActive)
                        .ToDictionaryAsync(x => x.WardCode, cancellationToken);

                    if (!provinces.TryGetValue(permanentProvinceCode, out var permanentProvince))
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return BadRequest("Tỉnh/thành phố thường trú không tồn tại hoặc đã ngừng sử dụng.");
                    }
                    if (!wards.TryGetValue(permanentWardCode, out var permanentWard) ||
                        permanentWard.ProvinceCode != permanentProvinceCode)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return BadRequest("Xã/phường thường trú không thuộc tỉnh/thành phố đã chọn.");
                    }
                    if (!provinces.TryGetValue(currentProvinceCode, out var currentProvince))
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return BadRequest("Tỉnh/thành phố hiện tại không tồn tại hoặc đã ngừng sử dụng.");
                    }
                    if (!wards.TryGetValue(currentWardCode, out var currentWard) ||
                        currentWard.ProvinceCode != currentProvinceCode)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return BadRequest("Xã/phường hiện tại không thuộc tỉnh/thành phố đã chọn.");
                    }

                    var permanentProvinceName = FullAdministrativeName(permanentProvince.ProvinceType, permanentProvince.ProvinceName);
                    var permanentWardName = FullAdministrativeName(permanentWard.WardType, permanentWard.WardName);
                    var currentProvinceName = FullAdministrativeName(currentProvince.ProvinceType, currentProvince.ProvinceName);
                    var currentWardName = FullAdministrativeName(currentWard.WardType, currentWard.WardName);

                    var item = await _context.UserVerifications
                        .FirstOrDefaultAsync(
                            x => x.AppUserID == userId && x.VerificationType == "Khách thuê",
                            cancellationToken);
                    if (item is null)
                    {
                        item = new UserVerification
                        {
                            AppUserID = userId,
                            VerificationType = "Khách thuê"
                        };
                        _context.UserVerifications.Add(item);
                    }

                    var oldVerificationFileIds = new[]
                    {
                        item.CitizenIdFrontFileID,
                        item.CitizenIdBackFileID,
                        item.DriverLicenseFileID,
                        item.PortraitFileID
                    }.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();

                    user.Phone = (dto.Phone ?? string.Empty).Trim();
                    item.LegalFullName = (dto.LegalFullName ?? string.Empty).Trim();
                    item.Gender = (dto.Gender ?? string.Empty).Trim();
                    item.CitizenIdAddress = (dto.CitizenIdAddress ?? string.Empty).Trim();
                    item.PermanentProvinceCode = permanentProvinceCode;
                    item.PermanentWardCode = permanentWardCode;
                    item.PermanentProvince = permanentProvinceName;
                    item.PermanentWard = permanentWardName;
                    item.PermanentDetail = permanentDetail;
                    item.PermanentAddress = JoinAddress(permanentDetail, permanentWardName, permanentProvinceName);
                    item.CurrentAddressSameAsPermanent = dto.CurrentAddressSameAsPermanent;
                    item.CurrentProvinceCode = currentProvinceCode;
                    item.CurrentWardCode = currentWardCode;
                    item.CurrentProvince = currentProvinceName;
                    item.CurrentWard = currentWardName;
                    item.CurrentDetail = currentDetail;
                    item.CurrentAddress = JoinAddress(currentDetail, currentWardName, currentProvinceName);
                    item.DriverLicenseNumber = (dto.DriverLicenseNumber ?? string.Empty).Trim();
                    item.DriverLicenseClass = (dto.DriverLicenseClass ?? string.Empty).Trim();
                    item.CitizenIdIssuedDate = dto.CitizenIdIssuedDate.Date;
                    item.CitizenIdExpiryDate = dto.CitizenIdExpiryDate.Date;
                    item.CitizenIdMasked = MaskCitizenId(citizenNumber);
                    item.CitizenIdFingerprint = citizenIdFingerprint;

                    // Một CCCD chỉ được gắn với một hồ sơ khách. Serializable transaction
                    // giúp giảm nguy cơ hai yêu cầu đồng thời vượt qua bước kiểm tra này.
                    var duplicateCitizenId = await _context.UserVerifications.AsNoTracking().AnyAsync(
                        x => x.AppUserID != userId && x.CitizenIdFingerprint == citizenIdFingerprint,
                        cancellationToken);
                    if (duplicateCitizenId)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return Conflict("CCCD đã được sử dụng cho một tài khoản khách khác.");
                    }

                    // Hồ sơ mới chỉ nhận FileId. Lấy entity có tracking để vừa kiểm tra quyền,
                    // vừa đánh dấu AttachedDate trong cùng transaction với UserVerification.
                    var verificationFiles = new List<PrivateFile>();
                    if (dto.CitizenIdFrontFileId.HasValue)
                    {
                        var files = await _files.ValidateForAttachmentAsync(
                            new[] { dto.CitizenIdFrontFileId.Value },
                            userId,
                            "CustomerCitizenIdFront",
                            null,
                            false,
                            cancellationToken);
                        verificationFiles.AddRange(files);
                        item.CitizenIdFrontFileID = dto.CitizenIdFrontFileId;
                        item.CitizenIdFrontUrl = null;
                    }
                    if (dto.CitizenIdBackFileId.HasValue)
                    {
                        var files = await _files.ValidateForAttachmentAsync(
                            new[] { dto.CitizenIdBackFileId.Value },
                            userId,
                            "CustomerCitizenIdBack",
                            null,
                            false,
                            cancellationToken);
                        verificationFiles.AddRange(files);
                        item.CitizenIdBackFileID = dto.CitizenIdBackFileId;
                        item.CitizenIdBackUrl = null;
                    }
                    if (dto.DriverLicenseFileId.HasValue)
                    {
                        var files = await _files.ValidateForAttachmentAsync(
                            new[] { dto.DriverLicenseFileId.Value },
                            userId,
                            "CustomerDriverLicense",
                            null,
                            false,
                            cancellationToken);
                        verificationFiles.AddRange(files);
                        item.DriverLicenseFileID = dto.DriverLicenseFileId;
                        item.DriverLicenseUrl = null;
                    }
                    if (dto.PortraitFileId.HasValue)
                    {
                        var files = await _files.ValidateForAttachmentAsync(
                            new[] { dto.PortraitFileId.Value },
                            userId,
                            "CustomerPortrait",
                            null,
                            false,
                            cancellationToken);
                        verificationFiles.AddRange(files);
                        item.PortraitFileID = dto.PortraitFileId;
                        item.PortraitUrl = null;
                    }

                    if (!item.CitizenIdFrontFileID.HasValue && string.IsNullOrWhiteSpace(item.CitizenIdFrontUrl))
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return BadRequest("Vui lòng tải ảnh CCCD mặt trước.");
                    }
                    if (!item.CitizenIdBackFileID.HasValue && string.IsNullOrWhiteSpace(item.CitizenIdBackUrl))
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return BadRequest("Vui lòng tải ảnh CCCD mặt sau.");
                    }
                    if (!item.DriverLicenseFileID.HasValue && string.IsNullOrWhiteSpace(item.DriverLicenseUrl))
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return BadRequest("Vui lòng tải ảnh bằng lái.");
                    }
                    if (!item.PortraitFileID.HasValue && string.IsNullOrWhiteSpace(item.PortraitUrl))
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return BadRequest("Vui lòng tải ảnh chân dung.");
                    }

                    item.DateOfBirth = dto.DateOfBirth.Date;
                    item.DriverLicenseIssuedDate = dto.DriverLicenseIssuedDate.Date;
                    item.DriverLicenseExpiry = dto.DriverLicenseExpiry.Date;
                    item.Status = "Chờ duyệt";
                    item.RejectionReason = null;
                    item.ReviewedByAppUserID = null;
                    item.ReviewedDate = null;
                    item.CreatedDate = DateTime.UtcNow;

                    // Lưu trước để hồ sơ mới có UserVerificationID thật.
                    await _context.SaveChangesAsync(cancellationToken);

                    if (verificationFiles.Count > 0)
                    {
                        _files.MarkAttached(
                            verificationFiles,
                            nameof(UserVerification),
                            item.UserVerificationID.ToString());
                    }

                    var effectiveVerificationFileIds = new[]
                    {
                        item.CitizenIdFrontFileID,
                        item.CitizenIdBackFileID,
                        item.DriverLicenseFileID,
                        item.PortraitFileID
                    }.Where(x => x.HasValue).Select(x => x!.Value).ToHashSet();

                    var replacedVerificationIds = oldVerificationFileIds
                        .Where(x => !effectiveVerificationFileIds.Contains(x))
                        .ToArray();
                    if (replacedVerificationIds.Length > 0)
                    {
                        var replacedFiles = await _context.PrivateFiles
                            .Where(x => replacedVerificationIds.Contains(x.PrivateFileID) &&
                                        x.OwnerAppUserID == userId &&
                                        !x.IsDeleted)
                            .ToListAsync(cancellationToken);
                        var deleteAt = DateTime.UtcNow;
                        foreach (var file in replacedFiles)
                        {
                            file.IsDeleted = true;
                            file.DeleteRequestedDate = deleteAt;
                            file.LastDeleteError = null;
                            file.AttachedEntityType = null;
                            file.AttachedEntityID = null;
                            file.AttachedDate = null;
                        }
                    }

                    // Mở lại toàn bộ công việc duyệt cũ để hồ sơ gửi lại xuất hiện ở hàng đợi.
                    var oldClaims = await _context.WorkItemClaims
                        .Where(x => x.QueueType == "Xác minh khách" &&
                                    x.EntityID == item.UserVerificationID)
                        .ToListAsync(cancellationToken);
                    foreach (var oldClaim in oldClaims)
                    {
                        oldClaim.Status = "Đã nhả";
                        oldClaim.DueAt = null;
                        oldClaim.AssignedAt = DateTime.UtcNow;
                    }

                    _context.AuditLogs.Add(new AuditLog
                    {
                        AppUserID = userId,
                        Action = "Gửi hồ sơ xác minh",
                        EntityName = nameof(UserVerification),
                        EntityID = item.UserVerificationID.ToString(),
                        Note = oldClaims.Count == 0
                            ? "Hồ sơ chuyển về Chờ duyệt; chưa có công việc cũ cần mở lại."
                            : $"Hồ sơ chuyển về Chờ duyệt; đã mở lại {oldClaims.Count} công việc duyệt cũ; thay thế {replacedVerificationIds.Length} tệp cũ.",
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                    });

                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return Ok("Đã gửi hồ sơ xác minh. Hồ sơ đã xuất hiện trong hàng đợi cần xử lý của nhân viên.");
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }

        [Authorize]
        [HttpGet("reservations/{id:int}/detail")]
        public async Task<IActionResult> GetReservationDetail(int id)
        {
            var reservation = await _context.Reservations.AsNoTracking()
                .Include(x => x.CustomerAppUser)
                .Include(x => x.Car).ThenInclude(x => x.Brand)
                .Include(x => x.Car).ThenInclude(x => x.CarPricings).ThenInclude(x => x.Pricing)
                .Include(x => x.PickUpLocation)
                .Include(x => x.DropOffLocation)
                .Include(x => x.PartnerVehicle).ThenInclude(x => x.OwnerAppUser)
                .Include(x => x.PartnerVehicle).ThenInclude(x => x.VehiclePartnerApplication)
                .FirstOrDefaultAsync(x => x.ReservationID == id);
            if (reservation is null) return NotFound("Không tìm thấy đơn thuê.");
            if (!CanAccess(reservation)) return Forbid();

            var histories = await _context.ReservationStatusHistories.AsNoTracking().Where(x => x.ReservationID == id).OrderBy(x => x.ChangedDate).ToListAsync();
            var payments = await _context.Payments.AsNoTracking().Where(x => x.ReservationID == id).OrderByDescending(x => x.CreatedDate).ToListAsync();
            var requiredHoldPaymentType = GetHoldPaymentType(reservation);
            var hasPendingHoldPaymentProof = payments.Any(x => x.PaymentType == requiredHoldPaymentType && x.Status == "Chờ xác nhận");
            var handovers = await _context.HandoverReports.AsNoTracking().Where(x => x.ReservationID == id).OrderBy(x => x.CreatedDate).ToListAsync();
            var charges = await _context.AdditionalCharges.AsNoTracking().Where(x => x.ReservationID == id).OrderByDescending(x => x.CreatedDate).ToListAsync();
            var successfulRentalPaid = payments.Where(x => x.PaymentType == PaymentTypes.Rental && x.Status == "Thành công").Sum(x => x.Amount);
            var successfulReservationDepositPaid = payments.Where(x => x.PaymentType == PaymentTypes.ReservationDeposit && x.Status == "Thành công").Sum(x => x.Amount);
            var successfulSecurityDepositPaid = payments.Where(x => x.PaymentType == PaymentTypes.SecurityDeposit && x.Status == "Thành công").Sum(x => x.Amount);
            var rentalPaymentDue = Math.Max(0m, reservation.TotalPrice - successfulRentalPaid - Math.Min(reservation.ReservationDepositAmount, successfulReservationDepositPaid));
            var securityDepositDue = Math.Max(0m, reservation.SecurityDepositAmount - successfulSecurityDepositPaid);
            var hasPendingRentalPayment = payments.Any(x => x.PaymentType == PaymentTypes.Rental && x.Status == "Chờ xác nhận");
            var hasPendingSecurityDeposit = payments.Any(x => x.PaymentType == PaymentTypes.SecurityDeposit && x.Status == "Chờ xác nhận");
            var incidents = await _context.Incidents.AsNoTracking().Where(x => x.ReservationID == id).OrderByDescending(x => x.CreatedDate).ToListAsync();
            var disputes = await _context.Disputes.AsNoTracking().Where(x => x.ReservationID == id).OrderByDescending(x => x.CreatedDate).ToListAsync();
            var disputeIds = disputes.Select(x => x.DisputeID).ToList();
            var disputeMessages = await _context.DisputeMessages.AsNoTracking().Where(x => disputeIds.Contains(x.DisputeID)).OrderBy(x => x.CreatedDate).ToListAsync();
            var messageUserIds = disputeMessages.Select(x => x.SenderAppUserID).Distinct().ToList();
            var messageUsers = await _context.AppUsers.AsNoTracking().Where(x => messageUserIds.Contains(x.AppUserId)).ToDictionaryAsync(x => x.AppUserId);
            var trafficFines = await _context.TrafficFines.AsNoTracking().Where(x => x.ReservationID == id).OrderByDescending(x => x.ViolationAt).ToListAsync();
            var settlement = await _context.Settlements.AsNoTracking().FirstOrDefaultAsync(x => x.ReservationID == id);
            var verification = await _context.UserVerifications.AsNoTracking().FirstOrDefaultAsync(x => x.AppUserID == reservation.CustomerAppUserID && x.VerificationType == "Khách thuê");

            var dailyPrice = reservation.Car.CarPricings.Where(x => x.Pricing != null && x.Pricing.Name == "Theo ngày").Select(x => x.Amount).FirstOrDefault();
            var start = reservation.PickUpDate.Date.Add(reservation.PickUpTime);
            var end = reservation.DropOffDate.Date.Add(reservation.DropOffTime);
            var days = Math.Max(1, (int)Math.Ceiling((end - start).TotalDays));
            var isCustomer = reservation.CustomerAppUserID == UserId();
            var isOwner = reservation.PartnerVehicle.OwnerAppUserID == UserId();
            var canManage = User.IsInRole("Admin") || User.IsInRole("Staff") || isOwner;

            return Ok(new ReservationDetailDto
            {
                Reservation = MapReservation(reservation),
                Price = BuildPrice(dailyPrice, days, dailyPrice * days, reservation.DepositAmount,
                    reservation.DeliveryMethod == "Nhận tại điểm giao xe" ? 0 : reservation.PartnerVehicle.VehiclePartnerApplication.DeliveryFee,
                    reservationDeposit: reservation.ReservationDepositAmount,
                    securityDeposit: reservation.SecurityDepositAmount),
                VerificationStatus = verification?.Status ?? "Chưa xác minh",
                DriverLicenseExpiry = verification?.DriverLicenseExpiry,
                HoldExpiresAt = reservation.HoldExpiresAt,
                CanCancel = isCustomer
                    && VietnamTime.LocalToUtc(reservation.PickUpDate, reservation.PickUpTime) > DateTime.UtcNow
                    && (reservation.Status is "Chờ chủ xe xác nhận" or ReservationStatuses.PaymentPending or "Chờ khách đặt cọc" or "Chờ khách thanh toán giữ chỗ" or "Chờ nhân viên xác nhận cọc" or "Chờ nhân viên xác nhận thanh toán" or "Đã đặt cọc" or "Đã thanh toán" or "Đã xác nhận" or "Chờ giao xe"),
                CanSubmitDepositProof = isCustomer &&
                    (reservation.Status is ReservationStatuses.PaymentPending or "Chờ khách đặt cọc" or "Chờ khách thanh toán giữ chỗ") &&
                    reservation.HoldExpiresAt > DateTime.UtcNow && !hasPendingHoldPaymentProof,
                CanSubmitRentalPaymentProof = isCustomer && rentalPaymentDue > 0 &&
                    !hasPendingRentalPayment && (reservation.Status is ReservationStatuses.Confirmed or ReservationStatuses.HandoverPending),
                RentalPaymentDue = rentalPaymentDue,
                CanSubmitSecurityDepositProof = isCustomer && reservation.RentalMode == ServiceTypes.SelfDrive && securityDepositDue > 0 &&
                    !hasPendingSecurityDeposit && (reservation.Status is ReservationStatuses.Confirmed or ReservationStatuses.HandoverPending),
                SecurityDepositDue = securityDepositDue,
                CanCreateDeliveryReport = canManage && rentalPaymentDue <= 0 && securityDepositDue <= 0 &&
                    (reservation.Status is ReservationStatuses.Confirmed or ReservationStatuses.HandoverPending or "Đã đặt cọc") && !handovers.Any(x => x.ReportType == "Giao xe" && !x.IsSuperseded),
                CanCreateReturnReport = canManage && (reservation.Status is "Đang thuê" or "Chờ trả xe") && !handovers.Any(x => x.ReportType == "Trả xe" && !x.IsSuperseded),
                CanReportIncident = (isCustomer || isOwner) && (reservation.Status is "Đang thuê" or "Chờ trả xe" or "Chờ đối soát"),
                CanOpenDispute = (isCustomer || isOwner) && reservation.Status is not ("Đã hủy" or "Hoàn thành"),
                CanOwnerDecide = isOwner && reservation.Status == "Chờ chủ xe xác nhận" &&
                    !ReservationAvailabilityRules.IsOwnerResponseExpired(reservation.CreatedDate, DateTime.UtcNow),
                Timeline = BuildTimeline(reservation, histories),
                Payments = payments.Select(x => new ReservationPaymentDto { PaymentID = x.PaymentID, PaymentType = x.PaymentType, Amount = x.Amount, ProviderFeeAmount = x.ProviderFeeAmount, ProviderFeeVerified = x.ProviderFeeVerified, Status = x.Status, TransactionCode = x.TransactionCode, Provider = x.Provider, TransferContent = x.TransferContent, CustomerReportedDate = x.CustomerReportedDate, IsSimulated = x.IsSimulated, VerificationNote = x.VerificationNote, RelatedEntityType = x.RelatedEntityType, RelatedEntityID = x.RelatedEntityID, CreatedDate = x.CreatedDate, ConfirmedDate = x.ConfirmedDate }).ToList(),
                Handovers = handovers.Select(x => new ReservationHandoverDto { HandoverReportID = x.HandoverReportID, ReportType = x.ReportType, OdometerKm = x.OdometerKm, FuelPercent = x.FuelPercent, ExistingDamage = x.ExistingDamage, Accessories = x.Accessories, LocationText = x.LocationText, PhotoUrls = x.PhotoUrls, CreatedDate = x.CreatedDate, ConfirmedDate = x.ConfirmedDate, IsLocked = x.IsLocked, IsSuperseded = x.IsSuperseded, OtpExpiresAt = x.OtpExpiresAt }).ToList(),
                AdditionalCharges = charges.Select(x =>
                {
                    var chargePayment = x.PaymentID.HasValue ? payments.FirstOrDefault(p => p.PaymentID == x.PaymentID.Value) : null;
                    return new ReservationAdditionalChargeDto
                    {
                        AdditionalChargeID = x.AdditionalChargeID,
                        ChargeType = x.ChargeType,
                        Amount = x.Amount,
                        Reason = x.Reason,
                        EvidenceUrls = x.EvidenceUrls,
                        Status = x.Status,
                        PaymentID = x.PaymentID,
                        PaymentStatus = chargePayment?.Status,
                        CanSubmitPaymentProof = isCustomer && (x.Status == AdditionalChargeStatuses.CustomerAccepted || x.Status == AdditionalChargeStatuses.Approved) && chargePayment?.Status is not ("Chờ xác nhận" or "Thành công") && reservation.Status is not ("Đã hủy" or "Hoàn thành"),
                        CreatedDate = x.CreatedDate
                    };
                }).ToList(),
                Incidents = incidents.Select(x => new ReservationIncidentDto { IncidentID = x.IncidentID, Type = x.Type, Description = x.Description, LocationText = x.LocationText, EvidenceUrls = x.EvidenceUrls, Status = x.Status, VehicleImmobilized = x.VehicleImmobilized, OccurredAt = x.OccurredAt, CreatedDate = x.CreatedDate }).ToList(),
                Disputes = disputes.Select(x => new ReservationDisputeDto
                {
                    DisputeID = x.DisputeID,
                    Type = x.Type,
                    Description = x.Description,
                    EvidenceUrls = x.EvidenceUrls,
                    Status = x.Status,
                    Resolution = x.Resolution,
                    CompensationAmount = x.CompensationAmount,
                    AssignedStaffAppUserID = x.AssignedStaffAppUserID,
                    CreatedDate = x.CreatedDate,
                    Messages = disputeMessages.Where(m => m.DisputeID == x.DisputeID).Select(m => new ReservationDisputeMessageDto
                    {
                        DisputeMessageID = m.DisputeMessageID,
                        SenderAppUserID = m.SenderAppUserID,
                        SenderName = messageUsers.TryGetValue(m.SenderAppUserID, out var sender) ? $"{sender.Surname} {sender.Name}".Trim() : $"Tài khoản #{m.SenderAppUserID}",
                        Message = m.Message,
                        EvidenceUrls = m.EvidenceUrls,
                        CreatedDate = m.CreatedDate
                    }).ToList()
                }).ToList(),
                TrafficFines = trafficFines.Select(x => new ReservationTrafficFineDto { TrafficFineID = x.TrafficFineID, ViolationAt = x.ViolationAt, Violation = x.Violation, LocationText = x.LocationText, Amount = x.Amount, NoticeNumber = x.NoticeNumber, EvidenceUrl = x.EvidenceUrl, Status = x.Status, DueDate = x.DueDate, CreatedDate = x.CreatedDate }).ToList(),
                Settlement = settlement is null ? null : new ReservationSettlementDto { SettlementID = settlement.SettlementID, GrossRental = settlement.GrossRental, PlatformFee = settlement.PlatformFee, PaymentGatewayFee = settlement.PaymentGatewayFee, RefundAmount = settlement.RefundAmount, CompensationAmount = settlement.CompensationAmount, OwnerPayout = settlement.OwnerPayout, Status = settlement.Status, PayoutTransactionCode = settlement.PayoutTransactionCode, CreatedByAppUserID = settlement.CreatedByAppUserID, ApprovedByAppUserID = settlement.ApprovedByAppUserID, CanApprovePayment = User.IsInRole("Admin") && (!settlement.CreatedByAppUserID.HasValue || settlement.CreatedByAppUserID.Value != UserId()) && settlement.Status == SettlementStatuses.AwaitingApproval, CreatedDate = settlement.CreatedDate, PaidDate = settlement.PaidDate }
            });
        }

        [Authorize(Roles = "Customer")]
        [HttpPost("reservations/{id:int}/payment-proof")]
        public async Task<IActionResult> SubmitPaymentProof(int id, SubmitPaymentProofDto dto)
        {
            if (IsVehiclePartnerAccount()) return Forbid();

            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var reservation = await _context.Reservations
                    .FirstOrDefaultAsync(x => x.ReservationID == id && x.CustomerAppUserID == UserId());
                if (reservation is null) return NotFound("Không tìm thấy đơn thuộc tài khoản này.");
                if (reservation.Status is "Đã hủy" or "Hoàn thành")
                    return Conflict("Đơn đã đóng, không thể báo thêm thanh toán.");

                var holdType = GetHoldPaymentType(reservation);
                var type = string.IsNullOrWhiteSpace(dto.PaymentType) ? holdType : (dto.PaymentType ?? string.Empty).Trim();
                if (type is not (PaymentTypes.LegacyDeposit or PaymentTypes.ReservationDeposit or PaymentTypes.SecurityDeposit or PaymentTypes.Rental or PaymentTypes.AdditionalCharge))
                    return BadRequest("Loại thanh toán không hợp lệ.");

                var isHoldPayment = type == holdType && (reservation.Status is ReservationStatuses.PaymentPending or "Chờ khách đặt cọc" or "Chờ khách thanh toán giữ chỗ");
                AdditionalCharge? charge = null;
                decimal expected;
                string transferContent;

                if (type is PaymentTypes.LegacyDeposit or PaymentTypes.ReservationDeposit)
                {
                    if (type != holdType || !isHoldPayment)
                        return Conflict("Đơn không ở bước thanh toán cọc giữ chỗ.");
                    expected = type == PaymentTypes.ReservationDeposit ? reservation.ReservationDepositAmount : reservation.DepositAmount;
                    transferContent = $"SC{reservation.ReservationID:D6}-BOOK";
                }
                else if (type == PaymentTypes.SecurityDeposit)
                {
                    if (reservation.RentalMode != ServiceTypes.SelfDrive || reservation.SecurityDepositAmount <= 0)
                        return Conflict("Đơn không có khoản cọc bảo đảm cần thanh toán.");
                    if (reservation.Status is not (ReservationStatuses.Confirmed or ReservationStatuses.HandoverPending))
                        return Conflict("Cọc bảo đảm chỉ được thanh toán sau khi đơn đã xác nhận và trước khi giao xe.");
                    var paid = await _context.Payments
                        .Where(x => x.ReservationID == id && x.PaymentType == PaymentTypes.SecurityDeposit && x.Status == "Thành công")
                        .SumAsync(x => (decimal?)x.Amount) ?? 0m;
                    expected = reservation.SecurityDepositAmount - paid;
                    transferContent = $"SC{reservation.ReservationID:D6}-SEC";
                }
                else if (type == PaymentTypes.Rental)
                {
                    if (!isHoldPayment && reservation.Status is not (ReservationStatuses.Confirmed or ReservationStatuses.HandoverPending))
                        return Conflict("Tiền thuê phải được thanh toán sau khi đơn xác nhận và trước khi giao xe.");
                    var paid = await _context.Payments
                        .Where(x => x.ReservationID == id && x.PaymentType == PaymentTypes.Rental && x.Status == "Thành công")
                        .SumAsync(x => (decimal?)x.Amount) ?? 0m;
                    var paidBookingDeposit = await _context.Payments
                        .Where(x => x.ReservationID == id && x.PaymentType == PaymentTypes.ReservationDeposit && x.Status == "Thành công")
                        .SumAsync(x => (decimal?)x.Amount) ?? 0m;
                    expected = reservation.TotalPrice - paid - Math.Min(reservation.ReservationDepositAmount, paidBookingDeposit);
                    transferContent = $"SC{reservation.ReservationID:D6}-RENT";
                }
                else
                {
                    if (!dto.AdditionalChargeID.HasValue || dto.AdditionalChargeID.Value <= 0)
                        return BadRequest("Phải xác định phụ phí cần thanh toán.");
                    charge = await _context.AdditionalCharges
                        .FirstOrDefaultAsync(x => x.AdditionalChargeID == dto.AdditionalChargeID.Value && x.ReservationID == id);
                    if (charge is null) return NotFound("Không tìm thấy phụ phí thuộc đơn này.");
                    if (charge.Status is not (AdditionalChargeStatuses.CustomerAccepted or AdditionalChargeStatuses.Approved)) return Conflict("Phụ phí chưa được khách chấp nhận hoặc nhân viên duyệt.");
                    if (charge.PaymentID.HasValue)
                    {
                        var linkedStatus = await _context.Payments.Where(x => x.PaymentID == charge.PaymentID.Value).Select(x => x.Status).FirstOrDefaultAsync();
                        if (linkedStatus is "Chờ xác nhận" or "Thành công") return Conflict("Phụ phí này đã có giao dịch đang xử lý hoặc đã thanh toán.");
                    }
                    expected = charge.Amount;
                    transferContent = $"SC{reservation.ReservationID:D6}-CHG{charge.AdditionalChargeID}";
                }

                if (expected <= 0) return Conflict($"Khoản {type.ToLowerInvariant()} đã được xác nhận đủ hoặc không còn số tiền phải thu.");

                if (isHoldPayment)
                {
                    if (!reservation.HoldExpiresAt.HasValue || reservation.HoldExpiresAt.Value <= DateTime.UtcNow)
                    {
                        var oldStatus = reservation.Status;
                        reservation.Status = ReservationStatuses.PaymentExpired;
                        _context.ReservationStatusHistories.Add(new ReservationStatusHistory
                        {
                            ReservationID = reservation.ReservationID,
                            OldStatus = oldStatus,
                            NewStatus = reservation.Status,
                            ChangedByAppUserID = UserId(),
                            Note = "Thời gian giữ thanh toán đã hết trước khi khách báo đã chuyển khoản."
                        });
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return Conflict("Thời gian giữ xe đã hết. Vui lòng đặt lại xe để nhận thời hạn thanh toán mới.");
                    }
                }

                Payment? payment = null;
                if (charge?.PaymentID is int existingPaymentId)
                    payment = await _context.Payments.FirstOrDefaultAsync(x => x.PaymentID == existingPaymentId);
                payment ??= await _context.Payments.FirstOrDefaultAsync(x =>
                    x.ReservationID == id && x.PaymentType == type && x.Status == "Chờ xác nhận" &&
                    (type != PaymentTypes.AdditionalCharge || (x.RelatedEntityType == nameof(AdditionalCharge) && x.RelatedEntityID == charge!.AdditionalChargeID)));

                if (payment is null)
                {
                    payment = new Payment
                    {
                        ReservationID = id,
                        PaymentType = type,
                        RelatedEntityType = charge is null ? null : nameof(AdditionalCharge),
                        RelatedEntityID = charge?.AdditionalChargeID,
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.Payments.Add(payment);
                }

                payment.Amount = expected;
                payment.TransactionCode = null;
                payment.Provider = string.IsNullOrWhiteSpace(dto.Provider) ? "Chuyển khoản ngân hàng thủ công" : (dto.Provider ?? string.Empty).Trim();
                payment.TransferContent = transferContent;
                payment.CustomerReportedDate = DateTime.UtcNow;
                payment.IsSimulated = false;
                payment.VerificationNote = null;
                payment.Status = "Chờ xác nhận";
                payment.RelatedEntityType = charge is null ? null : nameof(AdditionalCharge);
                payment.RelatedEntityID = charge?.AdditionalChargeID;

                if (payment.PaymentID == 0) await _context.SaveChangesAsync();
                if (charge is not null) charge.PaymentID = payment.PaymentID;

                if (isHoldPayment)
                {
                    var previousStatus = reservation.Status;
                    reservation.Status = type is PaymentTypes.LegacyDeposit or PaymentTypes.ReservationDeposit ? "Chờ nhân viên xác nhận cọc" : "Chờ nhân viên xác nhận thanh toán";
                    _context.ReservationStatusHistories.Add(new ReservationStatusHistory
                    {
                        ReservationID = reservation.ReservationID,
                        OldStatus = previousStatus,
                        NewStatus = reservation.Status,
                        ChangedByAppUserID = UserId(),
                        Note = $"Khách báo đã chuyển khoản với nội dung {transferContent} đúng thời hạn; giữ đơn để nhân viên đối chiếu thủ công."
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok($"Đã ghi nhận thông báo chuyển khoản {type.ToLowerInvariant()}. SmartCar đang đối chiếu sao kê; vui lòng không chuyển lại lần nữa.");
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                return Conflict("Khoản thanh toán đã được tạo hoặc xử lý bởi thao tác khác. Vui lòng tải lại đơn.");
            }
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet("staff/work-queue")]
        public async Task<IActionResult> GetStaffWorkQueue()
        {
            var now = DateTime.UtcNow;
            var sourceItems = new List<StaffQueueItemDto>();

            var verifications = await _context.UserVerifications.AsNoTracking()
                .Include(x => x.AppUser)
                .Where(x => x.Status == "Chờ duyệt")
                .OrderBy(x => x.CreatedDate).ToListAsync();
            sourceItems.AddRange(verifications.Select(x => Queue(
                "Xác minh khách", x.UserVerificationID, null,
                $"Hồ sơ {x.AppUser.Surname} {x.AppUser.Name}",
                "CCCD, bằng lái và ảnh chân dung chờ duyệt",
                x.Status, x.CreatedDate, "Cao",
                $"/StaffDashboard/Verification/{x.UserVerificationID}")));

            var partnerProfiles = await _context.VehiclePartnerProfiles.AsNoTracking()
                .Include(x => x.AppUser)
                .Where(x => x.Status == "Chờ duyệt")
                .OrderBy(x => x.SubmittedDate ?? x.CreatedDate)
                .ToListAsync();
            sourceItems.AddRange(partnerProfiles.Select(x => Queue(
                "Hồ sơ đối tác", x.VehiclePartnerProfileID, null,
                x.PartnerType == "Doanh nghiệp/Tổ chức" && !string.IsNullOrWhiteSpace(x.BusinessName)
                    ? $"Xác minh {x.BusinessName}"
                    : $"Xác minh {x.FullName}",
                $"{x.PartnerType} · {x.Phone} · {x.Email}",
                x.Status, x.SubmittedDate ?? x.CreatedDate, "Cao",
                "/Admin/AdminVehiclePartner/Index")));

            var applications = await _context.VehiclePartnerApplications.AsNoTracking()
                .Where(x => x.Status == "Chờ duyệt")
                .OrderBy(x => x.CreatedDate).ToListAsync();
            sourceItems.AddRange(applications.Select(x => Queue(
                "Hồ sơ xe", x.VehiclePartnerApplicationID, null,
                $"{x.BrandName} {x.Model} - {x.LicensePlate}",
                x.OwnerFullName, x.Status, x.CreatedDate, "Cao",
                "/Admin/AdminVehiclePartner/Index")));

            var documents = await _context.VehicleDocuments.AsNoTracking()
                .Include(x => x.PartnerVehicle).ThenInclude(x => x.Car)
                .Where(x => x.Status == "Chờ duyệt")
                .OrderBy(x => x.CreatedDate).ToListAsync();
            sourceItems.AddRange(documents.Select(x => Queue(
                "Giấy tờ xe", x.VehicleDocumentID, null,
                x.DocumentType, $"Xe #{x.PartnerVehicle.CarID}",
                x.Status, x.CreatedDate, "Cao",
                $"/StaffDashboard/VehicleDocument/{x.VehicleDocumentID}")));

            var payments = await _context.Payments.AsNoTracking()
                .Include(x => x.Reservation)
                .Where(x => x.Status == "Chờ xác nhận")
                .OrderBy(x => x.CreatedDate).ToListAsync();
            foreach (var payment in payments)
            {
                var transferContent = payment.TransferContent ?? $"SC{payment.ReservationID:D6}";
                var item = Queue(
                    "Thanh toán", payment.PaymentID, payment.ReservationID,
                    $"{payment.PaymentType} đơn #{payment.ReservationID}",
                    $"{payment.Amount.ToString("#,0", CultureInfo.InvariantCulture)} đồng · Nội dung {transferContent}",
                    payment.Status, payment.CreatedDate, "Khẩn cấp",
                    $"/ReservationLookup/Details/{payment.ReservationID}");

                if (payment.Reservation.Status is "Đã hủy" or "Hoàn thành" or "Bị từ chối")
                {
                    item.Bucket = "Quá hạn / lỗi";
                    item.Status = "Lỗi dữ liệu";
                    item.IsActionable = false;
                    item.IssueReason = $"Giao dịch vẫn chờ xác nhận nhưng đơn đã ở trạng thái '{payment.Reservation.Status}'. Cần quản trị kiểm tra hoặc hoàn tiền, không xác nhận tự động.";
                }
                else if (payment.Reservation.Status == "Hết hạn thanh toán")
                {
                    item.Bucket = "Quá hạn / lỗi";
                    item.Status = "Cần kiểm tra thời điểm chuyển tiền";
                    item.IssueReason = "Đơn đã hết hạn thanh toán nhưng vẫn còn giao dịch chờ đối chiếu. Cần kiểm tra khách gửi trước hay sau hạn để quyết định khôi phục hoặc hoàn tiền.";
                }
                sourceItems.Add(item);
            }

            var closedIncidentStatuses = new[] { "Đã xử lý", "Đã giải quyết", "Đã đóng", "Đã hủy", "Bị từ chối" };
            var incidents = await _context.Incidents.AsNoTracking()
                .Where(x => !closedIncidentStatuses.Contains(x.Status))
                .OrderBy(x => x.CreatedDate).ToListAsync();
            sourceItems.AddRange(incidents.Select(x => Queue(
                "Sự cố", x.IncidentID, x.ReservationID,
                x.Type, x.Description, x.Status, x.CreatedDate,
                x.VehicleImmobilized ? "Khẩn cấp" : "Cao",
                $"/ReservationLookup/Details/{x.ReservationID}")));

            var closedDisputeStatuses = new[] { "Đã giải quyết", "Đã đóng", "Đã hủy", "Bị từ chối" };
            var disputes = await _context.Disputes.AsNoTracking()
                .Where(x => !closedDisputeStatuses.Contains(x.Status))
                .OrderBy(x => x.CreatedDate).ToListAsync();
            sourceItems.AddRange(disputes.Select(x =>
            {
                var item = Queue(
                    "Tranh chấp", x.DisputeID, x.ReservationID,
                    x.Type, x.Description, x.Status, x.CreatedDate,
                    "Khẩn cấp", $"/ReservationLookup/Details/{x.ReservationID}");
                return item;
            }));

            var overdue = await _context.TrafficFines.AsNoTracking()
                .Where(x => x.Status != "Đã thanh toán" && x.Status != "Đã hủy" && x.DueDate < now)
                .OrderBy(x => x.DueDate).ToListAsync();
            sourceItems.AddRange(overdue.Select(x => Queue(
                "Phạt nguội", x.TrafficFineID, x.ReservationID,
                x.Violation, $"Quá hạn · {x.Amount.ToString("#,0", CultureInfo.InvariantCulture)} đồng",
                x.Status, x.CreatedDate, "Cao",
                $"/ReservationLookup/Details/{x.ReservationID}")));

            var reservationsAwaitingSettlement = await _context.Reservations.AsNoTracking()
                .Where(x => x.Status == "Chờ đối soát" && !_context.Settlements.Any(s => s.ReservationID == x.ReservationID))
                .OrderBy(x => x.DropOffDate).ToListAsync();
            sourceItems.AddRange(reservationsAwaitingSettlement.Select(x => Queue(
                "Đối soát", x.ReservationID, x.ReservationID,
                $"Lập đối soát đơn #{x.ReservationID}",
                $"Tổng tiền thuê {x.TotalPrice.ToString("#,0", CultureInfo.InvariantCulture)} đồng · cọc {x.DepositAmount.ToString("#,0", CultureInfo.InvariantCulture)} đồng",
                x.Status, x.CreatedDate, "Cao",
                $"/ReservationLookup/Details/{x.ReservationID}")));

            var claims = await _context.WorkItemClaims.AsNoTracking()
                .OrderByDescending(x => x.WorkItemClaimID)
                .Take(300)
                .ToListAsync();
            var staffIds = claims.Select(x => x.AssignedStaffAppUserID).Distinct().ToList();
            var staffNames = await _context.AppUsers.AsNoTracking()
                .Where(x => staffIds.Contains(x.AppUserId))
                .ToDictionaryAsync(x => x.AppUserId, x => ($"{x.Surname} {x.Name}").Trim());

            var matchedClaimIds = new HashSet<int>();
            foreach (var queueItem in sourceItems)
            {
                // Luôn dùng claim mới nhất. CSDL cũ có thể tồn tại claim trùng cho cùng
                // QueueType + EntityID; lấy một bản ghi không xác định sẽ làm bộ đếm có dữ liệu
                // nhưng bảng mặc định không hiển thị công việc.
                var claim = claims.FirstOrDefault(x => x.QueueType == queueItem.QueueType && x.EntityID == queueItem.EntityID);
                if (claim is null)
                {
                    if (queueItem.Bucket != "Quá hạn / lỗi") queueItem.Bucket = "Cần xử lý";
                    continue;
                }

                matchedClaimIds.Add(claim.WorkItemClaimID);
                queueItem.WorkItemClaimID = claim.WorkItemClaimID;
                queueItem.DueAt = claim.DueAt;

                // Tự phục hồi dữ liệu cũ: nếu đối tượng nghiệp vụ được gửi/cập nhật sau lần
                // claim gần nhất thì đây là một phiên bản mới và phải quay lại Cần xử lý,
                // dù claim cũ còn mang trạng thái Đang xử lý hoặc Đã hoàn tất.
                var isNewSubmissionAfterClaim = queueItem.CreatedDate > claim.AssignedAt.AddSeconds(1);
                if (isNewSubmissionAfterClaim && claim.Status != "Đã nhả")
                {
                    queueItem.Bucket = "Cần xử lý";
                    queueItem.CanClaim = true;
                    queueItem.DueAt = null;
                    queueItem.AssignedStaffAppUserID = null;
                    queueItem.AssignedStaffName = null;
                    queueItem.IsActionable = true;
                    queueItem.IsOverdue = false;
                    queueItem.IssueReason = null;
                    continue;
                }

                if (claim.Status == "Đã nhả")
                {
                    if (queueItem.Bucket != "Quá hạn / lỗi") queueItem.Bucket = "Cần xử lý";
                    queueItem.CanClaim = true;
                    continue;
                }

                queueItem.AssignedStaffAppUserID = claim.AssignedStaffAppUserID;
                queueItem.AssignedStaffName = staffNames.TryGetValue(claim.AssignedStaffAppUserID, out var staffName)
                    ? staffName
                    : $"Nhân viên #{claim.AssignedStaffAppUserID}";

                if (claim.Status == "Đang xử lý")
                {
                    if (queueItem.Bucket == "Quá hạn / lỗi")
                    {
                        queueItem.CanClaim = claim.AssignedStaffAppUserID == UserId();
                    }
                    else if (claim.DueAt.HasValue && claim.DueAt.Value < now)
                    {
                        queueItem.Bucket = "Quá hạn / lỗi";
                        queueItem.Status = "Quá hạn xử lý";
                        queueItem.IsOverdue = true;
                        queueItem.IssueReason = $"Đã quá hạn xử lý từ {claim.DueAt.Value:dd/MM/yyyy HH:mm}. Người phụ trách cần xử lý hoặc trả lại hàng đợi.";
                        queueItem.CanClaim = claim.AssignedStaffAppUserID == UserId();
                    }
                    else
                    {
                        queueItem.Bucket = "Đang xử lý";
                        queueItem.CanClaim = claim.AssignedStaffAppUserID == UserId();
                    }
                }
                else if (claim.Status == "Đã hoàn tất")
                {
                    queueItem.Bucket = "Quá hạn / lỗi";
                    queueItem.Status = "Lỗi đồng bộ";
                    queueItem.IsActionable = false;
                    queueItem.CanClaim = false;
                    queueItem.IssueReason = "Công việc đã được đánh dấu hoàn tất nhưng dữ liệu nghiệp vụ vẫn còn ở trạng thái cần xử lý. Cần quản trị kiểm tra đồng bộ trước khi thao tác tiếp.";
                }
                else
                {
                    queueItem.Bucket = "Quá hạn / lỗi";
                    queueItem.Status = claim.Status;
                    queueItem.IsActionable = false;
                    queueItem.CanClaim = false;
                    queueItem.IssueReason = $"Trạng thái công việc '{claim.Status}' không phù hợp với dữ liệu nghiệp vụ đang chờ.";
                }
            }

            var historyItems = claims
                .Where(x => !matchedClaimIds.Contains(x.WorkItemClaimID) && x.Status != "Đã nhả")
                .Take(120)
                .Select(claim =>
                {
                    var item = Queue(
                        claim.QueueType, claim.EntityID, null,
                        $"{claim.QueueType} #{claim.EntityID}",
                        claim.Status == "Đã hoàn tất"
                            ? "Công việc đã được xử lý và không còn nằm trong hàng đợi hoạt động."
                            : "Đối tượng nguồn không còn ở trạng thái cần xử lý.",
                        claim.Status, claim.AssignedAt, "Thấp", string.Empty);
                    item.WorkItemClaimID = claim.WorkItemClaimID;
                    item.AssignedStaffAppUserID = claim.AssignedStaffAppUserID;
                    item.AssignedStaffName = staffNames.TryGetValue(claim.AssignedStaffAppUserID, out var staffName)
                        ? staffName
                        : $"Nhân viên #{claim.AssignedStaffAppUserID}";
                    item.DueAt = claim.DueAt;
                    item.IsActionable = false;
                    item.CanClaim = false;

                    if (claim.Status == "Đã hoàn tất")
                    {
                        item.Bucket = "Đã hoàn tất";
                    }
                    else
                    {
                        item.Bucket = "Quá hạn / lỗi";
                        item.Status = "Dữ liệu không còn hiệu lực";
                        item.IssueReason = "Công việc vẫn mang trạng thái đang xử lý nhưng đối tượng nguồn đã hoàn tất, bị hủy hoặc không còn tồn tại trong hàng đợi nghiệp vụ.";
                    }
                    return item;
                })
                .ToList();

            var allItems = sourceItems.Concat(historyItems).ToList();
            var activeBuckets = new[] { "Cần xử lý", "Đang xử lý" };
            var activeItems = allItems.Where(x => activeBuckets.Contains(x.Bucket)).ToList();

            return Ok(new StaffWorkQueueDto
            {
                CurrentStaffAppUserID = UserId(),
                NeedActionCount = allItems.Count(x => x.Bucket == "Cần xử lý"),
                InProgressCount = allItems.Count(x => x.Bucket == "Đang xử lý"),
                ErrorCount = allItems.Count(x => x.Bucket == "Quá hạn / lỗi"),
                CompletedCount = allItems.Count(x => x.Bucket == "Đã hoàn tất"),
                PendingCustomerVerifications = activeItems.Count(x => x.QueueType == "Xác minh khách"),
                PendingVehicleApplications = activeItems.Count(x => x.QueueType == "Hồ sơ đối tác" || x.QueueType == "Hồ sơ xe"),
                PendingVehicleDocuments = activeItems.Count(x => x.QueueType == "Giấy tờ xe"),
                PendingPayments = activeItems.Count(x => x.QueueType == "Thanh toán"),
                OpenIncidents = activeItems.Count(x => x.QueueType == "Sự cố"),
                OpenDisputes = activeItems.Count(x => x.QueueType == "Tranh chấp"),
                OverdueTrafficFines = activeItems.Count(x => x.QueueType == "Phạt nguội"),
                PendingSettlements = activeItems.Count(x => x.QueueType == "Đối soát"),
                Items = allItems
                    .OrderBy(x => BucketOrder(x.Bucket))
                    .ThenBy(x => PriorityOrder(x.Priority))
                    .ThenByDescending(x => x.WaitingMinutes)
                    .Take(200)
                    .ToList()
            });
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet("staff/vehicle-documents/{id:int}")]
        public async Task<IActionResult> GetVehicleDocumentForReview(int id)
        {
            var item = await _context.VehicleDocuments.AsNoTracking()
                .Include(x => x.PartnerVehicle).ThenInclude(x => x.Car).ThenInclude(x => x.Brand)
                .Include(x => x.PartnerVehicle).ThenInclude(x => x.VehiclePartnerApplication)
                .Include(x => x.PartnerVehicle).ThenInclude(x => x.OwnerAppUser)
                .FirstOrDefaultAsync(x => x.VehicleDocumentID == id);
            if (item is null) return NotFound("Không tìm thấy giấy tờ xe.");
            return Ok(new StaffVehicleDocumentReviewDto
            {
                VehicleDocumentID = item.VehicleDocumentID,
                PartnerVehicleID = item.PartnerVehicleID,
                CarName = $"{item.PartnerVehicle.Car.Brand?.Name} {item.PartnerVehicle.Car.Model}".Trim(),
                LicensePlate = item.PartnerVehicle.VehiclePartnerApplication.LicensePlate,
                OwnerName = $"{item.PartnerVehicle.OwnerAppUser.Surname} {item.PartnerVehicle.OwnerAppUser.Name}".Trim(),
                DocumentType = item.DocumentType,
                DocumentNumber = item.DocumentNumber,
                FileUrl = item.FileUrl,
                IssuedDate = item.IssuedDate,
                ExpiryDate = item.ExpiryDate,
                Status = item.Status,
                RejectionReason = item.RejectionReason,
                CreatedDate = item.CreatedDate
            });
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet("staff/verifications/{id:int}")]
        public async Task<IActionResult> GetVerificationForReview(int id)
        {
            var item = await _context.UserVerifications.AsNoTracking().Include(x => x.AppUser)
                .FirstOrDefaultAsync(x => x.UserVerificationID == id);
            if (item is null) return NotFound("Không tìm thấy hồ sơ xác minh.");
            return Ok(new StaffVerificationReviewDto
            {
                UserVerificationID = item.UserVerificationID,
                FullName = ($"{item.AppUser.Surname} {item.AppUser.Name}").Trim(),
                Email = item.AppUser.Email,
                Phone = item.AppUser.Phone ?? string.Empty,
                LegalFullName = item.LegalFullName ?? string.Empty,
                Gender = item.Gender ?? string.Empty,
                CitizenIdAddress = item.CitizenIdAddress ?? string.Empty,
                PermanentProvince = item.PermanentProvince ?? string.Empty,
                PermanentWard = item.PermanentWard ?? string.Empty,
                PermanentDetail = item.PermanentDetail ?? string.Empty,
                PermanentAddress = item.PermanentAddress ?? string.Empty,
                CurrentAddressSameAsPermanent = item.CurrentAddressSameAsPermanent,
                CurrentProvince = item.CurrentProvince ?? string.Empty,
                CurrentWard = item.CurrentWard ?? string.Empty,
                CurrentDetail = item.CurrentDetail ?? string.Empty,
                CurrentAddress = item.CurrentAddress ?? string.Empty,
                DriverLicenseNumber = item.DriverLicenseNumber ?? string.Empty,
                DriverLicenseClass = item.DriverLicenseClass ?? string.Empty,
                CitizenIdMasked = item.CitizenIdMasked ?? string.Empty,
                CitizenIdIssuedDate = item.CitizenIdIssuedDate,
                CitizenIdExpiryDate = item.CitizenIdExpiryDate,
                CitizenIdFrontUrl = SecureViewUrl(item.CitizenIdFrontFileID, item.CitizenIdFrontUrl),
                CitizenIdBackUrl = SecureViewUrl(item.CitizenIdBackFileID, item.CitizenIdBackUrl),
                DriverLicenseUrl = SecureViewUrl(item.DriverLicenseFileID, item.DriverLicenseUrl),
                PortraitUrl = SecureViewUrl(item.PortraitFileID, item.PortraitUrl),
                DateOfBirth = item.DateOfBirth,
                DriverLicenseIssuedDate = item.DriverLicenseIssuedDate,
                DriverLicenseExpiry = item.DriverLicenseExpiry,
                Status = item.Status,
                RejectionReason = item.RejectionReason,
                CreatedDate = item.CreatedDate
            });
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost("staff/work-queue/claim")]
        public async Task<IActionResult> ClaimWorkItem(ClaimWorkItemDto dto)
        {
            var queueType = dto.QueueType?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(queueType) || dto.EntityID <= 0)
                return BadRequest("Công việc không hợp lệ.");

            var staffUserId = UserId();
            if (staffUserId <= 0)
                return Unauthorized("Không xác định được tài khoản nhân viên.");

            var cancellationToken = HttpContext.RequestAborted;
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                // ExecutionStrategy có thể chạy lại delegate sau lỗi tạm thời.
                // Xóa entity đang được theo dõi để lần chạy lại luôn đọc dữ liệu mới từ CSDL.
                _context.ChangeTracker.Clear();

                await using var transaction = await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable,
                    cancellationToken);

                try
                {
                    // Kiểm tra hiệu lực trong cùng transaction để tránh đối tượng bị xử lý
                    // giữa lúc kiểm tra và lúc nhân viên nhận việc.
                    var stillActive = queueType switch
                    {
                        "Xác minh khách" => await _context.UserVerifications.AnyAsync(
                            x => x.UserVerificationID == dto.EntityID && x.Status == "Chờ duyệt",
                            cancellationToken),
                        "Hồ sơ đối tác" => await _context.VehiclePartnerProfiles.AnyAsync(
                            x => x.VehiclePartnerProfileID == dto.EntityID && x.Status == "Chờ duyệt",
                            cancellationToken),
                        "Hồ sơ xe" => await _context.VehiclePartnerApplications.AnyAsync(
                            x => x.VehiclePartnerApplicationID == dto.EntityID && x.Status == "Chờ duyệt",
                            cancellationToken),
                        "Giấy tờ xe" => await _context.VehicleDocuments.AnyAsync(
                            x => x.VehicleDocumentID == dto.EntityID && x.Status == "Chờ duyệt",
                            cancellationToken),
                        "Thanh toán" => await _context.Payments.AnyAsync(
                            x => x.PaymentID == dto.EntityID &&
                                 x.Status == "Chờ xác nhận" &&
                                 x.Reservation.Status != "Đã hủy" &&
                                 x.Reservation.Status != "Hoàn thành",
                            cancellationToken),
                        "Sự cố" => await _context.Incidents.AnyAsync(
                            x => x.IncidentID == dto.EntityID &&
                                 x.Status != "Đã xử lý" &&
                                 x.Status != "Đã giải quyết" &&
                                 x.Status != "Đã đóng" &&
                                 x.Status != "Đã hủy",
                            cancellationToken),
                        "Tranh chấp" => await _context.Disputes.AnyAsync(
                            x => x.DisputeID == dto.EntityID &&
                                 x.Status != "Đã giải quyết" &&
                                 x.Status != "Đã đóng" &&
                                 x.Status != "Đã hủy",
                            cancellationToken),
                        "Phạt nguội" => await _context.TrafficFines.AnyAsync(
                            x => x.TrafficFineID == dto.EntityID &&
                                 x.Status != "Đã thanh toán" &&
                                 x.Status != "Đã hủy",
                            cancellationToken),
                        "Đối soát" => await _context.Reservations.AnyAsync(
                            x => x.ReservationID == dto.EntityID &&
                                 x.Status == "Chờ đối soát" &&
                                 !_context.Settlements.Any(s => s.ReservationID == x.ReservationID),
                            cancellationToken),
                        _ => false
                    };

                    if (!stillActive)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return Conflict("Công việc không còn hiệu lực hoặc đã được xử lý. Vui lòng tải lại hàng đợi.");
                    }

                    var existing = await _context.WorkItemClaims
                        .Where(x => x.QueueType == queueType && x.EntityID == dto.EntityID)
                        .OrderByDescending(x => x.WorkItemClaimID)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (existing is not null &&
                        existing.Status == "Đang xử lý" &&
                        existing.AssignedStaffAppUserID != staffUserId)
                    {
                        // Hồ sơ khách có thể đã được gửi lại sau lần nhận xử lý cũ.
                        // Khi đó claim cũ không được phép khóa phiên bản hồ sơ mới.
                        var isResubmittedVerification = queueType == "Xác minh khách" &&
                            await _context.UserVerifications.AnyAsync(
                                x => x.UserVerificationID == dto.EntityID &&
                                     x.CreatedDate > existing.AssignedAt.AddSeconds(1),
                                cancellationToken);

                        if (!isResubmittedVerification)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            return Conflict($"Công việc đang được xử lý bởi nhân viên #{existing.AssignedStaffAppUserID}.");
                        }
                    }

                    if (existing is null)
                    {
                        existing = new WorkItemClaim
                        {
                            QueueType = queueType,
                            EntityID = dto.EntityID
                        };
                        _context.WorkItemClaims.Add(existing);
                    }

                    var now = DateTime.UtcNow;
                    existing.AssignedStaffAppUserID = staffUserId;
                    existing.AssignedAt = now;
                    existing.DueAt = now.AddHours(dto.DueInHours is > 0 and <= 72 ? dto.DueInHours : 4);
                    existing.Status = "Đang xử lý";

                    _context.AuditLogs.Add(new AuditLog
                    {
                        AppUserID = staffUserId,
                        Action = "Nhận xử lý công việc",
                        EntityName = queueType,
                        EntityID = dto.EntityID.ToString(),
                        Note = dto.Note,
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                    });

                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return Ok(new
                    {
                        existing.WorkItemClaimID,
                        Message = "Đã nhận xử lý. Nhân viên khác sẽ thấy người phụ trách và hạn xử lý."
                    });
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("staff/work-queue/{id:int}/release")]
        public async Task<IActionResult> ReleaseWorkItem(int id)
        {
            var claim = await _context.WorkItemClaims.FirstOrDefaultAsync(x => x.WorkItemClaimID == id);
            if (claim is null) return NotFound();
            if (!User.IsInRole("Admin") && claim.AssignedStaffAppUserID != UserId()) return Forbid();
            claim.Status = "Đã nhả";
            await _context.SaveChangesAsync();
            return Ok("Đã trả công việc về hàng đợi.");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/risk-center")]
        public async Task<IActionResult> GetRiskCenter()
        {
            var fraud = await _context.FraudFlags.AsNoTracking().OrderByDescending(x => x.RiskScore).ThenByDescending(x => x.CreatedDate).Take(50).ToListAsync();
            var logs = await _context.AuditLogs.AsNoTracking().OrderByDescending(x => x.CreatedDate).Take(80).ToListAsync();
            var actorIds = logs.Where(x => x.AppUserID.HasValue).Select(x => x.AppUserID!.Value).Distinct().ToList();
            var actors = await _context.AppUsers.IgnoreQueryFilters().AsNoTracking().Where(x => actorIds.Contains(x.AppUserId)).ToDictionaryAsync(x => x.AppUserId, x => $"{x.Surname} {x.Name}".Trim());
            var cars = await _context.Cars.IgnoreQueryFilters().AsNoTracking().Where(x => x.IsDeleted).OrderByDescending(x => x.DeletedAt).Take(30).ToListAsync();
            var users = await _context.AppUsers.IgnoreQueryFilters().AsNoTracking().Where(x => x.IsDeleted).OrderByDescending(x => x.DeletedAt).Take(30).ToListAsync();
            var vietnamTodayStartUtc = VietnamTime.LocalToUtc(VietnamTime.Today);
            var vietnamTomorrowStartUtc = vietnamTodayStartUtc.AddDays(1);
            return Ok(new AdminRiskCenterDto
            {
                NewFraudFlags = await _context.FraudFlags.CountAsync(x => x.Status == "Mới"),
                OpenDisputes = await _context.Disputes.CountAsync(x => x.Status != "Đã giải quyết"),
                DeletedCars = await _context.Cars.IgnoreQueryFilters().CountAsync(x => x.IsDeleted),
                DeletedUsers = await _context.AppUsers.IgnoreQueryFilters().CountAsync(x => x.IsDeleted),
                AuditEventsToday = await _context.AuditLogs.CountAsync(x => x.CreatedDate >= vietnamTodayStartUtc && x.CreatedDate < vietnamTomorrowStartUtc),
                FraudFlags = fraud.Select(x => new AdminFraudFlagDto { FraudFlagID = x.FraudFlagID, AppUserID = x.AppUserID, ReservationID = x.ReservationID, RuleCode = x.RuleCode, Description = x.Description, RiskScore = x.RiskScore, Severity = x.RiskScore >= 80 ? "Khẩn cấp" : x.RiskScore >= 60 ? "Cao" : x.RiskScore >= 30 ? "Trung bình" : "Thấp", Status = x.Status, CreatedDate = x.CreatedDate }).ToList(),
                AuditLogs = logs.Select(x => new AdminAuditLogDto { AuditLogID = x.AuditLogID, AppUserID = x.AppUserID, ActorName = x.AppUserID.HasValue && actors.TryGetValue(x.AppUserID.Value, out var name) ? name : "Hệ thống", Action = x.Action, EntityName = x.EntityName, EntityID = x.EntityID, Note = x.Note, IpAddress = x.IpAddress, CreatedDate = x.CreatedDate }).ToList(),
                TrashItems = cars.Select(x => new AdminTrashItemDto { EntityType = "Xe", EntityID = x.CarID, DisplayName = x.Model, DeletedAt = x.DeletedAt, DeleteReason = x.DeleteReason, CanRestore = true })
                    .Concat(users.Select(x => new AdminTrashItemDto { EntityType = "Tài khoản", EntityID = x.AppUserId, DisplayName = string.IsNullOrWhiteSpace($"{x.Surname} {x.Name}".Trim()) ? x.Username : $"{x.Surname} {x.Name}".Trim(), DeletedAt = x.DeletedAt, DeleteReason = x.DeleteReason, CanRestore = x.AnonymizedAt == null })).OrderByDescending(x => x.DeletedAt).ToList()
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("admin/risk-center/fraud/{id:int}")]
        public async Task<IActionResult> ReviewFraudFlag(int id, UpdateFraudFlagDto dto)
        {
            var allowed = new[] { "Đang xem xét", "Đã xác nhận", "Bỏ qua", "Đã khóa liên quan" };
            var status = dto.Status?.Trim() ?? string.Empty;
            if (!allowed.Contains(status)) return BadRequest("Trạng thái xử lý cảnh báo không hợp lệ.");
            if (string.IsNullOrWhiteSpace(dto.Reason)) return BadRequest("Phải nhập lý do kết luận cảnh báo.");
            var item = await _context.FraudFlags.FirstOrDefaultAsync(x => x.FraudFlagID == id);
            if (item is null) return NotFound("Không tìm thấy cảnh báo.");
            item.Status = status;
            item.ReviewedByAppUserID = UserId();
            item.ReviewedDate = DateTime.UtcNow;
            _context.AuditLogs.Add(new AuditLog
            {
                AppUserID = UserId(), Action = "Xử lý cảnh báo gian lận", EntityName = nameof(FraudFlag),
                EntityID = id.ToString(), Note = $"{status}: {(dto.Reason ?? string.Empty).Trim()}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync();
            return Ok("Đã lưu kết luận cảnh báo và ghi audit log.");
        }

        private StaffQueueItemDto Queue(string type, int entityId, int? reservationId, string title, string description, string status, DateTime created, string priority, string url) => new()
        {
            QueueType = type, EntityID = entityId, ReservationID = reservationId, Title = title, Description = description, Status = status,
            Priority = priority, CreatedDate = created, WaitingMinutes = Math.Max(0, (int)(DateTime.UtcNow - created).TotalMinutes), ActionUrl = url, CanClaim = true
        };

        private static int BucketOrder(string bucket) => bucket switch
        {
            "Cần xử lý" => 0,
            "Đang xử lý" => 1,
            "Quá hạn / lỗi" => 2,
            "Đã hoàn tất" => 3,
            _ => 4
        };

        private static int PriorityOrder(string priority) => priority switch { "Khẩn cấp" => 0, "Cao" => 1, "Trung bình" => 2, _ => 3 };

        private static string GetHoldPaymentType(Reservation reservation)
        {
            if (reservation.ReservationDepositAmount > 0) return PaymentTypes.ReservationDeposit;
            if (reservation.ReservationDepositAmount <= 0 && reservation.SecurityDepositAmount <= 0 && reservation.DepositAmount > 0)
                return PaymentTypes.LegacyDeposit;
            return PaymentTypes.Rental;
        }

        private static ReservationPriceBreakdownDto BuildPrice(decimal dailyPrice, int days, decimal rentalAmount, decimal deposit, decimal deliveryFee = 0, decimal insuranceFee = 0, decimal customerPlatformFee = 0, decimal discountAmount = 0, decimal reservationDeposit = 0, decimal securityDeposit = 0)
        {
            var totalRental = Math.Max(0, rentalAmount + deliveryFee + insuranceFee + customerPlatformFee - discountAmount);
            var normalizedReservationDeposit = Math.Max(0, reservationDeposit);
            var normalizedSecurityDeposit = Math.Max(0, securityDeposit);
            if (normalizedReservationDeposit == 0 && normalizedSecurityDeposit == 0 && deposit > 0)
                normalizedSecurityDeposit = deposit; // Dữ liệu cũ trước v31.0.
            return new ReservationPriceBreakdownDto
            {
                DailyPrice = dailyPrice,
                RentalDays = days,
                RentalAmount = rentalAmount,
                DeliveryFee = deliveryFee,
                InsuranceFee = insuranceFee,
                CustomerPlatformFee = customerPlatformFee,
                DiscountAmount = discountAmount,
                TotalRental = totalRental,
                DepositAmount = normalizedReservationDeposit + normalizedSecurityDeposit,
                ReservationDepositAmount = normalizedReservationDeposit,
                SecurityDepositAmount = normalizedSecurityDeposit,
                TotalDue = totalRental + normalizedSecurityDeposit,
                RefundableExplanation = "Cọc giữ chỗ được cấn trừ vào tiền thuê; cọc bảo đảm không phải doanh thu và được hoàn sau khi trả xe, trừ nghĩa vụ được duyệt."
            };
        }

        private static List<ReservationTimelineItemDto> BuildTimeline(Reservation reservation, List<ReservationStatusHistory> histories)
        {
            var steps = new[]
            {
                ("request", "Đã gửi yêu cầu"), ("accepted", "Chủ xe chấp nhận"), ("deposit", "Đã thanh toán giữ chỗ"),
                ("handover", "Chờ giao xe"), ("renting", "Đang thuê"), ("return", "Trả xe và đối soát"), ("done", "Hoàn thành")
            };
            var current = reservation.Status switch
            {
                "Chờ chủ xe xác nhận" => 0,
                ReservationStatuses.PaymentPending or "Chờ khách đặt cọc" or "Chờ khách thanh toán giữ chỗ" => 1,
                "Chờ nhân viên xác nhận cọc" or "Chờ nhân viên xác nhận thanh toán" => 2,
                "Đã xác nhận" or "Đã thanh toán" or "Đã đặt cọc" => 2,
                "Chờ giao xe" => 3,
                "Đang thuê" => 4,
                "Chờ trả xe" or "Chờ đối soát" or "Đang tranh chấp" or "Đang xử lý sự cố" => 5,
                "Hoàn thành" => 6,
                _ => 0
            };
            var result = new List<ReservationTimelineItemDto>();
            for (var i = 0; i < steps.Length; i++)
            {
                var history = histories.LastOrDefault(h => StageIndex(h.NewStatus) == i);
                result.Add(new ReservationTimelineItemDto
                {
                    Status = steps[i].Item1,
                    Label = steps[i].Item2,
                    Note = history?.Note,
                    ChangedDate = history?.ChangedDate ?? (i == 0 ? reservation.CreatedDate : null),
                    State = reservation.Status == "Đã hủy" ? (i == 0 ? "completed" : "cancelled") : i < current ? "completed" : i == current ? "current" : "upcoming"
                });
            }
            return result;
        }

        private static int StageIndex(string status) => status switch
        {
            "Chờ chủ xe xác nhận" => 0,
            ReservationStatuses.PaymentPending or "Chờ khách đặt cọc" or "Chờ khách thanh toán giữ chỗ" => 1,
            "Chờ nhân viên xác nhận cọc" or "Chờ nhân viên xác nhận thanh toán" => 2,
            "Đã xác nhận" or "Đã thanh toán" or "Đã đặt cọc" => 2,
            "Chờ giao xe" => 3,
            "Đang thuê" => 4,
            "Chờ trả xe" or "Chờ đối soát" or "Đang tranh chấp" or "Đang xử lý sự cố" => 5,
            "Hoàn thành" => 6,
            _ => 0
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
            OwnerName = $"{x.PartnerVehicle.OwnerAppUser.Surname} {x.PartnerVehicle.OwnerAppUser.Name}".Trim(),
            OwnerPhone = x.PartnerVehicle.VehiclePartnerApplication?.Phone ?? string.Empty,
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

        private bool CanAccess(Reservation reservation) => User.IsInRole("Admin") || User.IsInRole("Staff") || reservation.CustomerAppUserID == UserId() || reservation.PartnerVehicle.OwnerAppUserID == UserId();
        private string IdentityKey()
            => _configuration["Security:IdentityHmacKey"]
               ?? throw new InvalidOperationException("Thiếu Security:IdentityHmacKey.");

        private static string FullAdministrativeName(string type, string name)
            => $"{(type ?? string.Empty).Trim()} {(name ?? string.Empty).Trim()}".Trim();

        private static string JoinAddress(params string?[] parts)
            => string.Join(", ", parts
                .Select(x => (x ?? string.Empty).Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x)));

        private static string MaskCitizenId(string number)
            => number.Length < 4 ? "****" : new string('*', number.Length - 4) + number[^4..];

        private static string SecureViewUrl(Guid? fileId, string? legacyUrl)
            => fileId.HasValue ? $"/SecureFiles/View/{fileId.Value}" : legacyUrl ?? string.Empty;

        private int UserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        private bool IsVehiclePartnerAccount() => string.Equals(User.FindFirstValue("IsVehiclePartner"), "true", StringComparison.OrdinalIgnoreCase);
    }

    public record ClaimWorkItemDto(string QueueType, int EntityID, int DueInHours, string? Note);

}
