using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SmartCar.WebUI.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class PaymentResolutionController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public PaymentResolutionController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(int id, int paymentId, string decision, string note)
        {
            var normalizedDecision = (decision ?? string.Empty).Trim();
            var normalizedNote = (note ?? string.Empty).Trim();
            if (id <= 0 || paymentId <= 0)
            {
                TempData["OperationError"] = "Không xác định được giao dịch cần xử lý.";
                return RedirectToAction("Details", "ReservationLookup", new { id });
            }
            if (normalizedNote.Length < 10 || normalizedNote.Length > 500)
            {
                TempData["OperationError"] = "Lý do hoặc hướng dẫn khách phải từ 10 đến 500 ký tự.";
                return RedirectToAction("Details", "ReservationLookup", new { id });
            }

            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"api/payment-resolution/reservations/{id}/review");
                request.Content = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        PaymentID = paymentId,
                        Decision = normalizedDecision,
                        Note = normalizedNote
                    }),
                    Encoding.UTF8,
                    "application/json");

                var response = await Client().SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    TempData["OperationSuccess"] = normalizedDecision is "Bị từ chối" or "Giao dịch không hợp lệ" or "Thanh toán muộn"
                        ? "Đã từ chối xác nhận thanh toán và giải phóng lịch xe."
                        : "Đã yêu cầu khách xử lý lại. Lịch xe được giữ thêm tối đa 15 phút.";
                }
                else
                {
                    TempData["OperationError"] = Clean(await response.Content.ReadAsStringAsync());
                }
            }
            catch (HttpRequestException)
            {
                TempData["OperationError"] = "Không kết nối được Web API khi xử lý thanh toán.";
            }

            return RedirectToAction("Details", "ReservationLookup", new { id, paymentSection = true }, "paymentSection");
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
