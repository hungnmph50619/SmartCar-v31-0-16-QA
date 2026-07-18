using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartCar.Dto.BlogDtos;
using SmartCar.WebUI.Helpers;
using SmartCar.Dto.TestimonialDtos;

namespace SmartCar.WebUI.ViewComponents.BlogViewComponents
{
    public class _GetLast3BlogsWithAuthorListComponentPartial : ViewComponent
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public _GetLast3BlogsWithAuthorListComponentPartial(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var responseMessage = await client.GetAsync("api/Blogs/GetLast3BlogsWitAuthorsList");
                if (!responseMessage.IsSuccessStatusCode)
                {
                    return View(new List<ResultLast3BlogsWithAuthors>());
                }

                var jsonData = await responseMessage.Content.ReadAsStringAsync();
                var values = JsonConvert.DeserializeObject<List<ResultLast3BlogsWithAuthors>>(jsonData)
                             ?? new List<ResultLast3BlogsWithAuthors>();
                foreach (var item in values)
                {
                    item.Title = BlogArticleContentBuilder.Build(item.BlogID, item.Title).DisplayTitle;
                }

                return View(values);
            }
            catch (HttpRequestException)
            {
                return View(new List<ResultLast3BlogsWithAuthors>());
            }
            catch (JsonException)
            {
                return View(new List<ResultLast3BlogsWithAuthors>());
            }
        }
    }
}
