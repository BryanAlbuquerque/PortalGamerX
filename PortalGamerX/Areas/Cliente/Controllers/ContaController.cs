using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PortalGamerX.Models.Entities;

namespace PortalGamerX
{
    [Area("Cliente")]
    public class ContaController : Controller
    {
        private readonly UserManager<Cliente> _userManager;
        private readonly SignInManager<Cliente> _signInManager;

        public ContaController(
            UserManager<Cliente> userManager,
            SignInManager<Cliente> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Cadastro()
        {
            return View();
        }

        [HttpGet]
        public IActionResult AcessoNegado()
        {
            return View();
        }
    }
}