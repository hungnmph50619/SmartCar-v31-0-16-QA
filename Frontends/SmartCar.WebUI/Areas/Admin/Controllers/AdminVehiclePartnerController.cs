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
    [Route("Admin/AdminVehiclePartner")]
    public class AdminVehiclePartnerController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public AdminVehiclePartnerController(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

        [HttpGet("Index")]
        public async Task<IActionResult> Index(string? status)
        {
            var model = new AdminVehiclePartnerViewModel();
            try
            {
                var client = CreateAuthorizedClient();
                var profilesResponse = await client.GetAsync("api/VehiclePartnerProfiles");
                if (profilesResponse.IsSuccessStatusCode)
                {
                    model.Profiles = JsonConvert.DeserializeObject<List<VehiclePartnerProfileDto>>(
                                         await profilesResponse.Content.ReadAsStringAsync())
                                     ?? new List<VehiclePartnerProfileDto>();
                }

                var applicationsUrl = "api/VehiclePartnerApplications";
                if (!string.IsNullOrWhiteSpace(status)) applicationsUrl += "?status=" + Uri.EscapeDataString(status);
                var applicationResponse = await client.GetAsync(applicationsUrl);
                if (applicationResponse.IsSuccessStatusCode)
                {
                    model.Applications = JsonConvert.DeserializeObject<List<ResultVehiclePartnerApplicationDto>>(
                                             await applicationResponse.Content.ReadAsStringAsync())
                                         ?? new List<ResultVehiclePartnerApplicationDto>();
                }
                else TempData["MarketplaceError"] = Clean(await applicationResponse.Content.ReadAsStringAsync());

                var vehiclesResponse = await client.GetAsync("api/PartnerVehicles");
                if (vehiclesResponse.IsSuccessStatusCode)
                {
                    model.Vehicles = JsonConvert.DeserializeObject<List<ResultPartnerVehicleDto>>(
                                         await vehiclesResponse.Content.ReadAsStringAsync())
                                     ?? new List<ResultPartnerVehicleDto>();
                }

                var feeResponse = await client.GetAsync("api/PlatformFees");
                if (feeResponse.IsSuccessStatusCode)
                {
                    var fee = JsonConvert.DeserializeObject<PlatformFeeSettingDto>(await feeResponse.Content.ReadAsStringAsync());
                    model.GlobalCommissionRate = fee?.VehiclePartnerCommissionPercent ?? 20m;
                }
            }
            catch (HttpRequestException)
            {
                TempData["MarketplaceError"] = "Không kết nối được Web API tại cổng 7060.";
            }
            ViewBag.SelectedStatus = status;
            return View(model);
        }

        [HttpPost("Review")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(int id, string status, string? adminNote, decimal? approvedDailyPrice, decimal? approvedDepositAmount, decimal? commissionRateOverride)
        {
            try
            {
                var dto = new ReviewVehiclePartnerApplicationDto
                {
                    Status = status,
                    AdminNote = adminNote,
                    ApprovedDailyPrice = approvedDailyPrice,
                    ApprovedDepositAmount = approvedDepositAmount,
                    CommissionRateOverride = commissionRateOverride
                };
                var response = await CreateAuthorizedClient().PutAsync(
                    $"api/VehiclePartnerApplications/{id}/review", JsonContent(dto));
                TempData[response.IsSuccessStatusCode ? "MarketplaceSuccess" : "MarketplaceError"] =
                    Clean(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException)
            {
                TempData["MarketplaceError"] = "Không kết nối được Web API tại cổng 7060.";
            }
            return RedirectToAction(nameof(Index));
        }


        [HttpPost("ReviewProfile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewProfile(int id, string status, string? reviewNote)
        {
            try
            {
                var dto = new ReviewVehiclePartnerProfileDto
                {
                    Status = status,
                    ReviewNote = reviewNote
                };
                var response = await CreateAuthorizedClient().PutAsync(
                    $"api/VehiclePartnerProfiles/{id}/review", JsonContent(dto));
                TempData[response.IsSuccessStatusCode ? "MarketplaceSuccess" : "MarketplaceError"] =
                    Clean(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException)
            {
                TempData["MarketplaceError"] = "Không kết nối được Web API tại cổng 7060.";
            }
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
