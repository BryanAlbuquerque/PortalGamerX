using PortalGamerX.Repository.Interfaces;
using PortalGamerX.Context;
using PortalGamerX.Models;

namespace PortalGamerX.Repository
{
    public class ProdutoRepository : IProdutoRepository
    {
        private readonly AppDbContext _context;

        public ProdutoRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Produto> Produtos => _context.Produtos;

        public Produto GetProdutoById(int produtoId) => _context.Produtos.FirstOrDefault(p => p.ProdutoId == produtoId);
    }
}
