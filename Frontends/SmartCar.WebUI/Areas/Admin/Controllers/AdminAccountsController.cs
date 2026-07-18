using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartCar.Dto.AdminAccountDtos;
using System.Text;

namespace SmartCar.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/AdminAccounts")]
    [Authorize(Roles = "Admin")]
    public class AdminAccountsController : Controller
    {
        private readonly IHttpClientFactory _factory;
        public AdminAccountsController(IHttpClientFactory factory) => _factory = factory;

        [HttpGet("Index")]
        public async Task<IActionResult> Index(string? type, string? search, string? province, string? ward, string? gender, string? verificationStatus, string? accountStatus, int? minAge, int? maxAge)
        {
            var url = "api/admin-accounts?type=" + Uri.EscapeDataString(type ?? "Khách hàng");
            Append(ref url, nameof(search), search);
            Append(ref url, nameof(province), province);
            Append(ref url, nameof(ward), ward);
            Append(ref url, nameof(gender), gender);
            Append(ref url, nameof(verificationStatus), verificationStatus);
            Append(ref url, nameof(accountStatus), accountStatus);
            if (minAge.HasValue) url += "&minAge=" + minAge.Value;
            if (maxAge.HasValue) url += "&maxAge=" + maxAge.Value;
            try
            {
                var response = await _factory.CreateClient().GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    ViewBag.Search = search;
                    ViewBag.Province = province;
                    ViewBag.Ward = ward;
                    ViewBag.Gender = gender;
                    ViewBag.VerificationStatus = verificationStatus;
                    ViewBag.AccountStatus = accountStatus;
                    ViewBag.MinAge = minAge;
                    ViewBag.MaxAge = maxAge;
                    return View(JsonConvert.DeserializeObject<AdminAccountListDto>(await response.Content.ReadAsStringAsync()) ?? new());
                }
                TempData["AccountError"] = Clean(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException) { TempData["AccountError"] = "Không kết nối được Web API."; }
            return View(new AdminAccountListDto { SelectedType = type ?? "Khách hàng" });
        }

        [HttpGet("Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var response = await _factory.CreateClient().GetAsync($"api/admin-accounts/{id}/detail");
                if (response.IsSuccessStatusCode)
                    return View(JsonConvert.DeserializeObject<AdminAccountDetailDto>(await response.Content.ReadAsStringAsync()) ?? new());
                TempData["AccountError"] = Clean(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException) { TempData["AccountError"] = "Không kết nối được Web API."; }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("UpdateStatus")]
        public async Task<IActionResult> UpdateStatus(int id, bool isActive, string reason, string type, string lockMode = "Permanent", int? lockDays = null)
        {
            var dto = new AdminAccountStatusDto { IsActive = isActive, Reason = reason, LockMode = lockMode, LockDays = lockDays };
            var result = await Send(HttpMethod.Put, $"api/admin-accounts/{id}/status", dto);
            TempData[result.Success ? "AccountSuccess" : "AccountError"] = result.Success ? result.Message : result.Message;
            return RedirectToAction(nameof(Index), new { type });
        }

        [HttpPost("RevokeSessions")]
        public async Task<IActionResult> RevokeSessions(int id, string reason, string type)
        {
            var result = await Send(HttpMethod.Post, $"api/admin-accounts/{id}/revoke-sessions", new AdminRevokeSessionDto { Reason = reason });
            TempData[result.Success ? "AccountSuccess" : "AccountError"] = result.Success ? "Đã đăng xuất tài khoản khỏi tất cả thiết bị." : result.Message;
            return RedirectToAction(nameof(Index), new { type });
        }

        [HttpPost("RequestReverification")]
        public async Task<IActionResult> RequestReverification(int id, string reason, bool restrictBooking = true, string severity = "Trung bình")
        {
            var result = await Send(HttpMethod.Post, $"api/admin-accounts/{id}/request-reverification", new AdminRequestReverificationDto { Reason = reason, RestrictBooking = restrictBooking, Severity = severity });
            TempData[result.Success ? "AccountSuccess" : "AccountError"] = result.Success ? result.Message : result.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        private static void Append(ref string url, string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) url += "&" + key + "=" + Uri.EscapeDataString(value);
        }

        private async Task<(bool Success, string Message)> Send(HttpMethod method, string url, object payload)
        {
            try
            {
                using var request = new HttpRequestMessage(method, url) { Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json") };
                var response = await _factory.CreateClient().SendAsync(request);
                return (response.IsSuccessStatusCode, Clean(await response.Content.ReadAsStringAsync()));
            }
            catch (HttpRequestException) { return (false, "Không kết nối được Web API."); }
        }
        private static string Clean(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Không thể xử lý yêu cầu.";
            try { var obj = JsonConvert.DeserializeObject<dynamic>(raw); return (string?)obj?.message ?? (string?)obj?.Message ?? raw.Trim().Trim('"'); }
            catch { return raw.Trim().Trim('"'); }
        }
    }
}
