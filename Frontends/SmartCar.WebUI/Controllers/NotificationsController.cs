using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartCar.Dto.NotificationDtos;
using System.Net.Http.Headers;

namespace SmartCar.WebUI.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public NotificationsController(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

        [HttpGet]
        public async Task<IActionResult> Index(string? status = null)
        {
            try
            {
                var response = await Client().GetAsync("api/comprehensive-operations/notifications");
                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = Clean(await response.Content.ReadAsStringAsync());
                    return View(new List<NotificationItemDto>());
                }

                var allNotifications = JsonConvert.DeserializeObject<List<NotificationItemDto>>(
                    await response.Content.ReadAsStringAsync()) ?? new();

                ViewBag.Status = status ?? "Tất cả";
                ViewBag.UnreadCount = allNotifications.Count(x => !x.IsRead);
                ViewBag.TotalCount = allNotifications.Count;

                try
                {
                    var countResponse = await Client().GetAsync("api/comprehensive-operations/notifications/unread-count");
                    if (countResponse.IsSuccessStatusCode)
                    {
                        var rawCount = await countResponse.Content.ReadAsStringAsync();
                        if (int.TryParse(rawCount, out var exactUnreadCount))
                            ViewBag.UnreadCount = exactUnreadCount;
                    }
                }
                catch (HttpRequestException)
                {
                    // Danh sách vẫn hiển thị được; dùng số đếm từ dữ liệu đã tải làm dự phòng.
                }

                var model = allNotifications;
                if (status == "Chưa đọc") model = model.Where(x => !x.IsRead).ToList();
                if (status == "Đã đọc") model = model.Where(x => x.IsRead).ToList();
                return View(model);
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Không kết nối được Web API.";
                return View(new List<NotificationItemDto>());
            }
        }


        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            try
            {
                var response = await Client().GetAsync("api/comprehensive-operations/notifications/unread-count");
                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode);

                var raw = await response.Content.ReadAsStringAsync();
                return Json(new { unreadCount = int.TryParse(raw, out var count) ? count : 0 });
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkRead(int id, string? returnUrl)
        {
            await MarkReadInternal(id);
            return SafeReturn(returnUrl);
        }

        [HttpPost]
        public async Task<IActionResult> Open(int id, string? link)
        {
            var success = await MarkReadInternal(id);
            if (success && !string.IsNullOrWhiteSpace(link) && Url.IsLocalUrl(link))
                return Redirect(link);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllRead(string? returnUrl)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Put, "api/comprehensive-operations/notifications/read-all");
                var response = await Client().SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    TempData["NotificationError"] = Clean(await response.Content.ReadAsStringAsync());
                else
                    TempData["NotificationSuccess"] = "Đã đánh dấu tất cả thông báo là đã đọc.";
            }
            catch (HttpRequestException)
            {
                TempData["NotificationError"] = "Không kết nối được Web API.";
            }

            return SafeReturn(returnUrl);
        }

        private async Task<bool> MarkReadInternal(int id)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Put, $"api/comprehensive-operations/notifications/{id}/read");
                var response = await Client().SendAsync(request);
                if (response.IsSuccessStatusCode) return true;

                TempData["NotificationError"] = Clean(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException)
            {
                TempData["NotificationError"] = "Không kết nối được Web API.";
            }

            return false;
        }

        private IActionResult SafeReturn(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        private HttpClient Client()
        {
            var client = _httpClientFactory.CreateClient();
            var token = User.FindFirst("carbooktoken")?.Value;
            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static string Clean(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Thao tác không thành công.";
            var text = raw.Trim().Trim('"');
            if (!text.StartsWith("{")) return text;
            try
            {
                var json = JObject.Parse(raw);
                return json["message"]?.ToString() ?? json["title"]?.ToString() ?? "Thao tác không thành công.";
            }
            catch
            {
                return "Thao tác không thành công.";
            }
        }
    }
}
