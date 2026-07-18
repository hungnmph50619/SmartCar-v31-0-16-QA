using Microsoft.AspNetCore.Mvc;

namespace SmartCar.WebUI.Controllers
{
    public class UILayoutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
