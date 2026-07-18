using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartCar.Dto.LocationDtos;
using System.Net.Http.Headers;
using System.Text;

namespace SmartCar.WebUI.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    [Route("Admin/AdminLocation")]
    public class AdminLocationController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public AdminLocationController(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            var client = CreateAuthorizedClient();
            var responseMessage = await client.GetAsync("api/Locations");
            if (responseMessage.IsSuccessStatusCode)
            {
                var jsonData = await responseMessage.Content.ReadAsStringAsync();
                return View(JsonConvert.DeserializeObject<List<ResultLocationDto>>(jsonData) ?? new List<ResultLocationDto>());
            }
            TempData["LocationError"] = "Không tải được danh sách địa điểm từ Web API.";
            return View(new List<ResultLocationDto>());
        }

        [HttpGet]
        [Route("CreateLocation")]
        public IActionResult CreateLocation() => View(new CreateLocationDto());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("CreateLocation")]
        public async Task<IActionResult> CreateLocation(CreateLocationDto model)
        {
            ValidateCoordinates(model.Latitude, model.Longitude);
            if (!ModelState.IsValid) return View(model);

            var jsonData = JsonConvert.SerializeObject(model);
            using var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            var response = await CreateAuthorizedClient().PostAsync("api/Locations", content);
            if (response.IsSuccessStatusCode)
            {
                TempData["LocationSuccess"] = "Đã thêm điểm giao nhận và tọa độ bản đồ.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, await ReadErrorAsync(response));
            return View(model);
        }

        [Route("RemoveLocation/{id}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLocation(int id)
        {
            var response = await CreateAuthorizedClient().DeleteAsync("api/Locations?id=" + id);
            TempData[response.IsSuccessStatusCode ? "LocationSuccess" : "LocationError"] = response.IsSuccessStatusCode
                ? "Đã xóa địa điểm."
                : await ReadErrorAsync(response);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Route("UpdateLocation/{id}")]
        public async Task<IActionResult> UpdateLocation(int id)
        {
            var response = await CreateAuthorizedClient().GetAsync($"api/Locations/{id}");
            if (response.IsSuccessStatusCode)
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                return View(JsonConvert.DeserializeObject<UpdateLocationDto>(jsonData));
            }
            TempData["LocationError"] = "Không tìm thấy địa điểm cần cập nhật.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("UpdateLocation/{id}")]
        public async Task<IActionResult> UpdateLocation(int id, UpdateLocationDto model)
        {
            model.LocationID = id;
            ValidateCoordinates(model.Latitude, model.Longitude);
            if (!ModelState.IsValid) return View(model);

            var jsonData = JsonConvert.SerializeObject(model);
            using var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            var response = await CreateAuthorizedClient().PutAsync("api/Locations", content);
            if (response.IsSuccessStatusCode)
            {
                TempData["LocationSuccess"] = "Đã cập nhật địa điểm và bản đồ.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, await ReadErrorAsync(response));
            return View(model);
        }

        private HttpClient CreateAuthorizedClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = User.Claims.FirstOrDefault(x => x.Type == "carbooktoken")?.Value;
            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private void ValidateCoordinates(decimal? latitude, decimal? longitude)
        {
            if (latitude.HasValue != longitude.HasValue)
                ModelState.AddModelError(string.Empty, "Phải nhập đồng thời cả vĩ độ và kinh độ, hoặc để trống cả hai.");
        }

        private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
        {
            var text = (await response.Content.ReadAsStringAsync()).Trim().Trim('"');
            return string.IsNullOrWhiteSpace(text) ? "Web API không xử lý được yêu cầu địa điểm." : text;
        }
    }
}
