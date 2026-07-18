using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartCar.Dto.BannerDtos;

namespace SmartCar.WebUI.ViewComponents.DefaultViewComponents
{
    public class _DefaultCoverUILayoutComponentPartial : ViewComponent
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public _DefaultCoverUILayoutComponentPartial(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var responseMessage = await client.GetAsync("api/Banners");
            if (responseMessage.IsSuccessStatusCode)
            {
                var jsonData = await responseMessage.Content.ReadAsStringAsync();
                var values = JsonConvert.DeserializeObject<List<ResultBannerDto>>(jsonData);
                return View(values);
            }
            return View();
        }
    }
}
