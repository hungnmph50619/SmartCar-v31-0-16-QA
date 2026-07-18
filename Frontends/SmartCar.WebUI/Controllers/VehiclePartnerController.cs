using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using SmartCar.Dto.AccountDtos;
using SmartCar.Dto.LocationDtos;
using SmartCar.Dto.MarketplaceDtos;
using SmartCar.Dto.RegisterDtos;
using SmartCar.WebUI.Models;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

using SmartCar.Dto.Security;
namespace SmartCar.WebUI.Controllers
{
    public class VehiclePartnerController : Controller
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
        private const long MaxFileSize = 6 * 1024 * 1024;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _environment;

        public VehiclePartnerController(IHttpClientFactory httpClientFactory, IWebHostEnvironment environment)
        {
            _httpClientFactory = httpClientFactory;
            _environment = environment;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult BecomeOwner() => View();

        [AllowAnonymous]
        [HttpGet]
        public IActionResult RegisterAccount()
        {
            if (IsVehiclePartnerAccount()) return RedirectToAction(nameof(Dashboard));
            return View(new CreateVehiclePartnerAccountDto());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterAccount(CreateVehiclePartnerAccountDto dto)
        {
            if (!ModelState.IsValid) return View(dto);
            try
            {
                var response = await _httpClientFactory.CreateClient().PostAsync(
                    "api/Registers/vehicle-partner", JsonContent(dto));
                var raw = await response.Content.ReadAsStringAsync();
                var message = Clean(raw);
                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(string.Empty, message);
                    return View(dto);
                }

                var result = JsonConvert.DeserializeObject<RegisterResultDto>(raw)
                             ?? new RegisterResultDto { Username = dto.Username, Email = dto.Email, Message = message };
                if (User.Identity?.IsAuthenticated == true)
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["RegistrationAttemptId"] = result.RegistrationAttemptId.ToString();
                TempData["RegisterUsername"] = string.IsNullOrWhiteSpace(result.Username) ? dto.Username : result.Username;
                TempData["RegisterEmail"] = result.Email;
                TempData["RegisterMessage"] = result.Message;
                TempData["RegisterEmailSent"] = result.EmailSent ? "true" : "false";
                return RedirectToAction("VerifyEmail", "Register", new { attemptId = result.RegistrationAttemptId, username = TempData["RegisterUsername"], email = result.Email });
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "Không kết nối được Web API tại cổng 7060.");
                return View(dto);
            }
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpGet]
        public async Task<IActionResult> VerifyProfile()
        {
            if (!IsVehiclePartnerAccount()) return RedirectToAction(nameof(BecomeOwner));
            var model = new VehiclePartnerProfileVerifyViewModel();
            await LoadPartnerProfileAsync(model);
            return View(model);
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpGet]
        public Task<IActionResult> Provinces(CancellationToken cancellationToken)
            => ProxyAdministrativeUnitsAsync("api/administrative-units/provinces", cancellationToken);

