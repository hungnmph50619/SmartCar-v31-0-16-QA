using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartCar.Dto.CommentDtos;

namespace SmartCar.WebUI.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    [Route("Admin/AdminComment")]
    public class AdminCommentController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public AdminCommentController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [Route("Index/{id}")]
        public async Task<IActionResult> Index(int id)
        {
            ViewBag.v = id;
            var client = _httpClientFactory.CreateClient();
            var responseMessage = await client.GetAsync("api/Comments/CommentListByBlog?id=" + id);
            if (responseMessage.IsSuccessStatusCode)
            {
                var jsonData = await responseMessage.Content.ReadAsStringAsync();
                var values = JsonConvert.DeserializeObject<List<ResultCommentDto>>(jsonData);
                return View(values);
            }
            return View(new List<ResultCommentDto>());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("RemoveComment")]
        public async Task<IActionResult> RemoveComment(int id, int blogId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                await client.DeleteAsync($"api/Comments?id={id}");
            }
            catch (HttpRequestException)
            {
                TempData["CommentError"] = "Không kết nối được Web API.";
            }

            return RedirectToAction(nameof(Index), new { id = blogId });
        }

    }
}
