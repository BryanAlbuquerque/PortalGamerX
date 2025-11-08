using Microsoft.AspNetCore.Mvc;
using PortalGamerX.Repository.Interfaces;

namespace PortalGamerX.Controllers
{
    public class JogoController : Controller
    {
        private readonly IJogoRepository _jogoRepository;

        public JogoController(IJogoRepository jogoRepository)
        {
            _jogoRepository = jogoRepository;
        }

        public IActionResult List()
        {
            var jogos = _jogoRepository.Jogos;
            return View(jogos);
        }
    }
}
