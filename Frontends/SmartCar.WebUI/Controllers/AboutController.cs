using Microsoft.AspNetCore.Mvc;

namespace SmartCar.WebUI.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.v1 = "Giới thiệu";
            ViewBag.v2 = "Về SmartCar";
            return View();
        }
    }
}
