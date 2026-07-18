using Microsoft.AspNetCore.Mvc;

namespace SmartCar.WebUI.Controllers
{
    public class TestController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
