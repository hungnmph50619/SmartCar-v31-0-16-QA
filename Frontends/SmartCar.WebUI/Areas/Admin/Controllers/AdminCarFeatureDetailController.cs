using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using SmartCar.Dto.CarFeatureDtos;
using SmartCar.Dto.CategoryDtos;
using SmartCar.Dto.FeatureDtos;

namespace SmartCar.WebUI.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    [Route("Admin/AdminCarFeatureDetail")]
    public class AdminCarFeatureDetailController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public AdminCarFeatureDetailController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [Route("Index/{id}")]
        [HttpGet]
        public async Task<IActionResult> Index(int id)
        {
            var client = _httpClientFactory.CreateClient();
            var responseMessage = await client.GetAsync("api/CarFeatures?id=" + id);
            if (responseMessage.IsSuccessStatusCode)
            {
                var jsonData = await responseMessage.Content.ReadAsStringAsync();
                var values = JsonConvert.DeserializeObject<List<ResultCarFeatureByCarIdDto>>(jsonData);
                return View(values);
            }
            return View();
        }

        [HttpPost]
        [Route("Index/{id}")]
        public async Task<IActionResult> Index(List<ResultCarFeatureByCarIdDto> resultCarFeatureByCarIdDto)
        {

            foreach(var item in  resultCarFeatureByCarIdDto)
            {
                if (item.Available)
                {
                    var client = _httpClientFactory.CreateClient();
                    await client.PutAsync("api/CarFeatures/CarFeatureChangeAvailableToTrue?id=" + item.CarFeatureID, content: null);
                    
                }
                else
                {
                    var client = _httpClientFactory.CreateClient();
                    await client.PutAsync("api/CarFeatures/CarFeatureChangeAvailableToFalse?id=" + item.CarFeatureID, content: null);
                }
            }
            return RedirectToAction("Index", "AdminCar");
        }

        [Route("CreateFeatureByCarId")]
        [HttpGet]
        public async Task<IActionResult> CreateFeatureByCarId()
        {
            var client = _httpClientFactory.CreateClient();
            var responseMessage = await client.GetAsync("api/Features");
            if (responseMessage.IsSuccessStatusCode)
            {
                var jsonData = await responseMessage.Content.ReadAsStringAsync();
                var values = JsonConvert.DeserializeObject<List<ResultFeatureDto>>(jsonData);
                return View(values);
            }
            return View();
        }
    }
}
