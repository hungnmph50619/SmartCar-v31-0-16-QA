using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartCar.Dto.MarketplaceDtos;
using System.Net.Http.Headers;

namespace SmartCar.WebUI.Controllers
{
    [Authorize(Roles = "VehiclePartner")]
    public class PartnerVehicleDetailsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public PartnerVehicleDetailsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("VehiclePartner/VehicleDetails/{id:int}")]
        public async Task<IActionResult> Index(int id)
        {
            if (id <= 0) return Redirect("/VehiclePartner/Dashboard#vehicles");

            try
            {
                var client = _httpClientFactory.CreateClient();
                var token = User.FindFirst("carbooktoken")?.Value;
                if (!string.IsNullOrWhiteSpace(token))
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync("api/PartnerVehicles/me/dashboard");
                if (!response.IsSuccessStatusCode)
                {
                    TempData["VehiclePartnerError"] = "Không tải được thông tin xe.";
                    return Redirect("/VehiclePartner/Dashboard#vehicles");
                }

                var dashboard = JsonConvert.DeserializeObject<PartnerVehicleDashboardDto>(await response.Content.ReadAsStringAsync())
                                ?? new PartnerVehicleDashboardDto();
                var vehicle = dashboard.Vehicles.FirstOrDefault(x => x.PartnerVehicleID == id);
                if (vehicle is null)
                {
                    TempData["VehiclePartnerError"] = "Không tìm thấy xe thuộc tài khoản của bạn.";
                    return Redirect("/VehiclePartner/Dashboard#vehicles");
                }

                var application = dashboard.Applications
                    .Where(x => x.ApprovedCarID == vehicle.CarID)
                    .OrderByDescending(x => x.CreatedDate)
                    .FirstOrDefault();
                if (application is null)
                {
                    TempData["VehiclePartnerError"] = "Không tìm thấy hồ sơ chi tiết của xe.";
                    return Redirect("/VehiclePartner/Dashboard#vehicles");
                }

                ViewBag.PartnerVehicleID = vehicle.PartnerVehicleID;
                ViewBag.DailyPrice = vehicle.DailyPrice;
                ViewBag.DepositAmount = vehicle.DepositAmount;
                ViewBag.OperationalStatus = vehicle.OperationalStatus;
                return View(application);
            }
            catch (HttpRequestException)
            {
                TempData["VehiclePartnerError"] = "Không kết nối được Web API.";
                return Redirect("/VehiclePartner/Dashboard#vehicles");
            }
        }
    }
}