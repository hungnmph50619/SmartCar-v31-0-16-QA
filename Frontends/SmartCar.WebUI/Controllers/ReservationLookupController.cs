using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartCar.Dto.ReservationDtos;
using SmartCar.WebUI.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace SmartCar.WebUI.Controllers
{
    [Authorize]
    public class ReservationLookupController : Controller
    {
        private static readonly HashSet<string> AllowedEvidenceExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp", ".mp4", ".mov" };
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public ReservationLookupController(IHttpClientFactory httpClientFactory, IWebHostEnvironment environment, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _environment = environment;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? group = null)
        {
            if (!User.IsInRole("Customer") || IsVehiclePartnerAccount()) return RedirectToRoleDashboard();
            ViewBag.v1 = "Đơn thuê của tôi";
            ViewBag.v2 = "Theo dõi yêu cầu thuê xe và việc cần làm";
            ViewBag.Group = group ?? "Tất cả";

            try
            {
                var response = await Client().GetAsync("api/Reservations/me");
                if (response.IsSuccessStatusCode)
                {
                    var model = JsonConvert.DeserializeObject<List<ResultReservationDto>>(await response.Content.ReadAsStringAsync()) ?? new();
                    return View(Filter(model, group));
                }
                if (response.StatusCode == HttpStatusCode.Unauthorized) return RedirectToAction("Index", "Login");
                ViewBag.ErrorMessage = Clean(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException) { ViewBag.ErrorMessage = "Không kết nối được Web API."; }
            catch (JsonException) { ViewBag.ErrorMessage = "Dữ liệu đơn thuê không đúng định dạng."; }
            return View(new List<ResultReservationDto>());
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0) return RedirectToRoleDashboard();
            try
            {
                var client = Client();
                var response = await client.GetAsync($"api/operations/reservations/{id}/detail");
                if (response.IsSuccessStatusCode)
                {
                    var model = JsonConvert.DeserializeObject<ReservationDetailDto>(await response.Content.ReadAsStringAsync());
                    if (model is null) return NotFound();
                    ViewBag.IsCustomer = User.IsInRole("Customer") && !IsVehiclePartnerAccount();
                    ViewBag.IsOwner = IsVehiclePartnerAccount();
                    ViewBag.IsStaff = User.IsInRole("Staff");
                    ViewBag.IsAdmin = User.IsInRole("Admin");
                    var paymentEnabled = bool.TryParse(_configuration["ManualPayment:Enabled"], out var enabled) && enabled;
                    var paymentBankName = _configuration["ManualPayment:BankName"]?.Trim() ?? string.Empty;
                    var paymentAccountNumber = _configuration["ManualPayment:AccountNumber"]?.Trim() ?? string.Empty;
                    var paymentAccountHolder = _configuration["ManualPayment:AccountHolder"]?.Trim() ?? string.Empty;
                    var paymentQrImagePath = _configuration["ManualPayment:QrImagePath"]?.Trim() ?? string.Empty;
                    ViewBag.PaymentConfigured = paymentEnabled
                        && !string.IsNullOrWhiteSpace(paymentBankName)
                        && !string.IsNullOrWhiteSpace(paymentAccountNumber)
                        && !string.IsNullOrWhiteSpace(paymentAccountHolder);
                    ViewBag.PaymentBankName = paymentBankName;
                    ViewBag.PaymentAccountNumber = paymentAccountNumber;
                    ViewBag.PaymentAccountHolder = paymentAccountHolder;
                    ViewBag.PaymentQrImagePath = paymentQrImagePath;
                    ViewBag.PaymentIsDemoQr = bool.TryParse(_configuration["ManualPayment:IsDemoQr"], out var isDemoQr) && isDemoQr;

                    if (ViewBag.IsCustomer == true && model.CanCancel)
                    {
                        try
                        {
                            var previewResponse = await client.GetAsync($"api/Reservations/{id}/cancellation-preview");
                            if (previewResponse.IsSuccessStatusCode)
                            {
                                ViewBag.CancellationPreview = JsonConvert.DeserializeObject<CancellationPreviewDto>(
                                    await previewResponse.Content.ReadAsStringAsync());
                            }
                        }
                        catch (HttpRequestException)
                        {
                            // Chi tiết đơn vẫn hiển thị; server sẽ tính lại phí khi khách xác nhận hủy.
                        }
                    }

                    return View(model);
                }
                TempData["OperationError"] = Clean(await response.Content.ReadAsStringAsync());
                return RedirectToRoleDashboard();
            }
            catch (HttpRequestException)
            {
                TempData["OperationError"] = "Không kết nối được Web API.";
                return RedirectToRoleDashboard();
            }
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            var normalizedReason = (reason ?? string.Empty).Trim();
            if (normalizedReason.Length < 10)
            {
                TempData["OperationError"] = "Vui lòng nhập lý do hủy đơn ít nhất 10 ký tự.";
                return RedirectToAction(nameof(Details), new { id });
            }
            if (normalizedReason.Length > 500)
            {
                TempData["OperationError"] = "Lý do hủy đơn không được vượt quá 500 ký tự.";
                return RedirectToAction(nameof(Details), new { id });
            }
            return await ExecuteAndReturn(HttpMethod.Put, $"api/Reservations/{id}/cancel", new { Reason = normalizedReason }, id, "Đã hủy đơn thuê.");
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        public async Task<IActionResult> SubmitPaymentProof(int id, string? paymentType, int? additionalChargeId)
            => await ExecuteAndReturn(
                HttpMethod.Post,
                $"api/operations/reservations/{id}/payment-proof",
                new { Provider = "Chuyển khoản ngân hàng thủ công", PaymentType = paymentType, AdditionalChargeID = additionalChargeId },
                id,
                "Đã báo chuyển khoản. SmartCar đang đối chiếu; vui lòng không chuyển lại lần nữa.");

        [Authorize(Roles = "VehiclePartner")]
        [HttpPost]
        public async Task<IActionResult> OwnerDecision(int id, string decision, string? note)
            => await ExecuteAndReturn(HttpMethod.Put, $"api/PartnerVehicles/reservations/{id}/decision", new { Decision = decision, Note = note }, id, decision == "Chấp nhận" ? "Đã chấp nhận. Xe được giữ 10 phút để khách thanh toán." : "Đã từ chối yêu cầu.");

        [HttpPost]
        public async Task<IActionResult> CreateHandover(int id, string reportType, int odometerKm, int fuelPercent, string? existingDamage, string? accessories, string? locationText, List<IFormFile>? photos)
        {
            var photoFileIds = await SaveEvidenceAsync(id, photos, "handover");
            return await ExecuteAndReturn(HttpMethod.Post, $"api/marketplace-operations/reservations/{id}/handover", new
            {
                ReportType = reportType,
                OdometerKm = odometerKm,
                FuelPercent = fuelPercent,
                ExistingDamage = existingDamage,
                Accessories = accessories,
                LocationText = locationText,
                PhotoFileIds = photoFileIds
            }, id, $"Đã lập biên bản {reportType.ToLowerInvariant()} và gửi OTP cho khách.", photoFileIds);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        public async Task<IActionResult> ConfirmOtp(int id, int reportId, string otp)
            => await ExecuteAndReturn(HttpMethod.Post, $"api/marketplace-operations/handover/{reportId}/confirm", new { Otp = otp }, id, "Đã xác nhận biên bản bằng OTP.");

        [HttpPost]
        public async Task<IActionResult> ResendOtp(int id, int reportId)
            => await ExecuteAndReturn(HttpMethod.Post, $"api/marketplace-operations/handover/{reportId}/resend-otp", null, id, "Đã gửi lại OTP đến email khách thuê.");

        [HttpPost]
        public async Task<IActionResult> ReportIncident(int id, string type, string description, string? locationText, bool vehicleImmobilized, bool policeInvolved, bool insuranceNotified, decimal estimatedDamage, DateTime occurredAt, List<IFormFile>? evidence)
        {
            var evidenceFileIds = await SaveEvidenceAsync(id, evidence, "incident");
            return await ExecuteAndReturn(HttpMethod.Post, $"api/comprehensive-operations/reservations/{id}/incidents", new
            {
                Type = type,
                Description = description,
                LocationText = locationText,
                EvidenceFileIds = evidenceFileIds,
                VehicleImmobilized = vehicleImmobilized,
                PoliceInvolved = policeInvolved,
                InsuranceNotified = insuranceNotified,
                EstimatedDamage = estimatedDamage,
                OccurredAt = VietnamTime.ToUtc(occurredAt)
            }, id, "Đã gửi báo cáo sự cố. Nhân viên sẽ tiếp nhận theo mức ưu tiên.", evidenceFileIds);
        }

        [HttpPost]
        public async Task<IActionResult> OpenDispute(int id, string type, string description, List<IFormFile>? evidence)
        {
            var evidenceFileIds = await SaveEvidenceAsync(id, evidence, "dispute");
            return await ExecuteAndReturn(HttpMethod.Post, $"api/marketplace-operations/reservations/{id}/disputes", new { Type = type, Description = description, EvidenceFileIds = evidenceFileIds }, id, "Đã mở yêu cầu hỗ trợ/khiếu nại.", evidenceFileIds);
        }

        [HttpPost]
        public async Task<IActionResult> AddDisputeMessage(int id, int disputeId, string message, List<IFormFile>? evidence)
        {
            var evidenceFileIds = await SaveEvidenceAsync(id, evidence, "dispute-message");
            return await ExecuteAndReturn(HttpMethod.Post, $"api/marketplace-operations/disputes/{disputeId}/messages", new { Message = message, EvidenceFileIds = evidenceFileIds }, id, "Đã gửi trao đổi vào hồ sơ tranh chấp.", evidenceFileIds);
        }

        [HttpPost]
        public async Task<IActionResult> AddTrafficFine(int id, DateTime violationAt, string violation, string? locationText, decimal amount, string? noticeNumber, DateTime? dueDate, List<IFormFile>? evidence)
        {
            var evidenceFileIds = await SaveEvidenceAsync(id, evidence, "traffic-fine");
            var evidenceFileId = evidenceFileIds.FirstOrDefault();
            return await ExecuteAndReturn(HttpMethod.Post, $"api/comprehensive-operations/reservations/{id}/traffic-fines", new { ViolationAt = VietnamTime.ToUtc(violationAt), Violation = violation, LocationText = locationText, Amount = amount, NoticeNumber = noticeNumber, EvidenceFileId = evidenceFileId == Guid.Empty ? (Guid?)null : evidenceFileId, DueDate = dueDate }, id, "Đã ghi nhận phạt nguội và thông báo cho khách thuê.", evidenceFileIds);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        public async Task<IActionResult> RespondCharge(int id, int chargeId, bool accept, string? note)
            => await ExecuteAndReturn(HttpMethod.Put, $"api/comprehensive-operations/additional-charges/{chargeId}/respond", new { Accept = accept, Note = note }, id, accept ? "Đã chấp nhận phụ phí." : "Đã từ chối phụ phí và chuyển sang xử lý tranh chấp.");

        [HttpPost]
        public async Task<IActionResult> AddCharge(int id, string chargeType, decimal amount, string reason, List<IFormFile>? evidence)
        {
            var evidenceFileIds = await SaveEvidenceAsync(id, evidence, "charge");
            return await ExecuteAndReturn(HttpMethod.Post, $"api/comprehensive-operations/reservations/{id}/additional-charges", new { ChargeType = chargeType, Amount = amount, Reason = reason, EvidenceFileIds = evidenceFileIds }, id, "Đã gửi đề xuất phụ phí cho khách xác nhận.", evidenceFileIds);
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int id, int paymentId, decimal amount, string paymentType, string transactionCode, string? provider, string? verificationNote)
        {
            if (id <= 0 || paymentId <= 0)
            {
                var invalidMessage = "Không xác định được giao dịch cần đối chiếu. Vui lòng tải lại hàng đợi.";
                if (User.IsInRole("Staff"))
                {
                    TempData["StaffError"] = invalidMessage;
                    return RedirectToAction("Index", "StaffDashboard");
                }
                TempData["OperationError"] = invalidMessage;
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"api/marketplace-operations/reservations/{id}/payments/confirm");
                request.Content = new StringContent(
                    JsonConvert.SerializeObject(new
                    {
                        PaymentID = paymentId,
                        Amount = amount,
                        PaymentType = paymentType,
                        TransactionCode = transactionCode,
                        Provider = provider,
                        VerificationNote = verificationNote
                    }),
                    Encoding.UTF8,
                    "application/json");

                var response = await Client().SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    const string success = "Đã đối chiếu và xác nhận thanh toán. Đơn đã chuyển sang chờ giao xe.";
                    if (User.IsInRole("Staff"))
                    {
                        TempData["StaffSuccess"] = success;
                        return RedirectToAction("Index", "StaffDashboard");
                    }
                    TempData["OperationSuccess"] = success;
                    return RedirectToAction(nameof(Details), new { id });
                }

                var error = Clean(await response.Content.ReadAsStringAsync());
                if (User.IsInRole("Staff"))
                {
                    TempData["StaffError"] = error;
                    return RedirectToAction("Index", "StaffDashboard");
                }
                TempData["OperationError"] = error;
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (HttpRequestException)
            {
                const string error = "Không kết nối được Web API khi xác nhận thanh toán.";
                if (User.IsInRole("Staff"))
                {
                    TempData["StaffError"] = error;
                    return RedirectToAction("Index", "StaffDashboard");
                }
                TempData["OperationError"] = error;
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        public async Task<IActionResult> ReviewPayment(int id, int paymentId, string decision, string note)
            => await ExecuteAndReturn(HttpMethod.Post, $"api/marketplace-operations/reservations/{id}/payments/review", new { PaymentID = paymentId, Decision = decision, Note = note }, id, "Đã lưu kết quả đối chiếu thanh toán.");

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> SimulatePayment(int id)
            => await ExecuteAndReturn(HttpMethod.Post, $"api/marketplace-operations/reservations/{id}/payments/simulate", null, id, "Đã tạo giao dịch giả lập thành công.");

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        public async Task<IActionResult> ResolveIncident(int id, int incidentId, decimal customerLiability, string note, bool openDispute)
            => await ExecuteAndReturn(HttpMethod.Put, $"api/comprehensive-operations/incidents/{incidentId}/resolve", new { CustomerLiability = customerLiability, Note = note, OpenDispute = openDispute }, id, "Đã cập nhật kết quả xử lý sự cố.");

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        public async Task<IActionResult> ResolveDispute(int id, int disputeId, string resolution, decimal compensationAmount)
            => await ExecuteAndReturn(HttpMethod.Put, $"api/marketplace-operations/disputes/{disputeId}/resolve", new { Resolution = resolution, CompensationAmount = compensationAmount }, id, "Đã lưu kết luận tranh chấp.");

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        public async Task<IActionResult> CreateSettlement(int id)
            => await ExecuteAndReturn(HttpMethod.Post, $"api/marketplace-operations/reservations/{id}/settlement", null, id, "Đã tạo đề xuất đối soát.");

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> PaySettlement(int id, int settlementId, string transactionCode)
            => await ExecuteAndReturn(HttpMethod.Put, $"api/marketplace-operations/settlements/{settlementId}/pay", new { TransactionCode = transactionCode }, id, "Đã ghi nhận chi trả cho chủ xe.");

        private async Task<IActionResult> ExecuteAndReturn(HttpMethod method, string url, object? payload, int id, string success, IReadOnlyCollection<Guid>? uploadedFileIds = null)
        {
            try
            {
                using var request = new HttpRequestMessage(method, url);
                if (payload is not null) request.Content = JsonContent(payload);
                var response = await Client().SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    TempData["OperationSuccess"] = success;
                    return RedirectToAction(nameof(Details), new { id });
                }
                await DeleteUploadedFilesAsync(uploadedFileIds);
                TempData["OperationError"] = Clean(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException)
            {
                await DeleteUploadedFilesAsync(uploadedFileIds);
                TempData["OperationError"] = "Không kết nối được Web API.";
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task<List<Guid>> SaveEvidenceAsync(int reservationId, List<IFormFile>? files, string category)
        {
            var saved = new List<Guid>();
            if (files is null || files.Count == 0) return saved;
            if (files.Count > 10) throw new InvalidOperationException("Chỉ được tải tối đa 10 tệp mỗi lần.");
            foreach (var file in files)
            {
                if (file.Length <= 0 || file.Length > 10 * 1024 * 1024) throw new InvalidOperationException("Mỗi tệp phải nhỏ hơn 10 MB.");
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedEvidenceExtensions.Contains(extension)) throw new InvalidOperationException("Chỉ chấp nhận JPG, PNG, WEBP, MP4 hoặc MOV.");
                using var content = new MultipartFormDataContent();
                await using var stream = file.OpenReadStream();
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                content.Add(fileContent, "file", file.FileName);
                content.Add(new StringContent($"reservation-{category}"), "category");
                content.Add(new StringContent($"Reservation:{reservationId}"), "ownerReference");
                var response = await Client().PostAsync("api/secure-files", content);
                if (!response.IsSuccessStatusCode)
                {
                    await DeleteUploadedFilesAsync(saved);
                    throw new InvalidOperationException(Clean(await response.Content.ReadAsStringAsync()));
                }
                var raw = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(raw);
                saved.Add(Guid.Parse(json["fileId"]!.ToString()));
            }
            return saved;
        }

        private async Task DeleteUploadedFilesAsync(IReadOnlyCollection<Guid>? fileIds)
        {
            if (fileIds is null || fileIds.Count == 0) return;
            foreach (var fileId in fileIds.Where(x => x != Guid.Empty).Distinct())
            {
                try { await Client().DeleteAsync($"api/secure-files/{fileId:D}"); }
                catch (HttpRequestException) { }
            }
        }

        private HttpClient Client()
        {
            var client = _httpClientFactory.CreateClient();
            var token = User.FindFirst("carbooktoken")?.Value;
            if (!string.IsNullOrWhiteSpace(token)) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private IActionResult RedirectToRoleDashboard()
        {
            if (User.IsInRole("Admin")) return Redirect("/Admin/AdminDashboard/Index");
            if (User.IsInRole("Staff")) return RedirectToAction("Index", "StaffDashboard");
            if (IsVehiclePartnerAccount()) return RedirectToAction("Dashboard", "VehiclePartner");
            return RedirectToAction(nameof(Index));
        }

        private static List<ResultReservationDto> Filter(List<ResultReservationDto> source, string? group)
        {
            if (string.IsNullOrWhiteSpace(group) || group == "Tất cả") return source;
            return group switch
            {
                "Chờ xử lý" => source.Where(x => x.Status is "Chờ chủ xe xác nhận" or "Chờ thanh toán" or "Chờ khách đặt cọc" or "Chờ khách thanh toán giữ chỗ" or "Chờ nhân viên xác nhận cọc" or "Chờ nhân viên xác nhận thanh toán" or "Đã xác nhận" or "Đã đặt cọc" or "Chờ giao xe").ToList(),
                "Sắp tới" => source.Where(x => x.PickUpDate.Date >= VietnamTime.Today && x.Status is not ("Đã hủy" or "Hoàn thành")).ToList(),
                "Đang thuê" => source.Where(x => x.Status is "Đang thuê" or "Chờ trả xe" or "Chờ đối soát").ToList(),
                "Hoàn thành" => source.Where(x => x.Status == "Hoàn thành").ToList(),
                "Đã hủy" => source.Where(x => x.Status == "Đã hủy").ToList(),
                "Tranh chấp" => source.Where(x => x.Status is "Đang tranh chấp" or "Đang xử lý sự cố").ToList(),
                _ => source
            };
        }

        private bool IsVehiclePartnerAccount() => string.Equals(User.FindFirst("IsVehiclePartner")?.Value, "true", StringComparison.OrdinalIgnoreCase);
        private static string Clean(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Thao tác không thành công.";
            var text = raw.Trim().Trim('"');
            if (!text.StartsWith("{")) return text;
            try { var json = JObject.Parse(raw); return json["message"]?.ToString() ?? json["title"]?.ToString() ?? "Thao tác không thành công."; }
            catch { return "Thao tác không thành công."; }
        }
    }
}
