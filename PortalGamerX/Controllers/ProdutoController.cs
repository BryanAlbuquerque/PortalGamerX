using Microsoft.AspNetCore.Mvc;
using PortalGamerX.Repository.Interfaces;

namespace PortalGamerX.Controllers
{
    public class ProdutoController : Controller
    {
        private readonly IProdutoRepository _produtoRepository; 
        public ProdutoController(IProdutoRepository produtoRepository)
        {
            _produtoRepository = produtoRepository;
        }

        public IActionResult List()
        {
            var produtos = _produtoRepository.Produtos;
            return View(produtos);
        }
    }
}
