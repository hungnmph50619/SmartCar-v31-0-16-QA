using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartCar.Dto.BlogDtos;
using SmartCar.WebUI.Helpers;

namespace SmartCar.WebUI.ViewComponents.BlogViewComponents
{
    public class _BlogDetailsMainComponentPartial : ViewComponent
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public _BlogDetailsMainComponentPartial(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IViewComponentResult> InvokeAsync(int id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var responseMessage = await client.GetAsync($"api/Blogs/{id}");
                if (!responseMessage.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage = "Không tải được nội dung bài viết.";
                    return View(new GetBlogById());
                }

                var jsonData = await responseMessage.Content.ReadAsStringAsync();
                var value = JsonConvert.DeserializeObject<GetBlogById>(jsonData);
                if (value is null)
                {
                    ViewBag.ErrorMessage = "Bài viết không tồn tại hoặc dữ liệu không hợp lệ.";
                    return View(new GetBlogById());
                }

                var articleContent = BlogArticleContentBuilder.Build(value.BlogID, value.Title);
                value.Title = articleContent.DisplayTitle;
                value.Description = articleContent.Introduction;
                ViewBag.ArticleSections = articleContent.Sections;
                return View(value);
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage = "Không kết nối được Web API để tải bài viết.";
                return View(new GetBlogById());
            }
            catch (JsonException)
            {
                ViewBag.ErrorMessage = "Dữ liệu bài viết trả về không đúng định dạng.";
                return View(new GetBlogById());
            }
        }
    }
}
