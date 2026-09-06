using Microsoft.AspNetCore.Mvc;

namespace PortalGamerX.Areas.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
