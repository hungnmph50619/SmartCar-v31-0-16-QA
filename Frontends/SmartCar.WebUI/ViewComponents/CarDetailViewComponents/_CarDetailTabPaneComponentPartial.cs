using Microsoft.AspNetCore.Mvc;

namespace SmartCar.WebUI.ViewComponents.CarDetailViewComponents
{
    public class _CarDetailTabPaneComponentPartial:ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();  
        }
    }
}