        [Authorize(Roles = "VehiclePartner")]
        [HttpGet]
        public Task<IActionResult> Wards(string provinceCode, CancellationToken cancellationToken)
        {
            var code = (provinceCode ?? string.Empty).Trim();
            if (code.Length != 2 || !code.All(char.IsDigit))
                return Task.FromResult<IActionResult>(BadRequest(new { message = "Mã tỉnh/thành phố không hợp lệ." }));
            return ProxyAdministrativeUnitsAsync(
                $"api/administrative-units/provinces/{Uri.EscapeDataString(code)}/wards",
                cancellationToken);
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyProfile(VehiclePartnerProfileVerifyViewModel model)
        {
            if (!IsVehiclePartnerAccount()) return RedirectToAction(nameof(BecomeOwner));
            model.CurrentProfile = await GetCurrentPartnerProfileAsync();
            var type = model.Form.PartnerType?.Trim() ?? CreateVehiclePartnerAccountDto.IndividualPartnerType;
            var savedFiles = new List<string>();

            ModelState.Remove("Form.CitizenFrontFileId");
            ModelState.Remove("Form.CitizenBackFileId");
            ModelState.Remove("Form.PortraitFileId");
            ModelState.Remove("Form.BusinessLicenseFileId");
            ModelState.Remove("Form.AuthorizationDocumentFileId");

            RemoveIrrelevantPartnerProfileModelStateErrors(type, model.Form.CurrentAddressSameAsPermanent);

            if (type == CreateVehiclePartnerAccountDto.IndividualPartnerType)
            {
                ValidateFile(model.CitizenFrontImage, nameof(model.CitizenFrontImage), "ảnh CCCD mặt trước");
                ValidateFile(model.CitizenBackImage, nameof(model.CitizenBackImage), "ảnh CCCD mặt sau");
                ValidateFile(model.PortraitImage, nameof(model.PortraitImage), "ảnh chân dung hiện tại");
            }
            else
            {
                ValidateFile(model.BusinessLicenseImage, nameof(model.BusinessLicenseImage), "giấy đăng ký doanh nghiệp hoặc tài liệu pháp lý tương đương");
                if (model.AuthorizationDocument is not null && model.AuthorizationDocument.Length > 0)
                    ValidateFile(model.AuthorizationDocument, nameof(model.AuthorizationDocument), "giấy ủy quyền");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ProfileSubmitFailed = "Hồ sơ chưa được gửi. Vui lòng kiểm tra các lỗi được liệt kê bên dưới.";
                return View(model);
            }

            PreparePartnerProfilePayloadBeforeSubmit(model.Form);

            try
            {
                if (type == CreateVehiclePartnerAccountDto.IndividualPartnerType)
                {
                    model.Form.CitizenFrontFileId = await SavePrivateFileAsync(model.CitizenFrontImage!, savedFiles, "PartnerCitizenFront");
                    model.Form.CitizenBackFileId = await SavePrivateFileAsync(model.CitizenBackImage!, savedFiles, "PartnerCitizenBack");
                    model.Form.PortraitFileId = await SavePrivateFileAsync(model.PortraitImage!, savedFiles, "PartnerPortrait");
                }
                else
                {
                    model.Form.BusinessLicenseFileId = await SavePrivateFileAsync(model.BusinessLicenseImage!, savedFiles, "PartnerBusinessLicense");
                    if (model.AuthorizationDocument is not null && model.AuthorizationDocument.Length > 0)
                        model.Form.AuthorizationDocumentFileId = await SavePrivateFileAsync(model.AuthorizationDocument, savedFiles, "PartnerAuthorization");
                }

                var response = await CreateAuthorizedClient().PostAsync("api/VehiclePartnerProfiles/me/submit", JsonContent(model.Form));
                var message = Clean(await response.Content.ReadAsStringAsync());
                if (!response.IsSuccessStatusCode)
                {
                    await DeleteSavedFilesAsync(savedFiles);
                    ModelState.AddModelError(string.Empty, string.IsNullOrWhiteSpace(message)
                        ? "Hồ sơ chưa được gửi. Vui lòng kiểm tra lại thông tin."
                        : $"Hồ sơ chưa được gửi. {message}");
                    ViewBag.ProfileSubmitFailed = "Hồ sơ chưa được gửi. Vui lòng kiểm tra các lỗi được liệt kê bên dưới.";
                    return View(model);
                }

                TempData["VehiclePartnerSuccess"] = string.IsNullOrWhiteSpace(message)
                    ? "Đã gửi hồ sơ xác minh. Hồ sơ của bạn đang ở trạng thái Chờ duyệt."
                    : message;
                return RedirectToAction(nameof(VerifyProfile));
            }
            catch (HttpRequestException)
            {
                await DeleteSavedFilesAsync(savedFiles);
                ModelState.AddModelError(string.Empty, "Không kết nối được Web API tại cổng 7060.");
                return View(model);
            }
            catch (IOException)
            {
                await DeleteSavedFilesAsync(savedFiles);
                ModelState.AddModelError(string.Empty, "Hồ sơ chưa được gửi. Không thể lưu ảnh hồ sơ. Vui lòng thử lại.");
                ViewBag.ProfileSubmitFailed = "Hồ sơ chưa được gửi. Không thể lưu ảnh hồ sơ.";
                return View(model);
            }
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (!IsVehiclePartnerAccount()) return RedirectToAction(nameof(BecomeOwner));
            try
            {
                var response = await CreateAuthorizedClient().GetAsync("api/PartnerVehicles/me/dashboard");
                if (!response.IsSuccessStatusCode)
                {
                    TempData["VehiclePartnerError"] = Clean(await response.Content.ReadAsStringAsync());
                    return View(new PartnerVehicleDashboardDto());
                }
                var model = JsonConvert.DeserializeObject<PartnerVehicleDashboardDto>(await response.Content.ReadAsStringAsync())
                            ?? new PartnerVehicleDashboardDto();
                return View(model);
            }
            catch (HttpRequestException)
            {
                TempData["VehiclePartnerError"] = "Không kết nối được Web API tại cổng 7060.";
                return View(new PartnerVehicleDashboardDto());
            }
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpGet]
        public async Task<IActionResult> Register()
        {
            if (!IsVehiclePartnerAccount()) return RedirectToAction(nameof(BecomeOwner));
            var profileResponse = await CreateAuthorizedClient().GetAsync("api/VehiclePartnerProfiles/me");
            if (!profileResponse.IsSuccessStatusCode)
            {
                TempData["VehiclePartnerError"] = "Bạn cần hoàn thiện hồ sơ đối tác trước khi đăng xe.";
                return RedirectToAction(nameof(VerifyProfile));
            }
            var currentProfile = JsonConvert.DeserializeObject<VehiclePartnerProfileDto>(await profileResponse.Content.ReadAsStringAsync());
            if (currentProfile?.Status != "Đã xác minh")
            {
                TempData["VehiclePartnerError"] = $"Hồ sơ đối tác hiện đang ở trạng thái '{currentProfile?.Status ?? "Chưa có"}'. Chỉ hồ sơ đã xác minh mới được gửi xe lên duyệt.";
                return RedirectToAction(nameof(Dashboard));
            }
            var model = new VehiclePartnerRegisterViewModel
            {
                Form = new CreateVehiclePartnerApplicationDto
                {
                    ManufactureYear = SmartCar.WebUI.Infrastructure.VietnamTime.Today.Year - 3,
                    Seat = 5,
                    Transmission = "Số tự động",
                    Fuel = "Xăng",
                    ProposedDailyPrice = 800000m,
                    ProposedDepositAmount = 10000000m,
                    RentalMode = "Tự lái",
                    DeliveryMethod = "Nhận tại điểm giao xe",
                    KmLimitPerDay = 300,
                    ExtraKmFee = 5000m,
                    RentalConditions = "Khách có GPLX hợp lệ, đã xác minh danh tính và tuân thủ quy định giao nhận.",
                    CancellationPolicy = "Hoàn theo chính sách SmartCar; phí có thể phát sinh nếu hủy sát giờ nhận xe."
                }
            };
            await LoadReferenceDataAsync(model, true);
            return View(model);
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(VehiclePartnerRegisterViewModel model)
        {
            if (!IsVehiclePartnerAccount()) return RedirectToAction(nameof(BecomeOwner));
            await LoadReferenceDataAsync(model, true);
            ModelState.Remove("Form.VehicleImageUrl");
            ModelState.Remove("Form.FrontImageUrl");
            ModelState.Remove("Form.RearImageUrl");
            ModelState.Remove("Form.LeftImageUrl");
            ModelState.Remove("Form.RightImageUrl");
            ModelState.Remove("Form.InteriorImageUrl");
            ModelState.Remove("Form.DashboardImageUrl");
            ModelState.Remove("Form.RegistrationFileId");
            ModelState.Remove("Form.InspectionFileId");
            ModelState.Remove("Form.InsuranceFileId");
            ModelState.Remove("Form.DriverLicenseFileId");

            var requiresDriverProfile = model.Form.RentalMode is "Có tài xế" or "Tự lái hoặc có tài xế";
            if (!requiresDriverProfile)
            {
                ModelState.Remove("Form.DriverFullName");
                ModelState.Remove("Form.DriverPhone");
                ModelState.Remove("Form.DriverCitizenIdentityNumber");
                ModelState.Remove("Form.DriverLicenseNumber");
                ModelState.Remove("Form.DriverLicenseClass");
                ModelState.Remove("Form.DriverLicenseExpiryDate");
            }

            ValidateImageFile(model.VehicleImage, nameof(model.VehicleImage), "ảnh tổng thể xe");
            ValidateImageFile(model.FrontImage, nameof(model.FrontImage), "ảnh đầu xe");
            ValidateImageFile(model.RearImage, nameof(model.RearImage), "ảnh đuôi xe");
            ValidateImageFile(model.LeftImage, nameof(model.LeftImage), "ảnh bên trái xe");
            ValidateImageFile(model.RightImage, nameof(model.RightImage), "ảnh bên phải xe");
            ValidateImageFile(model.InteriorImage, nameof(model.InteriorImage), "ảnh nội thất xe");
            ValidateImageFile(model.DashboardImage, nameof(model.DashboardImage), "ảnh đồng hồ kilomet");
            ValidateFile(model.RegistrationImage, nameof(model.RegistrationImage), "ảnh đăng ký xe");
            ValidateFile(model.InspectionImage, nameof(model.InspectionImage), "ảnh đăng kiểm");
            ValidateFile(model.InsuranceImage, nameof(model.InsuranceImage), "ảnh bảo hiểm xe");
            if (requiresDriverProfile)
            {
                ValidateFile(model.DriverLicenseImage, nameof(model.DriverLicenseImage), "ảnh giấy phép lái xe của tài xế");
                if (string.IsNullOrWhiteSpace(model.Form.DriverFullName))
                    ModelState.AddModelError("Form.DriverFullName", "Vui lòng nhập họ tên tài xế.");
                if (string.IsNullOrWhiteSpace(model.Form.DriverPhone))
                    ModelState.AddModelError("Form.DriverPhone", "Vui lòng nhập số điện thoại tài xế.");
                if (string.IsNullOrWhiteSpace(model.Form.DriverCitizenIdentityNumber))
                    ModelState.AddModelError("Form.DriverCitizenIdentityNumber", "Vui lòng nhập số CCCD tài xế.");
                if (string.IsNullOrWhiteSpace(model.Form.DriverLicenseNumber))
                    ModelState.AddModelError("Form.DriverLicenseNumber", "Vui lòng nhập số giấy phép lái xe của tài xế.");
                if (string.IsNullOrWhiteSpace(model.Form.DriverLicenseClass))
                    ModelState.AddModelError("Form.DriverLicenseClass", "Vui lòng nhập hạng giấy phép lái xe của tài xế.");
                if (!model.Form.DriverLicenseExpiryDate.HasValue || model.Form.DriverLicenseExpiryDate.Value.Date <= SmartCar.WebUI.Infrastructure.VietnamTime.Today)
                    ModelState.AddModelError("Form.DriverLicenseExpiryDate", "Giấy phép lái xe của tài xế phải còn hạn.");
            }

            if (model.Form.RentalMode == "Có tài xế")
                model.Form.ProposedDepositAmount = 0;
            if (model.Form.DeliveryMethod == "Nhận tại điểm giao xe")
                model.Form.DeliveryFee = 0;

            if (!ModelState.IsValid)
            {
                ViewBag.ProfileSubmitFailed = "Hồ sơ chưa được gửi. Vui lòng kiểm tra các lỗi được liệt kê bên dưới.";
                return View(model);
            }

            var savedFiles = new List<string>();
            try
            {
                model.Form.VehicleImageUrl = await SavePublicVehicleImageAsync(model.VehicleImage!, savedFiles);
                model.Form.FrontImageUrl = await SavePublicVehicleImageAsync(model.FrontImage!, savedFiles);
                model.Form.RearImageUrl = await SavePublicVehicleImageAsync(model.RearImage!, savedFiles);
                model.Form.LeftImageUrl = await SavePublicVehicleImageAsync(model.LeftImage!, savedFiles);
                model.Form.RightImageUrl = await SavePublicVehicleImageAsync(model.RightImage!, savedFiles);
                model.Form.InteriorImageUrl = await SavePublicVehicleImageAsync(model.InteriorImage!, savedFiles);
                model.Form.DashboardImageUrl = await SavePublicVehicleImageAsync(model.DashboardImage!, savedFiles);
                model.Form.RegistrationFileId = await SavePrivateFileAsync(model.RegistrationImage!, savedFiles, "VehicleRegistration");
                model.Form.InspectionFileId = await SavePrivateFileAsync(model.InspectionImage!, savedFiles, "VehicleInspection");
                model.Form.InsuranceFileId = await SavePrivateFileAsync(model.InsuranceImage!, savedFiles, "VehicleInsurance");
                if (requiresDriverProfile)
                    model.Form.DriverLicenseFileId = await SavePrivateFileAsync(model.DriverLicenseImage!, savedFiles, "VehicleDriverLicense");
                else
                    model.Form.DriverLicenseFileId = null;

                var response = await CreateAuthorizedClient().PostAsync(
                    "api/VehiclePartnerApplications", JsonContent(model.Form));
                if (!response.IsSuccessStatusCode)
                {
                    await DeleteSavedFilesAsync(savedFiles);
                    ModelState.AddModelError(string.Empty, Clean(await response.Content.ReadAsStringAsync()));
                    return View(model);
                }

                TempData["VehiclePartnerSuccess"] = "Đã gửi hồ sơ xe. Bạn có thể theo dõi kết quả tại khu vực đối tác.";
                return RedirectToAction(nameof(Dashboard));
            }
            catch (HttpRequestException)
            {
                await DeleteSavedFilesAsync(savedFiles);
                ModelState.AddModelError(string.Empty, "Không kết nối được Web API tại cổng 7060.");
                return View(model);
            }
            catch (IOException)
            {
                await DeleteSavedFilesAsync(savedFiles);
                ModelState.AddModelError(string.Empty, "Hồ sơ chưa được gửi. Không thể lưu ảnh hồ sơ. Vui lòng thử lại.");
                ViewBag.ProfileSubmitFailed = "Hồ sơ chưa được gửi. Không thể lưu ảnh hồ sơ.";
                return View(model);
            }
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpGet]
        public async Task<IActionResult> VehicleOperations(int id)
        {
            if (!IsVehiclePartnerAccount()) return RedirectToAction(nameof(BecomeOwner));
            try
            {
                var response = await CreateAuthorizedClient().GetAsync($"api/PartnerVehicles/me/{id}/operations");
                if (!response.IsSuccessStatusCode)
                {
                    TempData["VehiclePartnerError"] = Clean(await response.Content.ReadAsStringAsync());
                    return RedirectToAction(nameof(Dashboard));
                }
                var model = JsonConvert.DeserializeObject<PartnerVehicleOperationsDto>(await response.Content.ReadAsStringAsync());
                return model is null ? RedirectToAction(nameof(Dashboard)) : View(model);
            }
            catch (HttpRequestException)
            {
                TempData["VehiclePartnerError"] = "Không kết nối được Web API tại cổng 7060.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVehicleDocument(int id, string documentType, string documentNumber, DateTime? issuedDate, DateTime? expiryDate, IFormFile? documentFile)
        {
            if (!IsVehiclePartnerAccount()) return Forbid();
            if (documentFile is null || documentFile.Length == 0)
            {
                TempData["VehiclePartnerError"] = "Vui lòng tải ảnh hoặc tệp giấy tờ.";
                return RedirectToAction(nameof(VehicleOperations), new { id });
            }
            var extension = Path.GetExtension(documentFile.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension) || documentFile.Length > MaxFileSize)
            {
                TempData["VehiclePartnerError"] = "Giấy tờ chỉ chấp nhận JPG, PNG, WEBP hoặc PDF và tối đa 6 MB.";
                return RedirectToAction(nameof(VehicleOperations), new { id });
            }
            var savedFiles = new List<string>();
            try
            {
                var uploaded = await SavePrivateFileUploadAsync(documentFile, savedFiles, ResolveVehicleDocumentCategory(documentType));
                var response = await CreateAuthorizedClient().PostAsync($"api/comprehensive-operations/vehicles/{id}/documents", JsonContent(new
                {
                    DocumentType = documentType,
                    DocumentNumber = documentNumber,
                    FileId = uploaded.PrivateFileId,
                    IssuedDate = issuedDate,
                    ExpiryDate = expiryDate
                }));
                if (!response.IsSuccessStatusCode) await DeleteSavedFilesAsync(savedFiles);
                TempData[response.IsSuccessStatusCode ? "VehiclePartnerSuccess" : "VehiclePartnerError"] = response.IsSuccessStatusCode
                    ? "Đã gửi giấy tờ để nhân viên kiểm tra."
                    : Clean(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException)
            {
                await DeleteSavedFilesAsync(savedFiles);
                TempData["VehiclePartnerError"] = "Không kết nối được Web API tại cổng 7060.";
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException)
            {
                await DeleteSavedFilesAsync(savedFiles);
                TempData["VehiclePartnerError"] = string.IsNullOrWhiteSpace(ex.Message)
                    ? "Không thể lưu giấy tờ. Vui lòng thử lại."
                    : ex.Message;
            }
            return RedirectToAction(nameof(VehicleOperations), new { id });
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMaintenance(int id, DateTime maintenanceDate, int odometerKm, int? nextMaintenanceKm, DateTime? nextMaintenanceDate, string workPerformed, string? garage, decimal cost, bool hasUnresolvedSafetyIssue, string? safetyIssueNote)
        {
            if (!IsVehiclePartnerAccount()) return Forbid();
            try
            {
                var response = await CreateAuthorizedClient().PostAsync($"api/comprehensive-operations/vehicles/{id}/maintenance", JsonContent(new
                {
                    MaintenanceDate = maintenanceDate,
                    OdometerKm = odometerKm,
                    NextMaintenanceKm = nextMaintenanceKm,
                    NextMaintenanceDate = nextMaintenanceDate,
                    WorkPerformed = workPerformed,
                    Garage = garage,
                    Cost = cost,
                    HasUnresolvedSafetyIssue = hasUnresolvedSafetyIssue,
                    SafetyIssueNote = safetyIssueNote
                }));
                TempData[response.IsSuccessStatusCode ? "VehiclePartnerSuccess" : "VehiclePartnerError"] = response.IsSuccessStatusCode
                    ? "Đã lưu lịch sử bảo dưỡng và cập nhật cảnh báo vận hành."
                    : Clean(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException)
            {
                TempData["VehiclePartnerError"] = "Không kết nối được Web API tại cổng 7060.";
            }
            return RedirectToAction(nameof(VehicleOperations), new { id });
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DecideReservation(int id, string decision, string? note)
        {
            if (!IsVehiclePartnerAccount()) return Forbid();
            if (id <= 0)
            {
                TempData["OperationError"] = "Mã đơn thuê không hợp lệ.";
                return RedirectToAction(nameof(Dashboard));
            }

            decision = (decision ?? string.Empty).Trim();
            if (decision is not ("Chấp nhận" or "Từ chối"))
            {
                TempData["OperationError"] = "Vui lòng chọn Chấp nhận hoặc Từ chối yêu cầu thuê xe.";
                return RedirectToAction("Details", "ReservationLookup", new { id });
            }

            try
            {
                var response = await CreateAuthorizedClient().PutAsync(
                    $"api/PartnerVehicles/reservations/{id}/decision",
                    JsonContent(new OwnerReservationDecisionDto
                    {
                        Decision = decision,
                        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
                    }));

                var message = Clean(await response.Content.ReadAsStringAsync());
                if (response.IsSuccessStatusCode)
                {
                    TempData["OperationSuccess"] = string.IsNullOrWhiteSpace(message)
                        ? (decision == "Chấp nhận"
                            ? "Đã chấp nhận yêu cầu. Xe được giữ 15 phút để khách thanh toán giữ chỗ."
                            : "Đã từ chối yêu cầu thuê xe.")
                        : message;
                }
                else
                {
                    TempData["OperationError"] = string.IsNullOrWhiteSpace(message)
                        ? $"Không thể xử lý đơn thuê (HTTP {(int)response.StatusCode})."
                        : message;
                }
            }
            catch (HttpRequestException)
            {
                TempData["OperationError"] = "Không kết nối được Web API tại cổng 7060.";
            }
            catch (Exception)
            {
                TempData["OperationError"] = "Có lỗi khi lưu quyết định của chủ xe. Vui lòng thử lại.";
            }

            return RedirectToAction("Details", "ReservationLookup", new { id });
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAvailability(int id, bool isActive, string? reason)
        {
            if (!IsVehiclePartnerAccount()) return Forbid();
            try
            {
                var response = await CreateAuthorizedClient().PutAsync(
                    $"api/PartnerVehicles/{id}/availability",
                    JsonContent(new UpdatePartnerVehicleAvailabilityDto { IsActive = isActive, Reason = reason }));
                TempData[response.IsSuccessStatusCode ? "VehiclePartnerSuccess" : "VehiclePartnerError"] =
                    Clean(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException)
            {
                TempData["VehiclePartnerError"] = "Không kết nối được Web API tại cổng 7060.";
            }
            return RedirectToAction(nameof(Dashboard));
        }

        private async Task LoadReferenceDataAsync(VehiclePartnerRegisterViewModel model, bool includeProfile)
        {
            try
            {
                var publicClient = _httpClientFactory.CreateClient();
                var locationsResponse = await publicClient.GetAsync("api/Locations");
                if (locationsResponse.IsSuccessStatusCode)
                    model.Locations = (JsonConvert.DeserializeObject<List<ResultLocationDto>>(await locationsResponse.Content.ReadAsStringAsync()) ?? new())
                        .Where(x => x.IsActive)
                        .OrderBy(x => x.ProvinceCity)
                        .ThenBy(x => x.District)
                        .ThenBy(x => x.Name)
                        .ToList();

                var feeResponse = await publicClient.GetAsync("api/PlatformFees");
                if (feeResponse.IsSuccessStatusCode)
                {
                    var fee = JsonConvert.DeserializeObject<PlatformFeeSettingDto>(await feeResponse.Content.ReadAsStringAsync());
                    model.CurrentCommissionRate = fee?.VehiclePartnerCommissionPercent ?? 20m;
                }

                if (includeProfile)
                {
                    var profileResponse = await CreateAuthorizedClient().GetAsync("api/VehiclePartnerProfiles/me");
                    if (profileResponse.IsSuccessStatusCode)
                    {
                        var profile = JsonConvert.DeserializeObject<VehiclePartnerProfileDto>(await profileResponse.Content.ReadAsStringAsync());
                        if (profile is not null)
                        {
                            model.Form.OwnerFullName = !string.IsNullOrWhiteSpace(profile.BusinessName) ? profile.BusinessName : profile.FullName;
                            model.Form.Email = profile.Email;
                            model.Form.Phone = profile.Phone ?? string.Empty;
                            model.Form.Address = !string.IsNullOrWhiteSpace(profile.HeadquartersAddress) ? profile.HeadquartersAddress : profile.CurrentAddress;
                            model.Form.CitizenIdentityNumber = profile.CitizenIdentityNumber ?? string.Empty;
                            model.Form.BankName = profile.BankName ?? string.Empty;
                            model.Form.BankAccountNumber = profile.BankAccountNumber ?? string.Empty;
                            model.Form.BankAccountHolder = profile.BankAccountHolder ?? string.Empty;
                        }
                    }
                }
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "Không tải được dữ liệu vì Web API chưa chạy.");
            }
        }

        private async Task<VehiclePartnerProfileDto?> GetCurrentPartnerProfileAsync()
        {
            try
            {
                var response = await CreateAuthorizedClient().GetAsync("api/VehiclePartnerProfiles/me");
                if (!response.IsSuccessStatusCode) return null;
                return JsonConvert.DeserializeObject<VehiclePartnerProfileDto>(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        private async Task LoadPartnerProfileAsync(VehiclePartnerProfileVerifyViewModel model)
        {
            try
            {
                var response = await CreateAuthorizedClient().GetAsync("api/VehiclePartnerProfiles/me");
                if (!response.IsSuccessStatusCode) return;
                var profile = JsonConvert.DeserializeObject<VehiclePartnerProfileDto>(await response.Content.ReadAsStringAsync());
                model.CurrentProfile = profile;
                if (profile is null) return;
                model.Form.PartnerType = string.IsNullOrWhiteSpace(profile.PartnerType) ? CreateVehiclePartnerAccountDto.IndividualPartnerType : profile.PartnerType;
                model.Form.FullName = profile.FullName;
                model.Form.DateOfBirth = profile.DateOfBirth;
                model.Form.Gender = profile.Gender;
                model.Form.CitizenIdentityNumber = profile.CitizenIdentityNumber;
                model.Form.CitizenIssuedDate = profile.CitizenIssuedDate;
                model.Form.CitizenExpiryDate = profile.CitizenExpiryDate;
                model.Form.PermanentProvinceCode = profile.PermanentProvinceCode;
                model.Form.PermanentWardCode = profile.PermanentWardCode;
                model.Form.PermanentProvince = profile.PermanentProvince;
                model.Form.PermanentWard = profile.PermanentWard;
                model.Form.PermanentDetail = profile.PermanentDetail;
                model.Form.PermanentPaperAddress = profile.PermanentPaperAddress;
                model.Form.PermanentAddress = profile.PermanentAddress;
                model.Form.CurrentAddressSameAsPermanent = profile.CurrentAddressSameAsPermanent;
                model.Form.CurrentProvinceCode = profile.CurrentProvinceCode;
                model.Form.CurrentWardCode = profile.CurrentWardCode;
                model.Form.CurrentProvince = profile.CurrentProvince;
                model.Form.CurrentWard = profile.CurrentWard;
                model.Form.CurrentDetail = profile.CurrentDetail;
                model.Form.CurrentAddress = profile.CurrentAddress;
                model.Form.BusinessName = profile.BusinessName;
                model.Form.TaxCode = profile.TaxCode;
                model.Form.BusinessRegistrationNumber = profile.BusinessRegistrationNumber;
                model.Form.HeadquartersProvinceCode = profile.HeadquartersProvinceCode;
                model.Form.HeadquartersWardCode = profile.HeadquartersWardCode;
                model.Form.HeadquartersProvince = profile.HeadquartersProvince;
                model.Form.HeadquartersWard = profile.HeadquartersWard;
                model.Form.HeadquartersDetail = profile.HeadquartersDetail;
                model.Form.HeadquartersPaperAddress = profile.HeadquartersPaperAddress;
                model.Form.HeadquartersAddress = profile.HeadquartersAddress;
                model.Form.LegalRepresentativeName = profile.LegalRepresentativeName;
                model.Form.AccountManagerName = profile.AccountManagerName;
                model.Form.AccountManagerTitle = profile.AccountManagerTitle;
                model.Form.RepresentativeName = profile.RepresentativeName;
                model.Form.RepresentativeTitle = profile.RepresentativeTitle;
                model.Form.BankName = profile.BankName;
                model.Form.BankAccountNumber = profile.BankAccountNumber;
                model.Form.BankAccountHolder = profile.BankAccountHolder;
                model.Form.BankBranch = profile.BankBranch;
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "Không tải được hồ sơ đối tác vì Web API chưa chạy.");
            }
        }

        private bool IsVehiclePartnerAccount()
            => string.Equals(User.FindFirstValue("IsVehiclePartner"), "true", StringComparison.OrdinalIgnoreCase);

        private void RemoveIrrelevantPartnerProfileModelStateErrors(string partnerType, bool currentAddressSameAsPermanent)
        {
            static void RemoveMany(ModelStateDictionary modelState, params string[] keys)
            {
                foreach (var key in keys)
                {
                    modelState.Remove(key);
                    modelState.Remove($"Form.{key}");
                }
            }

            RemoveMany(ModelState,
                nameof(SubmitVehiclePartnerProfileDto.PermanentAddress),
                nameof(SubmitVehiclePartnerProfileDto.CurrentAddress),
                nameof(SubmitVehiclePartnerProfileDto.HeadquartersAddress),
                nameof(SubmitVehiclePartnerProfileDto.RepresentativeName),
                nameof(SubmitVehiclePartnerProfileDto.RepresentativeTitle));

            if (partnerType == CreateVehiclePartnerAccountDto.IndividualPartnerType)
            {
                RemoveMany(ModelState,
                    nameof(SubmitVehiclePartnerProfileDto.BusinessName),
                    nameof(SubmitVehiclePartnerProfileDto.TaxCode),
                    nameof(SubmitVehiclePartnerProfileDto.BusinessRegistrationNumber),
                    nameof(SubmitVehiclePartnerProfileDto.HeadquartersProvinceCode),
                    nameof(SubmitVehiclePartnerProfileDto.HeadquartersWardCode),
                    nameof(SubmitVehiclePartnerProfileDto.HeadquartersProvince),
                    nameof(SubmitVehiclePartnerProfileDto.HeadquartersWard),
                    nameof(SubmitVehiclePartnerProfileDto.HeadquartersDetail),
                    nameof(SubmitVehiclePartnerProfileDto.HeadquartersPaperAddress),
                    nameof(SubmitVehiclePartnerProfileDto.LegalRepresentativeName),
                    nameof(SubmitVehiclePartnerProfileDto.AccountManagerName),
                    nameof(SubmitVehiclePartnerProfileDto.AccountManagerTitle),
                    nameof(SubmitVehiclePartnerProfileDto.BusinessLicenseFileId),
                    nameof(SubmitVehiclePartnerProfileDto.AuthorizationDocumentFileId));

                if (currentAddressSameAsPermanent)
                {
                    RemoveMany(ModelState,
                        nameof(SubmitVehiclePartnerProfileDto.CurrentProvinceCode),
                        nameof(SubmitVehiclePartnerProfileDto.CurrentWardCode),
                        nameof(SubmitVehiclePartnerProfileDto.CurrentProvince),
                        nameof(SubmitVehiclePartnerProfileDto.CurrentWard),
                        nameof(SubmitVehiclePartnerProfileDto.CurrentDetail));
                }
            }
            else if (partnerType == CreateVehiclePartnerAccountDto.OrganizationPartnerType)
            {
                RemoveMany(ModelState,
                    nameof(SubmitVehiclePartnerProfileDto.FullName),
                    nameof(SubmitVehiclePartnerProfileDto.DateOfBirth),
                    nameof(SubmitVehiclePartnerProfileDto.Gender),
                    nameof(SubmitVehiclePartnerProfileDto.CitizenIdentityNumber),
                    nameof(SubmitVehiclePartnerProfileDto.CitizenIssuedDate),
                    nameof(SubmitVehiclePartnerProfileDto.CitizenExpiryDate),
                    nameof(SubmitVehiclePartnerProfileDto.PermanentProvinceCode),
                    nameof(SubmitVehiclePartnerProfileDto.PermanentWardCode),
                    nameof(SubmitVehiclePartnerProfileDto.PermanentProvince),
                    nameof(SubmitVehiclePartnerProfileDto.PermanentWard),
                    nameof(SubmitVehiclePartnerProfileDto.PermanentDetail),
                    nameof(SubmitVehiclePartnerProfileDto.PermanentPaperAddress),
                    nameof(SubmitVehiclePartnerProfileDto.CurrentProvinceCode),
                    nameof(SubmitVehiclePartnerProfileDto.CurrentWardCode),
                    nameof(SubmitVehiclePartnerProfileDto.CurrentProvince),
                    nameof(SubmitVehiclePartnerProfileDto.CurrentWard),
                    nameof(SubmitVehiclePartnerProfileDto.CurrentDetail),
                    nameof(SubmitVehiclePartnerProfileDto.CitizenFrontFileId),
                    nameof(SubmitVehiclePartnerProfileDto.CitizenBackFileId),
                    nameof(SubmitVehiclePartnerProfileDto.PortraitFileId));
            }
        }

        private static void PreparePartnerProfilePayloadBeforeSubmit(SubmitVehiclePartnerProfileDto form)
        {
            form.PartnerType = form.PartnerType?.Trim() ?? CreateVehiclePartnerAccountDto.IndividualPartnerType;

            if (form.PartnerType == CreateVehiclePartnerAccountDto.IndividualPartnerType)
            {
                if (form.CurrentAddressSameAsPermanent)
                {
                    form.CurrentProvinceCode = form.PermanentProvinceCode ?? string.Empty;
                    form.CurrentWardCode = form.PermanentWardCode ?? string.Empty;
                    form.CurrentProvince = form.PermanentProvince ?? string.Empty;
                    form.CurrentWard = form.PermanentWard ?? string.Empty;
                    form.CurrentDetail = form.PermanentDetail ?? string.Empty;
                }

                form.BusinessName = string.Empty;
                form.TaxCode = string.Empty;
                form.BusinessRegistrationNumber = string.Empty;
                form.HeadquartersProvinceCode = string.Empty;
                form.HeadquartersWardCode = string.Empty;
                form.HeadquartersProvince = string.Empty;
                form.HeadquartersWard = string.Empty;
                form.HeadquartersDetail = string.Empty;
                form.HeadquartersPaperAddress = string.Empty;
                form.HeadquartersAddress = string.Empty;
                form.LegalRepresentativeName = string.Empty;
                form.AccountManagerName = string.Empty;
                form.AccountManagerTitle = string.Empty;
                form.RepresentativeName = string.Empty;
                form.RepresentativeTitle = string.Empty;
                form.BusinessLicenseFileId = Guid.Empty;
                form.AuthorizationDocumentFileId = null;
            }
            else if (form.PartnerType == CreateVehiclePartnerAccountDto.OrganizationPartnerType)
            {
                form.FullName = string.Empty;
                form.DateOfBirth = null;
                form.Gender = string.Empty;
                form.CitizenIdentityNumber = string.Empty;
                form.CitizenIssuedDate = null;
                form.CitizenExpiryDate = null;
                form.PermanentProvinceCode = string.Empty;
                form.PermanentWardCode = string.Empty;
                form.PermanentProvince = string.Empty;
                form.PermanentWard = string.Empty;
                form.PermanentDetail = string.Empty;
                form.PermanentPaperAddress = string.Empty;
                form.PermanentAddress = string.Empty;
                form.CurrentAddressSameAsPermanent = false;
                form.CurrentProvinceCode = string.Empty;
                form.CurrentWardCode = string.Empty;
                form.CurrentProvince = string.Empty;
                form.CurrentWard = string.Empty;
                form.CurrentDetail = string.Empty;
                form.CurrentAddress = string.Empty;
                form.CitizenFrontFileId = Guid.Empty;
                form.CitizenBackFileId = Guid.Empty;
                form.PortraitFileId = Guid.Empty;
            }
        }

        private void ValidateFile(IFormFile? file, string key, string displayName)
        {
            if (file is null || file.Length == 0)
            {
                ModelState.AddModelError(key, $"Vui lòng tải {displayName}.");
                return;
            }
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension)) ModelState.AddModelError(key, $"{displayName} chỉ chấp nhận JPG, PNG, WEBP hoặc PDF.");
            if (file.Length > MaxFileSize) ModelState.AddModelError(key, $"{displayName} không được vượt quá 6 MB.");
        }

        private void ValidateImageFile(IFormFile? file, string key, string displayName)
        {
            if (file is null || file.Length == 0)
            {
                ModelState.AddModelError(key, $"Vui lòng tải {displayName}.");
                return;
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                ModelState.AddModelError(key, $"{displayName} chỉ chấp nhận JPG, PNG hoặc WEBP.");
            if (file.Length > MaxFileSize)
                ModelState.AddModelError(key, $"{displayName} không được vượt quá 6 MB.");
        }

        private async Task<Guid> SavePrivateFileAsync(IFormFile file, List<string> savedFiles, string category)
            => (await SavePrivateFileUploadAsync(file, savedFiles, category)).PrivateFileId;

        private async Task<SmartCar.Dto.ReservationDtos.SecureFileUploadResultDto> SavePrivateFileUploadAsync(IFormFile file, List<string> savedFiles, string category)
        {
            using var form = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            using var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            form.Add(content, "file", Path.GetFileName(file.FileName));
            form.Add(new StringContent(category), "category");
            var response = await CreateAuthorizedClient().PostAsync("api/secure-files/upload", form);
            var raw = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new IOException(Clean(raw));
            var result = JsonConvert.DeserializeObject<SmartCar.Dto.ReservationDtos.SecureFileUploadResultDto>(raw)
                ?? throw new IOException("API không trả về FileId.");
            savedFiles.Add($"private:{result.PrivateFileId:D}");
            return result;
        }

        private async Task<string> SavePublicVehicleImageAsync(IFormFile file, List<string> savedFiles)
        {
            // Ảnh ngoại thất/nội thất dùng cho trang tìm xe công khai. Kiểm tra nội dung thật,
            // không tin phần mở rộng hoặc Content-Type do trình duyệt gửi lên.
            await using var validationStream = file.OpenReadStream();
            var inspected = await FileUploadSecurity.InspectAsync(
                validationStream,
                file.FileName,
                file.ContentType,
                file.Length,
                FileUploadProfile.PublicVehicleImage,
                HttpContext.RequestAborted);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? throw new UnauthorizedAccessException("Không xác định được tài khoản tải ảnh xe.");
            if (!int.TryParse(userId, out var ownerId) || ownerId <= 0)
                throw new UnauthorizedAccessException("Không xác định được tài khoản tải ảnh xe.");
            var folder = Path.Combine(_environment.WebRootPath, "uploads", "vehicle-images", ownerId.ToString());
            Directory.CreateDirectory(folder);
            var fileName = $"{Guid.NewGuid():N}{inspected.Extension}";
            var fullPath = Path.Combine(folder, fileName);
            await using var source = file.OpenReadStream();
            await using var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
            await FileUploadSecurity.WriteSafeContentAsync(source, stream, inspected, HttpContext.RequestAborted);
            await stream.FlushAsync(HttpContext.RequestAborted);
            savedFiles.Add(fullPath);
            return $"/uploads/vehicle-images/{ownerId}/{fileName}";
        }

        private async Task DeleteSavedFilesAsync(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                if (file.StartsWith("private:", StringComparison.OrdinalIgnoreCase) && Guid.TryParse(file[8..], out var fileId))
                {
                    try { await CreateAuthorizedClient().DeleteAsync($"api/secure-files/{fileId:D}"); }
                    catch (HttpRequestException) { }
                    continue;
                }
                try { if (System.IO.File.Exists(file)) System.IO.File.Delete(file); } catch (IOException) { }
            }
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

        private HttpClient CreateAuthorizedClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = User.FindFirst("carbooktoken")?.Value;
            if (!string.IsNullOrWhiteSpace(token)) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<IActionResult> ProxyAdministrativeUnitsAsync(string path, CancellationToken cancellationToken)
        {
            try
            {
                using var client = CreateAuthorizedClient();
                using var response = await client.GetAsync(path, cancellationToken);
                var raw = await response.Content.ReadAsStringAsync(cancellationToken);
                return new ContentResult
                {
                    StatusCode = (int)response.StatusCode,
                    ContentType = "application/json; charset=utf-8",
                    Content = string.IsNullOrWhiteSpace(raw) ? "[]" : raw
                };
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Không tải được danh mục hành chính." });
            }
        }

        private static StringContent JsonContent(object value) => new(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json");

        private static string Clean(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Không thể xử lý yêu cầu.";
            try
            {
                var obj = JsonConvert.DeserializeObject<dynamic>(raw);
                var message = (string?)obj?.message ?? (string?)obj?.Message;
                if (!string.IsNullOrWhiteSpace(message)) return message;
            }
            catch (JsonException) { }
            return raw.Trim().Trim('"');
        }
    }
}
