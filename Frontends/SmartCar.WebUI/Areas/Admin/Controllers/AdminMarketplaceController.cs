using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartCar.Dto.MarketplaceDtos;
using SmartCar.WebUI.Models;
using System.Net.Http.Headers;
using System.Text;

namespace SmartCar.WebUI.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    [Area("Admin")]
    [Route("Admin/AdminMarketplace")]
    public class AdminMarketplaceController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public AdminMarketplaceController(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

        [HttpGet("Index")]
        public async Task<IActionResult> Index(string? status)
        {
            var model = new AdminMarketplaceViewModel { Status = status };
            try
            {
                var client = CreateAuthorizedClient();
                var settingResponse = await client.GetAsync("api/PlatformFees");
                if (settingResponse.IsSuccessStatusCode)
                {
                    model.Settings = JsonConvert.DeserializeObject<PlatformFeeSettingDto>(await settingResponse.Content.ReadAsStringAsync())
                                     ?? new PlatformFeeSettingDto();
                }

                var url = "api/CommissionTransactions";
                var query = new List<string>();
                if (!string.IsNullOrWhiteSpace(status)) query.Add("status=" + Uri.EscapeDataString(status));
                if (query.Count > 0) url += "?" + string.Join("&", query);
                var txResponse = await client.GetAsync(url);
                if (txResponse.IsSuccessStatusCode)
                {
                    model.Transactions = JsonConvert.DeserializeObject<List<ResultCommissionTransactionDto>>(
                                             await txResponse.Content.ReadAsStringAsync())
                                         ?? new List<ResultCommissionTransactionDto>();
                }
                else TempData["MarketplaceError"] = Clean(await txResponse.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException)
            {
                TempData["MarketplaceError"] = "Không kết nối được Web API tại cổng 7060.";
            }
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("UpdateSettings")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(UpdatePlatformFeeSettingDto dto)
        {
            try
            {
                var response = await CreateAuthorizedClient().PutAsync("api/PlatformFees", JsonContent(dto));
                TempData[response.IsSuccessStatusCode ? "MarketplaceSuccess" : "MarketplaceError"] =
                    response.IsSuccessStatusCode ? "Đã cập nhật mức chiết khấu của nền tảng." : Clean(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException)
            {
                TempData["MarketplaceError"] = "Không kết nối được Web API tại cổng 7060.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("UpdateTransaction")]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateTransaction(int id, string status, string? bankReference, string? note)
        {
            TempData["MarketplaceError"] = "Không thể cập nhật giao dịch đối soát trực tiếp. Hãy mở chi tiết đơn và chi trả qua Settlement đã được lập.";
            return RedirectToAction(nameof(Index));
        }

        private HttpClient CreateAuthorizedClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = User.FindFirst("carbooktoken")?.Value;
            if (!string.IsNullOrWhiteSpace(token)) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
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
