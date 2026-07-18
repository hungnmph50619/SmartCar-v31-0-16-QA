using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartCar.Dto.StaffDtos;
using System.Net.Http.Headers;
using System.Text;

namespace SmartCar.WebUI.Controllers
{
    [Authorize(Roles = "Staff")]
    public class StaffDashboardController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public StaffDashboardController(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

        [HttpGet]
        public async Task<IActionResult> Index(string? scope = null, string? type = null, string? priority = null)
        {
            try
            {
                var response = await Client().GetAsync("api/operations/staff/work-queue");
                if (response.IsSuccessStatusCode)
                {
                    var model = JsonConvert.DeserializeObject<StaffWorkQueueDto>(await response.Content.ReadAsStringAsync()) ?? new();
                    scope = string.IsNullOrWhiteSpace(scope) ? "pending" : scope.Trim().ToLowerInvariant();
                    model.Items = scope switch
                    {
                        "processing" => model.Items.Where(x => x.Bucket == "Đang xử lý").ToList(),
                        "errors" => model.Items.Where(x => x.Bucket == "Quá hạn / lỗi").ToList(),
                        "completed" => model.Items.Where(x => x.Bucket == "Đã hoàn tất").ToList(),
                        "all" => model.Items,
                        _ => model.Items.Where(x => x.Bucket == "Cần xử lý").ToList()
                    };
                    if (!string.IsNullOrWhiteSpace(type))
                    {
                        model.Items = type == "Hồ sơ chủ xe/xe"
                            ? model.Items.Where(x => x.QueueType == "Hồ sơ đối tác" || x.QueueType == "Hồ sơ xe").ToList()
                            : model.Items.Where(x => x.QueueType == type).ToList();
                    }
                    if (!string.IsNullOrWhiteSpace(priority)) model.Items = model.Items.Where(x => x.Priority == priority).ToList();

                    // Các KPI trong phần nội dung phải phản ánh đúng danh sách đang hiển thị.
                    // Trước đây KPI đếm cả công việc Đang xử lý trong khi bảng mặc định chỉ lọc
                    // Cần xử lý, dẫn đến tình trạng bộ đếm là 1 nhưng bảng lại trống.
                    model.PendingCustomerVerifications = model.Items.Count(x => x.QueueType == "Xác minh khách");
                    model.PendingVehicleApplications = model.Items.Count(x => x.QueueType == "Hồ sơ đối tác" || x.QueueType == "Hồ sơ xe");
                    model.PendingVehicleDocuments = model.Items.Count(x => x.QueueType == "Giấy tờ xe");
                    model.PendingPayments = model.Items.Count(x => x.QueueType == "Thanh toán");
                    model.OpenIncidents = model.Items.Count(x => x.QueueType == "Sự cố");
                    model.OpenDisputes = model.Items.Count(x => x.QueueType == "Tranh chấp");
                    model.OverdueTrafficFines = model.Items.Count(x => x.QueueType == "Phạt nguội");
                    model.PendingSettlements = model.Items.Count(x => x.QueueType == "Đối soát");

                    ViewBag.Scope = scope;
                    ViewBag.Type = type;
                    ViewBag.Priority = priority;
                    return View(model);
                }
                ViewBag.Error = Clean(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException) { ViewBag.Error = "Không kết nối được Web API."; }
            return View(new StaffWorkQueueDto());
        }

        [HttpPost]
        public async Task<IActionResult> Claim(string queueType, int entityId, int dueInHours = 4, string? note = null, string? returnUrl = null)
        {
            var result = await SendJson(HttpMethod.Post, "api/operations/staff/work-queue/claim", new { QueueType = queueType, EntityID = entityId, DueInHours = dueInHours, Note = note });
            SetResult(result, "Đã nhận xử lý công việc. Nhân viên khác sẽ thấy tên bạn và hạn xử lý.");
            if (result.Success && !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Release(int id)
        {
            var result = await SendJson(HttpMethod.Put, $"api/operations/staff/work-queue/{id}/release", null);
            SetResult(result, "Đã trả công việc về hàng đợi.");
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> VehicleDocument(int id)
        {
            try
            {
                var response = await Client().GetAsync($"api/operations/staff/vehicle-documents/{id}");
                if (!response.IsSuccessStatusCode)
                {
                    TempData["StaffError"] = Clean(await response.Content.ReadAsStringAsync());
                    return RedirectToAction(nameof(Index));
                }
                var model = JsonConvert.DeserializeObject<StaffVehicleDocumentReviewDto>(await response.Content.ReadAsStringAsync());
                return model is null ? RedirectToAction(nameof(Index)) : View(model);
            }
            catch (HttpRequestException)
            {
                TempData["StaffError"] = "Không kết nối được Web API.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReviewVehicleDocument(int id, string status, string? reason)
        {
            if (status == "Bị từ chối" && string.IsNullOrWhiteSpace(reason))
            {
                TempData["StaffError"] = "Phải nhập lý do từ chối giấy tờ.";
                return RedirectToAction(nameof(VehicleDocument), new { id });
            }
            var result = await SendJson(HttpMethod.Put, $"api/comprehensive-operations/vehicle-documents/{id}/review", new { Status = status, Reason = reason });
            SetResult(result, status == "Đã xác minh" ? "Đã xác minh giấy tờ xe." : "Đã từ chối và lưu lý do để chủ xe bổ sung.");
            return result.Success ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(VehicleDocument), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Verification(int id)
        {
            try
            {
                var response = await Client().GetAsync($"api/operations/staff/verifications/{id}");
                if (!response.IsSuccessStatusCode)
                {
                    TempData["StaffError"] = Clean(await response.Content.ReadAsStringAsync());
                    return RedirectToAction(nameof(Index));
                }
                var model = JsonConvert.DeserializeObject<StaffVerificationReviewDto>(await response.Content.ReadAsStringAsync());
                return model is null ? RedirectToAction(nameof(Index)) : View(model);
            }
            catch (HttpRequestException)
            {
                TempData["StaffError"] = "Không kết nối được Web API.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReviewVerification(int id, string status, string? reason)
        {
            if ((status is "Bị từ chối" or "Yêu cầu bổ sung") && string.IsNullOrWhiteSpace(reason))
            {
                TempData["StaffError"] = "Phải nhập lý do hoặc nội dung cần bổ sung.";
                return RedirectToAction(nameof(Verification), new { id });
            }
            var result = await SendJson(HttpMethod.Put, $"api/marketplace-operations/verifications/{id}/review", new { Status = status, Reason = reason });
            SetResult(result, status == "Đã xác minh" ? "Đã duyệt hồ sơ khách thuê." : "Đã phản hồi hồ sơ cho khách cập nhật.");
            return result.Success ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Verification), new { id });
        }

        private async Task<(bool Success, string Message)> SendJson(HttpMethod method, string url, object? payload)
        {
            try
            {
                using var request = new HttpRequestMessage(method, url);
                if (payload is not null) request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await Client().SendAsync(request);
                return (response.IsSuccessStatusCode, response.IsSuccessStatusCode ? string.Empty : Clean(await response.Content.ReadAsStringAsync()));
            }
            catch (HttpRequestException) { return (false, "Không kết nối được Web API."); }
        }

        private void SetResult((bool Success, string Message) result, string success)
        {
            if (result.Success) TempData["StaffSuccess"] = success;
            else TempData["StaffError"] = result.Message;
        }

        private HttpClient Client()
        {
            var client = _httpClientFactory.CreateClient();
            var token = User.FindFirst("carbooktoken")?.Value;
            if (!string.IsNullOrWhiteSpace(token)) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

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
