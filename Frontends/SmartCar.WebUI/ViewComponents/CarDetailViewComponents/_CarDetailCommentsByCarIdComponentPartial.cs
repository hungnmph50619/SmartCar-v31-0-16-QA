using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartCar.Dto.CarFeatureDtos;
using SmartCar.Dto.ReviewDtos;

namespace SmartCar.WebUI.ViewComponents.CarDetailViewComponents
{
	public class _CarDetailCommentsByCarIdComponentPartial:ViewComponent
	{
        private readonly IHttpClientFactory _httpClientFactory;
        public _CarDetailCommentsByCarIdComponentPartial(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IViewComponentResult> InvokeAsync(int id)
        {
            ViewBag.carid = id;
            var client = _httpClientFactory.CreateClient();
            var responseMessage = await client.GetAsync("api/Reviews?id=" + id);
            if (responseMessage.IsSuccessStatusCode)
            {
                var jsonData = await responseMessage.Content.ReadAsStringAsync();
                var values = JsonConvert.DeserializeObject<List<ResultReviewByCarIdDto>>(jsonData);
                return View(values ?? new List<ResultReviewByCarIdDto>());
            }
            return View(new List<ResultReviewByCarIdDto>());
        }
    }
}
