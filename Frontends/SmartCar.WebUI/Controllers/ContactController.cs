using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartCar.Dto.ContactDtos;
using System.Text;

namespace SmartCar.WebUI.Controllers
{
    public class ContactController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ContactController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.v1 = "Liên hệ";
            ViewBag.v2 = "Gửi yêu cầu hỗ trợ";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CreateContactDto createContactDto)
        {
            ViewBag.v1 = "Liên hệ";
            ViewBag.v2 = "Gửi yêu cầu hỗ trợ";

            if (!ModelState.IsValid)
            {
                return View(createContactDto);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                createContactDto.SendDate = DateTime.UtcNow;
                var jsonData = JsonConvert.SerializeObject(createContactDto);
                using var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("api/Contacts", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["ContactSuccess"] = "Tin nhắn của bạn đã được gửi thành công.";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(string.Empty, "Không thể gửi tin nhắn. Vui lòng thử lại.");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "Không kết nối được Web API. Hãy chạy SmartCar.WebApi.");
            }

            return View(createContactDto);
        }
    }
}
