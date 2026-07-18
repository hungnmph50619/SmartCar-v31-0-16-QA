using Microsoft.AspNetCore.Mvc;
namespace SmartCar.WebUI.Controllers
{
    public class PolicyController : Controller
    {
        public IActionResult Index() => RedirectToAction(nameof(OperatingRules));
        public IActionResult OperatingRules() => View();
        public IActionResult Privacy() => View();
        public IActionResult PaymentAndRefund() => View();
        public IActionResult DisputeResolution() => View();
    }
}
