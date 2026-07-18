using Microsoft.AspNetCore.Mvc;

namespace SmartCar.WebUI.Controllers
{
    public class SignalRCarController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
