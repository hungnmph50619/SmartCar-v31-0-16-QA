using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartCar.Dto.AdminDashboardDtos;
using System.Net.Http.Headers;

namespace SmartCar.WebUI.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    [Route("Admin/AdminStatistics")]
    public class AdminStatisticsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AdminStatisticsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("Index")]
        public async Task<IActionResult> Index(int? year)
        {
            var selectedYear = year is >= 2000 and <= 2100 ? year.Value : DateTime.UtcNow.Year;
            var model = new AdminDashboardSummaryDto { SelectedYear = selectedYear };

            try
            {
                var response = await CreateAuthorizedClient().GetAsync(
                    $"api/AdminDashboardOverview?year={selectedYear}");

                if (response.IsSuccessStatusCode)
                {
                    model = JsonConvert.DeserializeObject<AdminDashboardSummaryDto>(
                                await response.Content.ReadAsStringAsync())
                            ?? model;
                }
                else
                {
                    ViewBag.StatisticsError = "Không tải được dữ liệu báo cáo. Vui lòng kiểm tra Web API.";
                }
            }
            catch (HttpRequestException)
            {
                ViewBag.StatisticsError = "Không kết nối được Web API tại cổng 7060.";
            }

            ViewBag.AvailableYears = Enumerable.Range(DateTime.UtcNow.Year - 4, 5)
                .Append(selectedYear)
                .Distinct()
                .OrderByDescending(x => x)
                .ToList();
            return View(model);
        }

        private HttpClient CreateAuthorizedClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = User.FindFirst("carbooktoken")?.Value;
            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }
    }
}
