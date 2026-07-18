using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartCar.Dto.AboutDtos;

namespace SmartCar.WebUI.ViewComponents.AboutViewComponents
{
    public class _AboutUsComponentPartial : ViewComponent
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public _AboutUsComponentPartial(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var responseMessage = await client.GetAsync("api/Abouts");
            if (responseMessage.IsSuccessStatusCode)
            {
                var jsonData= await responseMessage.Content.ReadAsStringAsync();
                var values=JsonConvert.DeserializeObject<List<ResultAboutDto>>(jsonData);
                return View(values);
            }
            return View();
        }
    }
}
