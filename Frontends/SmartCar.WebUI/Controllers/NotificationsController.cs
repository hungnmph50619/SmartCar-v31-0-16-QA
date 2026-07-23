using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartCar.Dto.NotificationDtos;
using SmartCar.Dto.ReservationDtos;
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
                allNotifications.InsertRange(0, await GetActionNotificationsAsync());

                ViewBag.Status = status ?? "Tất cả";
                ViewBag.UnreadCount = allNotifications.Count(x => !x.IsRead);
                ViewBag.TotalCount = allNotifications.Count;

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
        public async Task<IActionResult> Feed()
        {
            try
            {
                var client = Client();
                var response = await client.GetAsync("api/comprehensive-operations/notifications");
                var stored = response.IsSuccessStatusCode
                    ? JsonConvert.DeserializeObject<List<NotificationItemDto>>(await response.Content.ReadAsStringAsync()) ?? new()
                    : new List<NotificationItemDto>();
                var actions = await GetActionNotificationsAsync();
                var items = actions.Concat(stored.Where(x => !x.IsRead))
                    .OrderByDescending(x => x.CreatedDate)
                    .Take(10)
                    .Select(x => new
                    {
                        id = x.NotificationID,
                        key = x.NotificationID < 0 ? $"action:{-x.NotificationID}:{x.Type}" : $"notification:{x.NotificationID}",
                        title = x.Title,
                        message = x.Message,
                        type = x.Type,
                        link = x.Link,
                        isAction = x.NotificationID < 0,
                        createdDate = x.CreatedDate
                    });

                return Json(new { unreadCount = stored.Count(x => !x.IsRead) + actions.Count, items });
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            try
            {
                var response = await Client().GetAsync("api/comprehensive-operations/notifications/unread-count");
                var count = 0;
                if (response.IsSuccessStatusCode)
                {
                    var raw = await response.Content.ReadAsStringAsync();
                    int.TryParse(raw, out count);
                }
                count += (await GetActionNotificationsAsync()).Count;
                return Json(new { unreadCount = count });
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkRead(int id, string? returnUrl)
        {
            if (id > 0) await MarkReadInternal(id);
            return SafeReturn(returnUrl);
        }

        [HttpPost]
        public async Task<IActionResult> Open(int id, string? link)
        {
            var success = id <= 0 || await MarkReadInternal(id);
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
                TempData[response.IsSuccessStatusCode ? "NotificationSuccess" : "NotificationError"] = response.IsSuccessStatusCode
                    ? "Đã đánh dấu tất cả thông báo là đã đọc."
                    : Clean(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException)
            {
                TempData["NotificationError"] = "Không kết nối được Web API.";
            }
            return SafeReturn(returnUrl);
        }

        private async Task<List<NotificationItemDto>> GetActionNotificationsAsync()
        {
            var result = new List<NotificationItemDto>();
            if (User.IsInRole("Customer") && !IsVehiclePartnerAccount())
            {
                var response = await Client().GetAsync("api/Reservations/me");
                if (!response.IsSuccessStatusCode) return result;
                var reservations = JsonConvert.DeserializeObject<List<ResultReservationDto>>(await response.Content.ReadAsStringAsync()) ?? new();
                foreach (var item in reservations.Where(x => x.Status is "Chờ thanh toán" or "Chờ khách thanh toán giữ chỗ"))
                {
                    result.Add(new NotificationItemDto
                    {
                        NotificationID = -item.ReservationID,
                        Title = "Chủ xe đã chấp nhận yêu cầu",
                        Message = $"Đơn #{item.ReservationID} – {item.CarName}: hãy thanh toán {item.TotalPrice:#,0} đồng trước khi hết thời gian giữ xe.",
                        Type = "PaymentAction",
                        Link = $"/ReservationLookup/Details/{item.ReservationID}#paymentSection",
                        CreatedDate = DateTime.UtcNow,
                        IsRead = false
                    });
                }
            }
            else if (IsVehiclePartnerAccount())
            {
                var response = await Client().GetAsync("api/PartnerVehicles/me/dashboard");
                if (!response.IsSuccessStatusCode) return result;
                var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                foreach (var item in json["reservations"]?.Children<JObject>().Where(x => x["status"]?.ToString() == "Chờ chủ xe xác nhận") ?? Enumerable.Empty<JObject>())
                {
                    var id = item["reservationID"]?.Value<int>() ?? 0;
                    if (id <= 0) continue;
                    result.Add(new NotificationItemDto
                    {
                        NotificationID = -id,
                        Title = "Có yêu cầu thuê xe mới",
                        Message = $"Đơn #{id} đang chờ bạn phản hồi. Hãy kiểm tra thời gian, địa điểm và giá thuê.",
                        Type = "OwnerAction",
                        Link = $"/ReservationLookup/Details/{id}",
                        CreatedDate = DateTime.UtcNow,
                        IsRead = false
                    });
                }
            }
            return result;
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
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }

        private HttpClient Client()
        {
            var client = _httpClientFactory.CreateClient();
            var token = User.FindFirst("carbooktoken")?.Value;
            if (!string.IsNullOrWhiteSpace(token)) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private bool IsVehiclePartnerAccount() => string.Equals(User.FindFirst("IsVehiclePartner")?.Value, "true", StringComparison.OrdinalIgnoreCase);

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
            catch { return "Thao tác không thành công."; }
        }
    }
}
