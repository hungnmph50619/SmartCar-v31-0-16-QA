using Microsoft.AspNetCore.Mvc;
using SmartCar.WebUI.Models;
using System.Net.Http.Json;

namespace SmartCar.WebUI.ViewComponents.Shared
{
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public NotificationBellViewComponent(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IViewComponentResult> InvokeAsync(string theme = "public")
        {
            var model = new NotificationBellViewModel
            {
                Theme = string.Equals(theme, "admin", StringComparison.OrdinalIgnoreCase) ? "admin" : "public"
            };

            if (HttpContext.User.Identity?.IsAuthenticated != true)
                return View(model);

            try
            {
                var client = _httpClientFactory.CreateClient();
                using var response = await client.GetAsync("api/comprehensive-operations/notifications/unread-count");
                if (response.IsSuccessStatusCode)
                    model.UnreadCount = await response.Content.ReadFromJsonAsync<int>();
            }
            catch (HttpRequestException)
            {
                // Không làm hỏng thanh điều hướng nếu Web API tạm thời không phản hồi.
            }
            catch (TaskCanceledException)
            {
                // Giữ thanh điều hướng hoạt động khi yêu cầu đếm thông báo hết thời gian.
            }

            return View(model);
        }
    }
}
