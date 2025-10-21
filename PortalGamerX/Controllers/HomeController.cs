using Microsoft.AspNetCore.Mvc;

namespace PortalGamerX.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View(); // Abre Views/Home/Index.cshtml
        }

        public IActionResult PortalGamerX()
        {
            return View(); // Abre Views/Home/PortalGamerX.cshtml
        }
    }
}
