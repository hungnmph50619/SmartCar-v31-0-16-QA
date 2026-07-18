using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartCar.Dto.AdminDashboardDtos;
using System.Text;

namespace SmartCar.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/AdminRiskCenter")]
    [Authorize(Roles = "Admin")]
    public class AdminRiskCenterController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public AdminRiskCenterController(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var response = await Client().GetAsync("api/operations/admin/risk-center");
                if (response.IsSuccessStatusCode)
                    return View(JsonConvert.DeserializeObject<AdminRiskCenterDto>(await response.Content.ReadAsStringAsync()) ?? new());
                TempData["RiskError"] = Clean(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException) { TempData["RiskError"] = "Không kết nối được Web API."; }
            return View(new AdminRiskCenterDto());
        }

        [HttpPost("ReviewFraud")]
        public async Task<IActionResult> ReviewFraud(int id, string status, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["RiskError"] = "Phải nhập lý do kết luận cảnh báo.";
                return RedirectToAction(nameof(Index));
            }
            var result = await Send(HttpMethod.Put, $"api/operations/admin/risk-center/fraud/{id}", new UpdateFraudFlagDto { Status = status, Reason = reason });
            TempData[result.Success ? "RiskSuccess" : "RiskError"] = result.Success ? "Đã cập nhật cảnh báo và lưu nhật ký." : result.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Restore")]
        public async Task<IActionResult> Restore(string entityType, int id)
        {
            var url = entityType == "Xe" ? $"api/data-governance/cars/{id}/restore" : $"api/data-governance/users/{id}/restore";
            var result = await Send(HttpMethod.Post, url, null);
            TempData[result.Success ? "RiskSuccess" : "RiskError"] = result.Success ? $"Đã khôi phục {entityType.ToLowerInvariant()}." : result.Message;
            return RedirectToAction(nameof(Index));
        }

        private async Task<(bool Success, string Message)> Send(HttpMethod method, string url, object? payload)
        {
            try
            {
                using var request = new HttpRequestMessage(method, url);
                if (payload is not null) request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await Client().SendAsync(request);
                return (response.IsSuccessStatusCode, Clean(await response.Content.ReadAsStringAsync()));
            }
            catch (HttpRequestException) { return (false, "Không kết nối được Web API."); }
        }

        private HttpClient Client() => _httpClientFactory.CreateClient();
        private static string Clean(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Không thể xử lý yêu cầu.";
            try
            {
                var obj = JsonConvert.DeserializeObject<dynamic>(raw);
                return (string?)obj?.message ?? (string?)obj?.Message ?? raw.Trim().Trim('"');
            }
            catch { return raw.Trim().Trim('"'); }
        }
    }
}
