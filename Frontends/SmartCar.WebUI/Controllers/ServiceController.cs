using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartCar.Dto.ServiceDtos;

namespace SmartCar.WebUI.Controllers
{
    public class ServiceController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.v1 = "Dịch vụ";
            ViewBag.v2 = "Dịch vụ của chúng tôi";
            return View();
        }
    }
}
