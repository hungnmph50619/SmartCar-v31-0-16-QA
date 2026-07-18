using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartCar.Dto.CarPricingDtos;

namespace SmartCar.WebUI.Controllers
{
    public class CarPricingController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CarPricingController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.v1 = "Bảng giá";
            ViewBag.v2 = "Bảng giá thuê xe";

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(
                    "api/CarPricings/GetCarPricingWithTimePeriodList");

                if (response.IsSuccessStatusCode)
                {
                    var values = JsonConvert.DeserializeObject<List<ResultCarPricingListWithModelDto>>(
                                     await response.Content.ReadAsStringAsync())
                                 ?? new List<ResultCarPricingListWithModelDto>();
                    return View(values);
                }

                ViewBag.ErrorMessage = $"Không tải được bảng giá từ Web API (HTTP {(int)response.StatusCode}).";
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage = "Không kết nối được Web API. Hãy chạy SmartCar.WebApi tại cổng 7060.";
            }
            catch (TaskCanceledException)
            {
                ViewBag.ErrorMessage = "Web API phản hồi quá chậm hoặc đã hết thời gian chờ.";
            }

            return View(new List<ResultCarPricingListWithModelDto>());
        }
    }
}
