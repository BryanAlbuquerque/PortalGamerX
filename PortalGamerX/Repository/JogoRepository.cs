using PortalGamerX.Repository.Interfaces;
using PortalGamerX.Context;
using PortalGamerX.Models;
using Microsoft.EntityFrameworkCore;

namespace PortalGamerX.Repository
{
    public class JogoRepository : IJogoRepository
    {
        private readonly AppDbContext _context;
        public JogoRepository(AppDbContext context)
        {
            _context = context;
        }
        public IEnumerable<Jogo> Jogos => _context.Jogos.Include(c => c.Categoria);

        public Jogo GetJogosById(int jogoId) => _context.Jogos.FirstOrDefault(j => j.JogoId == jogoId);
    }
}
