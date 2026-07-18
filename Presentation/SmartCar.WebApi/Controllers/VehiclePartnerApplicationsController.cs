using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.Entities;
using SmartCar.Dto.MarketplaceDtos;
using SmartCar.Persistence.Context;
using SmartCar.WebApi.Services;
using System.Security.Claims;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehiclePartnerApplicationsController : ControllerBase
    {
        private static readonly string[] ReviewStatuses = { "Yêu cầu bổ sung", "Từ chối", "Đã duyệt" };
        private static readonly string[] AllowedRentalModes = { "Tự lái", "Có tài xế", "Tự lái hoặc có tài xế" };
        private static readonly string[] AllowedDeliveryMethods = { "Nhận tại điểm giao xe", "Giao xe tận nơi", "Nhận tại điểm hoặc giao tận nơi" };
        private static readonly string[] AllowedTransmissions = { "Số tự động", "Số sàn" };
        private static readonly string[] AllowedFuels = { "Xăng", "Dầu", "Điện", "Hybrid" };
        private static readonly string[] AllowedColors = { "Trắng", "Đen", "Bạc", "Xám", "Đỏ", "Xanh", "Vàng", "Nâu", "Cam" };
        private static readonly Dictionary<string, string[]> AllowedModelsByBrand = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Toyota"] = new[] { "Vios", "Camry", "Corolla Cross", "Fortuner", "Innova", "Raize", "Yaris Cross", "Veloz Cross" },
            ["Honda"] = new[] { "City", "Civic", "CR-V", "HR-V", "Accord", "BR-V" },
            ["Mazda"] = new[] { "Mazda 2", "Mazda 3", "Mazda 6", "CX-3", "CX-5", "CX-8" },
            ["Kia"] = new[] { "Morning", "Soluto", "K3", "K5", "Seltos", "Sonet", "Carnival" },
            ["Hyundai"] = new[] { "Grand i10", "Accent", "Elantra", "Creta", "Tucson", "Santa Fe", "Stargazer" },
            ["Mitsubishi"] = new[] { "Attrage", "Xpander", "Outlander", "Triton", "Pajero Sport" },
            ["Ford"] = new[] { "Ranger", "Everest", "Territory", "Transit", "EcoSport" },
            ["VinFast"] = new[] { "Fadil", "VF 3", "VF 5", "VF 6", "VF 7", "VF 8", "VF 9", "Lux A2.0", "Lux SA2.0" },
            ["Nissan"] = new[] { "Almera", "Navara", "Terra", "X-Trail" },
            ["Suzuki"] = new[] { "Swift", "Ertiga", "XL7", "Ciaz", "Carry" },
            ["Mercedes-Benz"] = new[] { "C-Class", "E-Class", "S-Class", "GLC", "GLE" },
            ["BMW"] = new[] { "3 Series", "5 Series", "7 Series", "X3", "X5" }
        };
        private readonly CarBookContext _context;
        private readonly IPrivateFileService _files;
        private readonly IWebHostEnvironment _environment;
        public VehiclePartnerApplicationsController(
            CarBookContext context,
            IPrivateFileService files,
            IWebHostEnvironment environment)
        {
            _context = context;
            _files = files;
            _environment = environment;
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateVehiclePartnerApplicationDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage)));
            }

            var userId = GetCurrentUserId();
            if (userId <= 0) return Unauthorized();
            if (!IsVehiclePartnerAccount()) return Forbid();

            var partnerProfile = await _context.VehiclePartnerProfiles.AsNoTracking()
                .FirstOrDefaultAsync(x => x.AppUserID == userId);
            if (partnerProfile is null) return BadRequest("Tài khoản chưa có hồ sơ đối tác. Vui lòng hoàn thiện hồ sơ xác minh trước khi đăng xe.");
            if (partnerProfile.Status != "Đã xác minh")
                return BadRequest($"Hồ sơ đối tác hiện đang ở trạng thái '{partnerProfile.Status}'. Chỉ đối tác đã xác minh mới được gửi xe lên duyệt.");

            var normalizedPlate = NormalizePlate(dto.LicensePlate);
            if (string.IsNullOrWhiteSpace(normalizedPlate)) return BadRequest("Biển số xe không hợp lệ.");

            var brandName = dto.BrandName?.Trim() ?? string.Empty;
            var modelName = dto.Model?.Trim() ?? string.Empty;
            if (!AllowedModelsByBrand.TryGetValue(brandName, out var allowedModels))
                return BadRequest("Hãng xe không hợp lệ. Vui lòng chọn hãng xe từ danh mục.");
            if (!allowedModels.Contains(modelName, StringComparer.OrdinalIgnoreCase))
                return BadRequest("Dòng xe không hợp lệ hoặc không thuộc hãng xe đã chọn.");

            var transmission = dto.Transmission?.Trim() ?? string.Empty;
            if (!AllowedTransmissions.Contains(transmission))
                return BadRequest("Hộp số không hợp lệ. Vui lòng chọn Số tự động hoặc Số sàn.");

            var fuel = dto.Fuel?.Trim() ?? string.Empty;
            if (!AllowedFuels.Contains(fuel))
                return BadRequest("Nhiên liệu không hợp lệ. Vui lòng chọn Xăng, Dầu, Điện hoặc Hybrid.");

            var color = dto.Color?.Trim() ?? string.Empty;
            if (!AllowedColors.Contains(color))
                return BadRequest("Màu xe không hợp lệ. Vui lòng chọn màu xe từ danh mục.");

            var rentalMode = dto.RentalMode?.Trim() ?? string.Empty;
            if (!AllowedRentalModes.Contains(rentalMode))
                return BadRequest("Hình thức thuê phải là: Tự lái, Có tài xế hoặc Tự lái hoặc có tài xế.");

            var requiresDriverProfile = rentalMode is "Có tài xế" or "Tự lái hoặc có tài xế";
            if (requiresDriverProfile)
            {
                if (string.IsNullOrWhiteSpace(dto.DriverFullName))
                    return BadRequest("Xe có tài xế phải khai họ tên tài xế.");
                if (string.IsNullOrWhiteSpace(dto.DriverPhone))
                    return BadRequest("Xe có tài xế phải khai số điện thoại tài xế.");
                if (string.IsNullOrWhiteSpace(dto.DriverCitizenIdentityNumber))
                    return BadRequest("Xe có tài xế phải khai số CCCD của tài xế.");
                if (string.IsNullOrWhiteSpace(dto.DriverLicenseNumber))
                    return BadRequest("Xe có tài xế phải khai số giấy phép lái xe của tài xế.");
                if (string.IsNullOrWhiteSpace(dto.DriverLicenseClass))
                    return BadRequest("Xe có tài xế phải khai hạng giấy phép lái xe của tài xế.");
                if (!dto.DriverLicenseExpiryDate.HasValue || dto.DriverLicenseExpiryDate.Value.Date <= SmartCar.Domain.Time.VietnamTime.Today)
                    return BadRequest("Giấy phép lái xe của tài xế phải còn hạn.");
                if (!dto.DriverLicenseFileId.HasValue || dto.DriverLicenseFileId == Guid.Empty)
                    return BadRequest("Xe có tài xế phải tải ảnh giấy phép lái xe của tài xế.");
            }

            var deliveryMethod = dto.DeliveryMethod?.Trim() ?? string.Empty;
            if (!AllowedDeliveryMethods.Contains(deliveryMethod))
                return BadRequest("Hình thức giao nhận không hợp lệ.");
            if (deliveryMethod == "Nhận tại điểm giao xe" && dto.DeliveryFee != 0)
                return BadRequest("Nếu khách nhận và trả xe tại điểm giao xe thì phí giao xe phải bằng 0 đồng.");
            if (deliveryMethod != "Nhận tại điểm giao xe" && string.IsNullOrWhiteSpace(dto.DeliveryAddress))
                return BadRequest("Vui lòng nhập địa chỉ hoặc khu vực hỗ trợ giao xe tận nơi.");

            if (string.IsNullOrWhiteSpace(dto.ChassisNumber)) return BadRequest("Vui lòng nhập số khung của xe.");
            if (string.IsNullOrWhiteSpace(dto.EngineNumber)) return BadRequest("Vui lòng nhập số máy của xe.");

            var duplicated = await _context.VehiclePartnerApplications.AnyAsync(x =>
                x.LicensePlate == normalizedPlate && x.Status != "Từ chối");
            if (duplicated || await _context.PartnerVehicles.AnyAsync(x => x.VehiclePartnerApplication.LicensePlate == normalizedPlate))
            {
                return Conflict("Biển số xe này đã có hồ sơ hoặc đã được đăng cho thuê trên hệ thống.");
            }

            if (!await _context.Locations.AnyAsync(x => x.LocationID == dto.LocationID))
            {
                return BadRequest("Địa điểm giao nhận xe không hợp lệ.");
            }

            var publicImageUrls = new[]
            {
                dto.VehicleImageUrl, dto.FrontImageUrl, dto.RearImageUrl, dto.LeftImageUrl,
                dto.RightImageUrl, dto.InteriorImageUrl, dto.DashboardImageUrl
            };
            var publicImageValidation = await ValidatePublicVehicleImagesAsync(publicImageUrls, userId, HttpContext.RequestAborted);
            if (!publicImageValidation.IsValid)
                return BadRequest(publicImageValidation.ErrorMessage);

            var privateDocuments = new List<PrivateFile>();
            try
            {
                privateDocuments.AddRange(await _files.ValidateForAttachmentAsync(
                    new[] { dto.RegistrationFileId }, userId, "VehicleRegistration", null, false, HttpContext.RequestAborted));
                privateDocuments.AddRange(await _files.ValidateForAttachmentAsync(
                    new[] { dto.InspectionFileId }, userId, "VehicleInspection", null, false, HttpContext.RequestAborted));
                privateDocuments.AddRange(await _files.ValidateForAttachmentAsync(
                    new[] { dto.InsuranceFileId }, userId, "VehicleInsurance", null, false, HttpContext.RequestAborted));
                if (requiresDriverProfile && dto.DriverLicenseFileId.HasValue)
                    privateDocuments.AddRange(await _files.ValidateForAttachmentAsync(
                        new[] { dto.DriverLicenseFileId.Value }, userId, "VehicleDriverLicense", null, false, HttpContext.RequestAborted));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            var entity = new VehiclePartnerApplication
            {
                AppUserID = userId,
                OwnerFullName = !string.IsNullOrWhiteSpace(partnerProfile.BusinessName) ? partnerProfile.BusinessName : partnerProfile.FullName,
                Email = partnerProfile.Email,
                Phone = partnerProfile.Phone,
                Address = !string.IsNullOrWhiteSpace(partnerProfile.HeadquartersAddress) ? partnerProfile.HeadquartersAddress : partnerProfile.CurrentAddress,
                CitizenIdentityNumber = partnerProfile.CitizenIdentityNumber,
                BankName = partnerProfile.BankName,
                BankAccountNumber = partnerProfile.BankAccountNumber,
                BankAccountHolder = partnerProfile.BankAccountHolder,
                BrandName = brandName,
                Model = modelName,
                VehicleVersion = string.IsNullOrWhiteSpace(dto.VehicleVersion) ? null : (dto.VehicleVersion ?? string.Empty).Trim(),
                ChassisNumber = (dto.ChassisNumber ?? string.Empty).Trim().ToUpperInvariant(),
                EngineNumber = (dto.EngineNumber ?? string.Empty).Trim().ToUpperInvariant(),
                ManufactureYear = dto.ManufactureYear,
                LicensePlate = normalizedPlate,
                Color = color,
                Transmission = transmission,
                Fuel = fuel,
                Seat = dto.Seat,
                Km = dto.Km,
                LocationID = dto.LocationID,
                ProposedDailyPrice = decimal.Round(dto.ProposedDailyPrice, 0),
                ProposedDepositAmount = rentalMode == "Có tài xế" ? 0 : decimal.Round(dto.ProposedDepositAmount, 0),
                RentalMode = rentalMode,
                DriverFullName = requiresDriverProfile ? dto.DriverFullName!.Trim() : null,
                DriverPhone = requiresDriverProfile ? dto.DriverPhone!.Trim() : null,
                DriverCitizenIdentityNumber = requiresDriverProfile ? dto.DriverCitizenIdentityNumber!.Trim() : null,
                DriverLicenseNumber = requiresDriverProfile ? dto.DriverLicenseNumber!.Trim() : null,
                DriverLicenseClass = requiresDriverProfile ? dto.DriverLicenseClass!.Trim() : null,
                DriverLicenseExpiryDate = requiresDriverProfile ? dto.DriverLicenseExpiryDate!.Value.Date : null,
                DriverLicenseImageUrl = requiresDriverProfile && dto.DriverLicenseFileId.HasValue ? _files.BuildViewUrl(dto.DriverLicenseFileId.Value) : null,
                DeliveryMethod = deliveryMethod,
                DeliveryAddress = string.IsNullOrWhiteSpace(dto.DeliveryAddress) ? null : (dto.DeliveryAddress ?? string.Empty).Trim(),
                KmLimitPerDay = dto.KmLimitPerDay,
                ExtraKmFee = decimal.Round(dto.ExtraKmFee, 0),
                DeliveryFee = deliveryMethod == "Nhận tại điểm giao xe" ? 0 : decimal.Round(dto.DeliveryFee, 0),
                Amenities = string.IsNullOrWhiteSpace(dto.Amenities) ? null : (dto.Amenities ?? string.Empty).Trim(),
                Accessories = string.IsNullOrWhiteSpace(dto.Accessories) ? null : (dto.Accessories ?? string.Empty).Trim(),
                RentalConditions = string.IsNullOrWhiteSpace(dto.RentalConditions) ? null : (dto.RentalConditions ?? string.Empty).Trim(),
                CancellationPolicy = string.IsNullOrWhiteSpace(dto.CancellationPolicy) ? null : (dto.CancellationPolicy ?? string.Empty).Trim(),
                VehicleImageUrl = (dto.VehicleImageUrl ?? string.Empty).Trim(),
                FrontImageUrl = (dto.FrontImageUrl ?? string.Empty).Trim(),
                RearImageUrl = (dto.RearImageUrl ?? string.Empty).Trim(),
                LeftImageUrl = (dto.LeftImageUrl ?? string.Empty).Trim(),
                RightImageUrl = (dto.RightImageUrl ?? string.Empty).Trim(),
                InteriorImageUrl = (dto.InteriorImageUrl ?? string.Empty).Trim(),
                DashboardImageUrl = (dto.DashboardImageUrl ?? string.Empty).Trim(),
                RegistrationImageUrl = _files.BuildViewUrl(dto.RegistrationFileId),
                InspectionImageUrl = _files.BuildViewUrl(dto.InspectionFileId),
                InsuranceImageUrl = _files.BuildViewUrl(dto.InsuranceFileId),
                Status = "Chờ duyệt",
                CreatedDate = DateTime.UtcNow
            };

            await using var transaction = await _context.Database.BeginTransactionAsync(HttpContext.RequestAborted);
            _context.VehiclePartnerApplications.Add(entity);
            await _context.SaveChangesAsync(HttpContext.RequestAborted);
            _files.MarkAttached(privateDocuments, nameof(VehiclePartnerApplication), entity.VehiclePartnerApplicationID.ToString());
            await _context.SaveChangesAsync(HttpContext.RequestAborted);
            await transaction.CommitAsync(HttpContext.RequestAborted);
            return Ok(new { entity.VehiclePartnerApplicationID, Message = "Đã gửi hồ sơ xe đối tác. Smart Car sẽ kiểm tra giấy tờ và phản hồi trong khu vực Xe đối tác của tôi." });
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpGet("me")]
        public async Task<IActionResult> GetMine()
        {
            if (!IsVehiclePartnerAccount()) return Forbid();
            var userId = GetCurrentUserId();
            var values = await _context.VehiclePartnerApplications.AsNoTracking()
                .Include(x => x.Location)
                .Where(x => x.AppUserID == userId)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();
            return Ok(values.Select(Map).ToList());
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult> GetAll(string? status)
        {
            var query = _context.VehiclePartnerApplications.AsNoTracking()
                .Include(x => x.Location)
                .Include(x => x.AppUser)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim());
            var values = await query.OrderByDescending(x => x.CreatedDate).ToListAsync();
            return Ok(values.Select(Map).ToList());
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("{id:int}/review")]
        public async Task<IActionResult> Review(int id, ReviewVehiclePartnerApplicationDto dto)
        {
            var status = dto.Status?.Trim() ?? string.Empty;
            if (!ReviewStatuses.Contains(status)) return BadRequest("Trạng thái duyệt hồ sơ xe không hợp lệ.");

            var application = await _context.VehiclePartnerApplications
                .Include(x => x.Location)
                .FirstOrDefaultAsync(x => x.VehiclePartnerApplicationID == id);
            if (application is null) return NotFound("Không tìm thấy hồ sơ xe đối tác.");
            if (application.Status == "Đã duyệt") return BadRequest("Hồ sơ này đã được duyệt trước đó.");

            application.Status = status;
            application.AdminNote = string.IsNullOrWhiteSpace(dto.AdminNote) ? null : (dto.AdminNote ?? string.Empty).Trim();
            application.ReviewedDate = DateTime.UtcNow;

            if (status != "Đã duyệt")
            {
                var rejectedClaim = await _context.WorkItemClaims.FirstOrDefaultAsync(x => x.QueueType == "Hồ sơ xe" && x.EntityID == id && x.Status == "Đang xử lý");
                if (rejectedClaim is not null) rejectedClaim.Status = "Đã hoàn tất";
                await _context.SaveChangesAsync();
                return Ok("Đã cập nhật kết quả duyệt hồ sơ xe đối tác.");
            }

            var approvedDailyPrice = dto.ApprovedDailyPrice ?? application.ProposedDailyPrice;
            var approvedDepositAmount = application.RentalMode == "Có tài xế"
                ? 0
                : dto.ApprovedDepositAmount ?? application.ProposedDepositAmount;
            if (approvedDailyPrice < 100000m) return BadRequest("Giá thuê theo ngày được duyệt phải từ 100.000 VNĐ.");
            if (approvedDepositAmount < 0) return BadRequest("Tiền đặt cọc không hợp lệ.");
            if (dto.CommissionRateOverride.HasValue && (dto.CommissionRateOverride.Value < 0 || dto.CommissionRateOverride.Value > 100)) return BadRequest("Mức chiết khấu riêng phải từ 0 đến 100%.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var brandName = application.BrandName.Trim();
                var brand = await _context.Brands.FirstOrDefaultAsync(x => x.Name.ToLower() == brandName.ToLower());
                if (brand is null)
                {
                    brand = new Brand { Name = brandName };
                    _context.Brands.Add(brand);
                    await _context.SaveChangesAsync();
                }

                var car = new Car
                {
                    BrandID = brand.BrandID,
                    Model = string.IsNullOrWhiteSpace(application.VehicleVersion) ? application.Model : $"{application.Model} {application.VehicleVersion}",
                    CoverImageUrl = application.VehicleImageUrl,
                    BigImageUrl = application.VehicleImageUrl,
                    Km = application.Km,
                    Transmission = application.Transmission,
                    Seat = application.Seat,
                    Luggage = 2,
                    Fuel = application.Fuel
                };
                _context.Cars.Add(car);
                await _context.SaveChangesAsync();

                await AddPricingAsync(car.CarID, "Theo giờ", Math.Max(50000m, approvedDailyPrice / 10m));
                await AddPricingAsync(car.CarID, "Theo ngày", approvedDailyPrice);
                await AddPricingAsync(car.CarID, "Theo tuần", approvedDailyPrice * 6m);
                await AddPricingAsync(car.CarID, "Theo tháng", approvedDailyPrice * 25m);

                _context.RentACars.Add(new RentACar
                {
                    CarID = car.CarID,
                    LocationID = application.LocationID,
                    Available = true
                });

                _context.PartnerVehicles.Add(new PartnerVehicle
                {
                    CarID = car.CarID,
                    OwnerAppUserID = application.AppUserID,
                    VehiclePartnerApplicationID = application.VehiclePartnerApplicationID,
                    CommissionRateOverride = dto.CommissionRateOverride,
                    DepositAmount = approvedDepositAmount,
                    IsActive = true,
                    ListedDate = DateTime.UtcNow
                });

                application.ApprovedCarID = car.CarID;
                var approvedClaim = await _context.WorkItemClaims.FirstOrDefaultAsync(x => x.QueueType == "Hồ sơ xe" && x.EntityID == id && x.Status == "Đang xử lý");
                if (approvedClaim is not null) approvedClaim.Status = "Đã hoàn tất";
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { Message = "Đã duyệt hồ sơ và đưa xe đối tác lên hệ thống.", CarID = car.CarID });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task AddPricingAsync(int carId, string pricingName, decimal amount)
        {
            var pricing = await _context.Pricings.FirstOrDefaultAsync(x => x.Name == pricingName);
            if (pricing is null)
            {
                pricing = new Pricing { Name = pricingName };
                _context.Pricings.Add(pricing);
                await _context.SaveChangesAsync();
            }
            _context.CarPricings.Add(new CarPricing
            {
                CarID = carId,
                PricingID = pricing.PricingID,
                Amount = decimal.Round(amount, 0)
            });
        }

        private bool IsVehiclePartnerAccount()
            => string.Equals(User.FindFirstValue("IsVehiclePartner"), "true", StringComparison.OrdinalIgnoreCase);

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        private async Task<(bool IsValid, string ErrorMessage)> ValidatePublicVehicleImagesAsync(
            IEnumerable<string?> values,
            int ownerId,
            CancellationToken cancellationToken)
        {
            var urls = values.Select(x => (x ?? string.Empty).Trim()).ToArray();
            if (urls.Any(string.IsNullOrWhiteSpace))
                return (false, "Vui lòng tải đủ ảnh hiển thị xe.");
            if (urls.Distinct(StringComparer.OrdinalIgnoreCase).Count() != urls.Length)
                return (false, "Mỗi vị trí ảnh xe phải dùng một tệp riêng.");

            var prefix = $"/uploads/vehicle-images/{ownerId}/";
            var webRoot = Path.GetFullPath(_environment.WebRootPath);
            foreach (var url in urls)
            {
                if (!url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || url.Contains('?') || url.Contains('#'))
                    return (false, "Ảnh xe không thuộc tài khoản đang đăng nhập.");
                var relative = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var physicalPath = Path.GetFullPath(Path.Combine(webRoot, relative));
                if (!physicalPath.StartsWith(webRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    return (false, "Đường dẫn ảnh xe không hợp lệ.");
                var extension = Path.GetExtension(physicalPath);
                if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension, StringComparer.OrdinalIgnoreCase))
                    return (false, "Ảnh xe phải là JPG, PNG hoặc WEBP.");
                if (!System.IO.File.Exists(physicalPath))
                    return (false, "Một hoặc nhiều ảnh xe không còn tồn tại trên máy chủ.");
            }

            var used = await _context.VehiclePartnerApplications.AsNoTracking().AnyAsync(x =>
                urls.Contains(x.VehicleImageUrl) || urls.Contains(x.FrontImageUrl) || urls.Contains(x.RearImageUrl) ||
                urls.Contains(x.LeftImageUrl) || urls.Contains(x.RightImageUrl) || urls.Contains(x.InteriorImageUrl) ||
                urls.Contains(x.DashboardImageUrl), cancellationToken);
            if (used) return (false, "Một hoặc nhiều ảnh đã được gắn vào hồ sơ xe khác.");
            return (true, string.Empty);
        }

        private static string NormalizePlate(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return new string(value.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        }

        private static ResultVehiclePartnerApplicationDto Map(VehiclePartnerApplication x) => new()
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
            DriverFullName = x.DriverFullName,
            DriverPhone = x.DriverPhone,
            DriverCitizenIdentityNumber = x.DriverCitizenIdentityNumber,
            DriverLicenseNumber = x.DriverLicenseNumber,
            DriverLicenseClass = x.DriverLicenseClass,
            DriverLicenseExpiryDate = x.DriverLicenseExpiryDate,
            DriverLicenseImageUrl = x.DriverLicenseImageUrl,
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
    }
}
