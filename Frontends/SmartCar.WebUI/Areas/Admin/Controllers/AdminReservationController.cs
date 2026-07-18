using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using SmartCar.Dto.ReservationDtos;
using System.Text;

namespace SmartCar.WebUI.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    [Area("Admin")]
    [Route("Admin/AdminReservation")]
    public class AdminReservationController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AdminReservationController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        [Route("Index")]
        public async Task<IActionResult> Index(string? status)
        {
            var values = new List<ResultReservationDto>();

            try
            {
                var client = CreateAuthorizedClient();
                var response = await client.GetAsync("api/Reservations");
                if (response.IsSuccessStatusCode)
                {
                    values = JsonConvert.DeserializeObject<List<ResultReservationDto>>(
                                 await response.Content.ReadAsStringAsync())
                             ?? new List<ResultReservationDto>();
                }
                else
                {
                    TempData["AdminReservationError"] = await response.Content.ReadAsStringAsync();
                }
            }
            catch (HttpRequestException)
            {
                TempData["AdminReservationError"] = "Không kết nối được Web API tại cổng 7060.";
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                values = values.Where(x => x.Status == status).ToList();
            }

            ViewBag.SelectedStatus = status;
            return View(values);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("UpdateStatus")]
        public async Task<IActionResult> UpdateStatus(int id, string status, string? note)
        {
            try
            {
                var client = CreateAuthorizedClient();
                var dto = new UpdateReservationStatusDto { Status = status, Note = note };
                var content = new StringContent(
                    JsonConvert.SerializeObject(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PutAsync(
                    $"api/Reservations/{id}/status",
                    content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["AdminReservationSuccess"] = "Đã cập nhật trạng thái đơn đặt xe.";
                }
                else
                {
                    TempData["AdminReservationError"] =
                        (await response.Content.ReadAsStringAsync()).Trim().Trim('"');
                }
            }
            catch (HttpRequestException)
            {
                TempData["AdminReservationError"] = "Không kết nối được Web API tại cổng 7060.";
            }

            return RedirectToAction(nameof(Index));
        }
        private HttpClient CreateAuthorizedClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = User.Claims.FirstOrDefault(x => x.Type == "carbooktoken")?.Value;
            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

    }
}
