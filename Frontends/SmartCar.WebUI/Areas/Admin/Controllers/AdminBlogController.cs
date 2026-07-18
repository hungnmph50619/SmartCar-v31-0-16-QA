using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using SmartCar.Dto.BlogDtos;

namespace SmartCar.WebUI.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    [Route("Admin/AdminBlog")]
    public class AdminBlogController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public AdminBlogController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient();
            var responseMessage = await client.GetAsync("api/Blogs/GetAllBlogsWithAuthorList");
            if (responseMessage.IsSuccessStatusCode)
            {
                var jsonData = await responseMessage.Content.ReadAsStringAsync();
                var values = JsonConvert.DeserializeObject<List<ResultAllBlogsWithAuthorDto>>(jsonData);
                return View(values);
            }
            return View();
        }

        [Route("RemoveBlog/{id}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveBlog(int id)
        {
            var client = _httpClientFactory.CreateClient();
            var responseMessage = await client.DeleteAsync("api/Blogs?id=" + id);
            if (responseMessage.IsSuccessStatusCode)
            {
                return RedirectToAction("Index", "AdminBlog", new { area = "Admin" });
            }
            return View();
        }
        //BlogListByBlogID

    }
}
