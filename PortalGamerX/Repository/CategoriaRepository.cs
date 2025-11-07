using PortalGamerX.Repository.Interfaces;
using PortalGamerX.Models;
using PortalGamerX.Context;

namespace PortalGamerX.Repository
{
    public class CategoriaRepository : ICategoriaRepository
    {
        private readonly AppDbContext _context;
        public CategoriaRepository(AppDbContext context)
        {
            _context = context;
        }
        public IEnumerable<Categoria> Categorias => _context.Categorias;
    }
}
