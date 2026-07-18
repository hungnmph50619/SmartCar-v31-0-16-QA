using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartCar.Dto.CarPricingDtos;

namespace SmartCar.WebUI.ViewComponents.DashboardComponents
{
    public class _AdminDashboardCarPricingListComponentPartial : ViewComponent
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public _AdminDashboardCarPricingListComponentPartial(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            ViewBag.v1 = "Bảng giá";
            ViewBag.v2 = "Bảng giá thuê xe";
            var client = _httpClientFactory.CreateClient();
            var responseMessage = await client.GetAsync("api/CarPricings/GetCarPricingWithTimePeriodList");
            if (responseMessage.IsSuccessStatusCode)
            {
                var jsonData = await responseMessage.Content.ReadAsStringAsync();
                var values = JsonConvert.DeserializeObject<List<ResultCarPricingListWithModelDto>>(jsonData);
                return View(values);
            }
            return View();
        }
    }
}
