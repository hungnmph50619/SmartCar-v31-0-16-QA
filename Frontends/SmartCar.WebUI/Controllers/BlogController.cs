using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartCar.Dto.BlogDtos;
using SmartCar.Dto.CommentDtos;
using SmartCar.WebUI.Helpers;
using System.Text;

namespace SmartCar.WebUI.Controllers
{
    public class BlogController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public BlogController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.v1 = "Bài viết";
            ViewBag.v2 = "Bài viết của chúng tôi";

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync("api/Blogs/GetAllBlogsWithAuthorList");
                if (response.IsSuccessStatusCode)
                {
                    var values = JsonConvert.DeserializeObject<List<ResultAllBlogsWithAuthorDto>>(
                                     await response.Content.ReadAsStringAsync())
                                 ?? new List<ResultAllBlogsWithAuthorDto>();

                    foreach (var item in values)
                    {
                        var article = BlogArticleContentBuilder.Build(item.blogID, item.title);
                        item.title = article.DisplayTitle;
                        item.Description = article.Introduction;
                    }

                    return View(values);
                }

                ViewBag.ErrorMessage = "Không tải được danh sách bài viết.";
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage = "Không kết nối được Web API. Hãy chạy SmartCar.WebApi.";
            }

            return View(new List<ResultAllBlogsWithAuthorDto>());
        }

        public async Task<IActionResult> BlogDetail(int id)
        {
            if (id <= 0)
            {
                return RedirectToAction(nameof(Index));
            }

            ViewBag.v1 = "Bài viết";
            ViewBag.v2 = "Chi tiết bài viết và bình luận";
            ViewBag.blogid = id;
            ViewBag.commentCount = 0;

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"api/Comments/CommentCountByBlog?id={id}");
                if (response.IsSuccessStatusCode)
                {
                    ViewBag.commentCount = await response.Content.ReadAsStringAsync();
                }
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage = "Không tải được dữ liệu bình luận từ Web API.";
            }

            return View();
        }

        [HttpGet]
        public PartialViewResult AddComment(int id)
        {
            ViewBag.blogid = id;
            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(CreateCommentDto createCommentDto)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var jsonData = JsonConvert.SerializeObject(createCommentDto);
                using var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(
                    "api/Comments/CreateCommentWithMediator",
                    content);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(BlogDetail), new { id = createCommentDto.BlogID });
                }

                ModelState.AddModelError(string.Empty, "Không thể gửi bình luận.");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "Không kết nối được Web API.");
            }

            ViewBag.blogid = createCommentDto.BlogID;
            return View(createCommentDto);
        }
    }
}
