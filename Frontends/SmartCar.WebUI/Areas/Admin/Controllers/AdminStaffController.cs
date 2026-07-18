using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartCar.Dto.StaffDtos;
using System.Net.Http.Headers;
using System.Text;

namespace SmartCar.WebUI.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    [Route("Admin/AdminStaff")]
    public class AdminStaffController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AdminStaffController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var values = new List<ResultStaffDto>();
            try
            {
                var response = await CreateAuthorizedClient().GetAsync("api/StaffAccounts");
                if (response.IsSuccessStatusCode)
                    values = JsonConvert.DeserializeObject<List<ResultStaffDto>>(await response.Content.ReadAsStringAsync()) ?? new();
                else
                    TempData["StaffError"] = Clean(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException)
            {
                TempData["StaffError"] = "Không kết nối được Web API tại cổng 7060.";
            }
            return View(values);
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateStaffDto dto)
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");
                var response = await CreateAuthorizedClient().PostAsync("api/StaffAccounts", content);
                TempData[response.IsSuccessStatusCode ? "StaffSuccess" : "StaffError"] = Clean(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException)
            {
                TempData["StaffError"] = "Không kết nối được Web API tại cổng 7060.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("ResetPassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id, string newPassword)
        {
            try
            {
                var dto = new ResetStaffPasswordDto { NewPassword = newPassword };
                var content = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");
                var response = await CreateAuthorizedClient().PutAsync($"api/StaffAccounts/{id}/reset-password", content);
                TempData[response.IsSuccessStatusCode ? "StaffSuccess" : "StaffError"] = Clean(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException)
            {
                TempData["StaffError"] = "Không kết nối được Web API tại cổng 7060.";
            }
            return RedirectToAction(nameof(Index));
        }

        private HttpClient CreateAuthorizedClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = User.Claims.FirstOrDefault(x => x.Type == "carbooktoken")?.Value;
            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static string Clean(string raw) => string.IsNullOrWhiteSpace(raw) ? "Không thể xử lý yêu cầu." : raw.Trim().Trim('"');
    }
}
