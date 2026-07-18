using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartCar.Dto.CarPricingDtos;
using SmartCar.Dto.RentACarDtos;

namespace SmartCar.WebUI.Controllers
{
    public class CarController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public CarController(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

        public async Task<IActionResult> Index()
        {
            ViewBag.v1 = "Danh sách xe";
            ViewBag.v2 = "Chọn xe của bạn";
            try
            {
                var response = await _httpClientFactory.CreateClient().GetAsync("api/CarPricings");
                if (response.IsSuccessStatusCode)
                    return View(JsonConvert.DeserializeObject<List<ResultCarPricingWithCarDto>>(await response.Content.ReadAsStringAsync()) ?? new());
                ViewBag.ErrorMessage = "Không tải được danh sách xe.";
            }
            catch (HttpRequestException) { ViewBag.ErrorMessage = "Không kết nối được Web API. Hãy chạy SmartCar.WebApi."; }
            return View(new List<ResultCarPricingWithCarDto>());
        }

        [HttpGet]
        public async Task<IActionResult> CarDetail(int id)
        {
            if (id <= 0) return RedirectToAction(nameof(Index));
            ViewBag.v1 = "Chi tiết xe";
            ViewBag.v2 = "Thông tin, giá và lịch xe";
            try
            {
                var response = await _httpClientFactory.CreateClient().GetAsync($"api/operations/cars/{id}/detail");
                if (response.IsSuccessStatusCode)
                {
                    var model = JsonConvert.DeserializeObject<EnhancedCarDetailDto>(await response.Content.ReadAsStringAsync());
                    if (model is not null) return View(model);
                }
                ViewBag.ErrorMessage = "Không tải được chi tiết xe.";
            }
            catch (HttpRequestException) { ViewBag.ErrorMessage = "Không kết nối được Web API."; }
            return View(new EnhancedCarDetailDto { CarID = id });
        }
    }
}
