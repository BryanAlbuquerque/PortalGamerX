using PortalGamerX.Context;
using PortalGamerX.Models.Entities;

namespace PortalGamerX.Repository.Interfaces
{
    public class CategoriaRepository : ICategoriaRepository
    {
        private AppDbContext  _context;
        public CategoriaRepository(AppDbContext context) 
        {
            _context = context;
        }

        public IEnumerable<Categoria> Categorias => _context.Categorias;
    }
}
