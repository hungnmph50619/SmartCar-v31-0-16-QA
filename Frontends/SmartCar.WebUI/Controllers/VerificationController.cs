using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartCar.Dto.ReservationDtos;
using SmartCar.WebUI.Models;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace SmartCar.WebUI.Controllers
{
    [Authorize(Roles = "Customer")]
    public class VerificationController : Controller
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxFileSize = 5 * 1024 * 1024;
        private readonly IHttpClientFactory _httpClientFactory;
        public VerificationController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? returnUrl = null)
        {
            if (IsVehiclePartnerAccount()) return RedirectToAction("Dashboard", "VehiclePartner");
            var readiness = await LoadReadinessAsync();
            ViewBag.Readiness = readiness;
            return View(new CustomerVerificationViewModel
            {
                Phone = readiness?.Phone ?? string.Empty,
                LegalFullName = readiness?.LegalFullName ?? string.Empty,
                Gender = readiness?.Gender ?? string.Empty,
                CitizenIdAddress = readiness?.CitizenIdAddress ?? string.Empty,
                PermanentProvinceCode = readiness?.PermanentProvinceCode ?? string.Empty,
                PermanentWardCode = readiness?.PermanentWardCode ?? string.Empty,
                PermanentProvince = readiness?.PermanentProvince ?? string.Empty,
                PermanentWard = readiness?.PermanentWard ?? string.Empty,
                PermanentDetail = readiness?.PermanentDetail ?? string.Empty,
                PermanentAddress = readiness?.PermanentAddress ?? string.Empty,
                CurrentAddressSameAsPermanent = readiness?.CurrentAddressSameAsPermanent ?? false,
                CurrentProvinceCode = readiness?.CurrentProvinceCode ?? string.Empty,
                CurrentWardCode = readiness?.CurrentWardCode ?? string.Empty,
                CurrentProvince = readiness?.CurrentProvince ?? string.Empty,
                CurrentWard = readiness?.CurrentWard ?? string.Empty,
                CurrentDetail = readiness?.CurrentDetail ?? string.Empty,
                CurrentAddress = readiness?.CurrentAddress ?? string.Empty,
                DriverLicenseNumber = readiness?.DriverLicenseNumber ?? string.Empty,
                DriverLicenseClass = readiness?.DriverLicenseClass ?? string.Empty,
                CitizenIdIssuedDate = readiness?.CitizenIdIssuedDate ?? SmartCar.WebUI.Infrastructure.VietnamTime.Today.AddYears(-2),
                CitizenIdExpiryDate = readiness?.CitizenIdExpiryDate ?? SmartCar.WebUI.Infrastructure.VietnamTime.Today.AddYears(8),
                DateOfBirth = readiness?.DateOfBirth ?? SmartCar.WebUI.Infrastructure.VietnamTime.Today.AddYears(-22),
                DriverLicenseIssuedDate = readiness?.DriverLicenseIssuedDate ?? SmartCar.WebUI.Infrastructure.VietnamTime.Today.AddYears(-2),
                DriverLicenseExpiry = readiness?.DriverLicenseExpiry ?? SmartCar.WebUI.Infrastructure.VietnamTime.Today.AddYears(3),
                ReturnUrl = returnUrl
            });
        }

        [HttpGet]
        public Task<IActionResult> Provinces(CancellationToken cancellationToken)
            => ProxyAdministrativeUnitsAsync("api/administrative-units/provinces", cancellationToken);

        [HttpGet]
        public Task<IActionResult> Wards(string provinceCode, CancellationToken cancellationToken)
        {
            var code = (provinceCode ?? string.Empty).Trim();
            if (code.Length != 2) return Task.FromResult<IActionResult>(BadRequest("Mã tỉnh/thành phố không hợp lệ."));
            return ProxyAdministrativeUnitsAsync($"api/administrative-units/provinces/{Uri.EscapeDataString(code)}/wards", cancellationToken);
        }

        [HttpPost]
        public async Task<IActionResult> Index(CustomerVerificationViewModel model)
        {
            if (IsVehiclePartnerAccount()) return RedirectToAction("Dashboard", "VehiclePartner");
            var readiness = await LoadReadinessAsync();
            if (readiness?.VerificationStatus is "Chờ duyệt" or "Chờ duyệt hồ sơ")
            {
                TempData["VerificationInfo"] = "Hồ sơ của bạn đang chờ nhân viên duyệt. Trong thời gian này bạn không thể chỉnh sửa trực tiếp.";
                return RedirectToAction(nameof(Index));
            }
            if (readiness?.VerificationStatus == "Đã xác minh")
            {
                TempData["VerificationInfo"] = "Hồ sơ của bạn đã được xác minh. Thông tin pháp lý đã được khóa; nếu cần thay đổi, hãy gửi yêu cầu cập nhật hồ sơ.";
                return RedirectToAction(nameof(Index));
            }
            NormalizeAddressInput(model);
            ValidateAddressModel(model);
            ValidateDates(model);
            ValidateCitizenId(model, readiness);
            ValidateRequiredFile(model.CitizenIdFront, nameof(model.CitizenIdFront), readiness?.HasCitizenIdFront == true, "Vui lòng tải ảnh CCCD mặt trước.");
            ValidateRequiredFile(model.CitizenIdBack, nameof(model.CitizenIdBack), readiness?.HasCitizenIdBack == true, "Vui lòng tải ảnh CCCD mặt sau.");
            ValidateRequiredFile(model.DriverLicense, nameof(model.DriverLicense), readiness?.HasDriverLicense == true, "Vui lòng tải ảnh bằng lái.");
            ValidateRequiredFile(model.Portrait, nameof(model.Portrait), readiness?.HasPortrait == true, "Vui lòng tải ảnh chân dung.");
            if (!ModelState.IsValid)
            {
                ViewBag.Readiness = readiness;
                return View(model);
            }

            var uploadedFileIds = new List<Guid>();
            var submissionSucceeded = false;
            try
            {
                Guid? front = model.CitizenIdFront is { Length: > 0 } ? await UploadAndTrackAsync(model.CitizenIdFront, "CustomerCitizenIdFront", uploadedFileIds) : null;
                Guid? back = model.CitizenIdBack is { Length: > 0 } ? await UploadAndTrackAsync(model.CitizenIdBack, "CustomerCitizenIdBack", uploadedFileIds) : null;
                Guid? license = model.DriverLicense is { Length: > 0 } ? await UploadAndTrackAsync(model.DriverLicense, "CustomerDriverLicense", uploadedFileIds) : null;
                Guid? portrait = model.Portrait is { Length: > 0 } ? await UploadAndTrackAsync(model.Portrait, "CustomerPortrait", uploadedFileIds) : null;
                var dto = new SubmitVerificationDto
                {
                    Phone = model.Phone,
                    LegalFullName = model.LegalFullName,
                    Gender = model.Gender,
                    CitizenIdAddress = model.CitizenIdAddress,
                    PermanentProvinceCode = model.PermanentProvinceCode,
                    PermanentWardCode = model.PermanentWardCode,
                    PermanentDetail = model.PermanentDetail,
                    CurrentAddressSameAsPermanent = model.CurrentAddressSameAsPermanent,
                    CurrentProvinceCode = model.CurrentProvinceCode,
                    CurrentWardCode = model.CurrentWardCode,
                    CurrentDetail = model.CurrentDetail,
                    DriverLicenseNumber = model.DriverLicenseNumber,
                    DriverLicenseClass = model.DriverLicenseClass,
                    CitizenIdIssuedDate = model.CitizenIdIssuedDate,
                    CitizenIdExpiryDate = model.CitizenIdExpiryDate,
                    CitizenIdFrontFileId = front,
                    CitizenIdBackFileId = back,
                    DriverLicenseFileId = license,
                    PortraitFileId = portrait,
                    DateOfBirth = model.DateOfBirth,
                    DriverLicenseIssuedDate = model.DriverLicenseIssuedDate,
                    DriverLicenseExpiry = model.DriverLicenseExpiry,
                    CitizenIdentityNumber = (model.CitizenIdNumber ?? string.Empty).Trim()
                };
                var json = JsonConvert.SerializeObject(dto);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await CreateAuthorizedClient().PostAsync("api/operations/customer/verification", content);
                if (response.IsSuccessStatusCode)
                {
                    submissionSucceeded = true;
                    TempData["VerificationSuccess"] = readiness?.VerificationStatus == "Cần xác minh lại"
                        ? "Đã gửi lại hồ sơ sau yêu cầu xác minh lại. Hồ sơ mới đã quay về trạng thái Chờ duyệt."
                        : readiness?.VerificationStatus is "Bị từ chối" or "Yêu cầu bổ sung"
                            ? "Đã gửi lại hồ sơ sau khi bổ sung. Hồ sơ mới đã quay về trạng thái Chờ duyệt."
                            : "Đã gửi hồ sơ xác minh. Nhân viên chỉ có quyền duyệt, từ chối hoặc yêu cầu bổ sung; không được sửa dữ liệu của bạn.";
                    if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)) return Redirect(model.ReturnUrl);
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError(string.Empty, Clean(await response.Content.ReadAsStringAsync()));
            }
            catch (IOException)
            {
                ModelState.AddModelError(string.Empty, "Không lưu được ảnh hồ sơ. Vui lòng kiểm tra dung lượng và thử lại.");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "Không kết nối được Web API.");
            }
            finally
            {
                if (!submissionSucceeded && uploadedFileIds.Count > 0)
                    await DeleteUploadedFilesAsync(uploadedFileIds);
            }

            ViewBag.Readiness = await LoadReadinessAsync();
            return View(model);
        }

        private static void NormalizeAddressInput(CustomerVerificationViewModel model)
        {
            model.PermanentProvinceCode = (model.PermanentProvinceCode ?? string.Empty).Trim();
            model.PermanentWardCode = (model.PermanentWardCode ?? string.Empty).Trim();
            model.PermanentDetail = (model.PermanentDetail ?? string.Empty).Trim();
            model.CurrentProvinceCode = (model.CurrentProvinceCode ?? string.Empty).Trim();
            model.CurrentWardCode = (model.CurrentWardCode ?? string.Empty).Trim();
            model.CurrentDetail = (model.CurrentDetail ?? string.Empty).Trim();
            model.CitizenIdAddress = (model.CitizenIdAddress ?? string.Empty).Trim();

            if (model.CurrentAddressSameAsPermanent)
            {
                model.CurrentProvinceCode = model.PermanentProvinceCode;
                model.CurrentWardCode = model.PermanentWardCode;
                model.CurrentDetail = model.PermanentDetail;
            }
        }

        private void ValidateAddressModel(CustomerVerificationViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.CitizenIdAddress))
                ModelState.AddModelError(nameof(model.CitizenIdAddress), "Vui lòng nhập địa chỉ theo giấy tờ để nhân viên đối chiếu CCCD.");
            if (string.IsNullOrWhiteSpace(model.PermanentProvinceCode))
                ModelState.AddModelError(nameof(model.PermanentProvinceCode), "Vui lòng chọn tỉnh/thành phố thường trú.");
            if (string.IsNullOrWhiteSpace(model.PermanentWardCode))
                ModelState.AddModelError(nameof(model.PermanentWardCode), "Vui lòng chọn xã/phường/đặc khu thường trú.");
            if (string.IsNullOrWhiteSpace(model.PermanentDetail))
                ModelState.AddModelError(nameof(model.PermanentDetail), "Vui lòng nhập số nhà, đường, thôn/xóm/tổ dân phố thường trú.");
            if (!model.CurrentAddressSameAsPermanent)
            {
                if (string.IsNullOrWhiteSpace(model.CurrentProvinceCode))
                    ModelState.AddModelError(nameof(model.CurrentProvinceCode), "Vui lòng chọn tỉnh/thành phố hiện tại.");
                if (string.IsNullOrWhiteSpace(model.CurrentWardCode))
                    ModelState.AddModelError(nameof(model.CurrentWardCode), "Vui lòng chọn xã/phường/đặc khu hiện tại.");
                if (string.IsNullOrWhiteSpace(model.CurrentDetail))
                    ModelState.AddModelError(nameof(model.CurrentDetail), "Vui lòng nhập số nhà, đường, thôn/xóm/tổ dân phố hiện tại.");
            }
        }

        private void ValidateCitizenId(CustomerVerificationViewModel model, CustomerReadinessDto? readiness)
        {
            var value = model.CitizenIdNumber?.Trim() ?? string.Empty;
            // Không lưu số CCCD đầy đủ trong hồ sơ; vì vậy mỗi lần gửi/gửi lại hồ sơ
            // người dùng phải nhập lại đủ 12 số để server tự tạo bản che an toàn.
            if (string.IsNullOrWhiteSpace(value))
            {
                ModelState.AddModelError(nameof(model.CitizenIdNumber), "Vui lòng nhập đủ 12 số CCCD.");
                return;
            }
            if (value.Length != 12 || value.Any(c => !char.IsDigit(c)))
                ModelState.AddModelError(nameof(model.CitizenIdNumber), "CCCD phải gồm đúng 12 chữ số.");
        }

        private void ValidateRequiredFile(IFormFile? file, string key, bool hasExistingFile, string requiredMessage)
        {
            if ((file is null || file.Length == 0) && !hasExistingFile)
            {
                ModelState.AddModelError(key, requiredMessage);
                return;
            }
            ValidateFile(file, key);
        }

        private void ValidateDates(CustomerVerificationViewModel model)
        {
            if (model.DateOfBirth > SmartCar.WebUI.Infrastructure.VietnamTime.Today.AddYears(-18)) ModelState.AddModelError(nameof(model.DateOfBirth), "Khách thuê phải đủ 18 tuổi.");
            if (model.CitizenIdIssuedDate > SmartCar.WebUI.Infrastructure.VietnamTime.Today) ModelState.AddModelError(nameof(model.CitizenIdIssuedDate), "Ngày cấp CCCD không hợp lệ.");
            if (model.CitizenIdExpiryDate <= SmartCar.WebUI.Infrastructure.VietnamTime.Today) ModelState.AddModelError(nameof(model.CitizenIdExpiryDate), "CCCD phải còn hiệu lực.");
            if (model.CitizenIdExpiryDate <= model.CitizenIdIssuedDate) ModelState.AddModelError(nameof(model.CitizenIdExpiryDate), "Ngày hết hạn CCCD phải sau ngày cấp.");
            if (model.DriverLicenseIssuedDate > SmartCar.WebUI.Infrastructure.VietnamTime.Today) ModelState.AddModelError(nameof(model.DriverLicenseIssuedDate), "Ngày cấp bằng lái không hợp lệ.");
            if (model.DriverLicenseExpiry <= SmartCar.WebUI.Infrastructure.VietnamTime.Today) ModelState.AddModelError(nameof(model.DriverLicenseExpiry), "Bằng lái phải còn hiệu lực.");
            if (model.DriverLicenseExpiry <= model.DriverLicenseIssuedDate) ModelState.AddModelError(nameof(model.DriverLicenseExpiry), "Ngày hết hạn phải sau ngày cấp.");
        }

        private void ValidateFile(IFormFile? file, string key)
        {
            if (file is null || file.Length == 0) return;
            if (file.Length > MaxFileSize) ModelState.AddModelError(key, "Mỗi ảnh không được vượt quá 5 MB.");
            if (!AllowedExtensions.Contains(Path.GetExtension(file.FileName))) ModelState.AddModelError(key, "Chỉ chấp nhận JPG, PNG hoặc WEBP.");
            if (!HasValidImageSignature(file)) ModelState.AddModelError(key, "Nội dung tệp không đúng định dạng ảnh JPG, PNG hoặc WEBP.");
        }

        private static bool HasValidImageSignature(IFormFile file)
        {
            Span<byte> header = stackalloc byte[12];
            using var stream = file.OpenReadStream();
            var read = stream.Read(header);
            if (read < 4) return false;
            var jpg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
            var png = read >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
            var webp = read >= 12 && header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F'
                && header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P';
            return jpg || png || webp;
        }


        private async Task<Guid> UploadAndTrackAsync(IFormFile file, string category, ICollection<Guid> uploadedFileIds)
        {
            var fileId = await UploadPrivateFileAsync(file, category);
            uploadedFileIds.Add(fileId);
            return fileId;
        }

        private async Task DeleteUploadedFilesAsync(IEnumerable<Guid> fileIds)
        {
            using var client = CreateAuthorizedClient();
            foreach (var fileId in fileIds.Distinct())
            {
                try
                {
                    await client.DeleteAsync($"api/secure-files/{fileId:D}");
                }
                catch
                {
                    // API cleanup nền vẫn xử lý file bỏ dở sau 24 giờ.
                }
            }
        }

        private async Task<Guid> UploadPrivateFileAsync(IFormFile file, string category)
        {
            using var form = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            form.Add(fileContent, "file", Path.GetFileName(file.FileName));
            form.Add(new StringContent(category), "category");
            var response = await CreateAuthorizedClient().PostAsync("api/secure-files/upload", form);
            var raw = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new IOException(Clean(raw));
            var result = JsonConvert.DeserializeObject<SecureFileUploadResultDto>(raw) ?? throw new IOException("API không trả về FileId.");
            return result.PrivateFileId;
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

        private async Task<CustomerReadinessDto?> LoadReadinessAsync()
        {
            try
            {
                var response = await CreateAuthorizedClient().GetAsync("api/operations/customer/readiness");
                return response.IsSuccessStatusCode ? JsonConvert.DeserializeObject<CustomerReadinessDto>(await response.Content.ReadAsStringAsync()) : null;
            }
            catch { return null; }
        }

        private HttpClient CreateAuthorizedClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = User.FindFirst("carbooktoken")?.Value;
            if (!string.IsNullOrWhiteSpace(token)) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private int UserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        private bool IsVehiclePartnerAccount() => string.Equals(User.FindFirstValue("IsVehiclePartner"), "true", StringComparison.OrdinalIgnoreCase);
        private static string Clean(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Không gửi được hồ sơ.";
            var text = raw.Trim().Trim('"');
            if (!text.StartsWith("{")) return text;
            try { var json = JObject.Parse(raw); return json["message"]?.ToString() ?? json["title"]?.ToString() ?? "Dữ liệu hồ sơ chưa hợp lệ."; }
            catch { return "Dữ liệu hồ sơ chưa hợp lệ."; }
        }
    }
}
