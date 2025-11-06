using Microsoft.AspNetCore.Mvc;

namespace PortalGamerX.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View(); 
        }

        public IActionResult PortalGamerX()
        {
            return View(); 
        }
    }
}
