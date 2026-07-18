using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartCar.Dto.NotificationDtos;
using System.Net.Http.Headers;
using System.Text;

namespace SmartCar.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [Route("Admin/AdminAnnouncement")]
    public class AdminAnnouncementController : Controller
    {
        private readonly IHttpClientFactory _factory;
        public AdminAnnouncementController(IHttpClientFactory factory) => _factory = factory;

        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var model = new List<ResultCompanyAnnouncementDto>();
            try
            {
                var response = await Client().GetAsync("api/CompanyAnnouncements");
                if (response.IsSuccessStatusCode) model = JsonConvert.DeserializeObject<List<ResultCompanyAnnouncementDto>>(await response.Content.ReadAsStringAsync()) ?? model;
                else TempData["AnnouncementError"] = Clean(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException) { TempData["AnnouncementError"] = "Không kết nối được Web API tại cổng 7060."; }
            return View(model);
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCompanyAnnouncementDto dto)
        {
            try
            {
                var response = await Client().PostAsync("api/CompanyAnnouncements", new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json"));
                TempData[response.IsSuccessStatusCode ? "AnnouncementSuccess" : "AnnouncementError"] = Clean(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException) { TempData["AnnouncementError"] = "Không kết nối được Web API tại cổng 7060."; }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var response = await Client().DeleteAsync($"api/CompanyAnnouncements/{id}");
                TempData[response.IsSuccessStatusCode ? "AnnouncementSuccess" : "AnnouncementError"] = Clean(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException) { TempData["AnnouncementError"] = "Không kết nối được Web API tại cổng 7060."; }
            return RedirectToAction(nameof(Index));
        }

        private HttpClient Client(){var c=_factory.CreateClient();var t=User.FindFirst("carbooktoken")?.Value;if(!string.IsNullOrWhiteSpace(t))c.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",t);return c;}
        private static string Clean(string raw)=>string.IsNullOrWhiteSpace(raw)?"Không thể xử lý yêu cầu.":raw.Trim().Trim('"');
    }
}
