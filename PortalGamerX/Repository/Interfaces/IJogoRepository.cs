using PortalGamerX.Models;

namespace PortalGamerX.Repository.Interfaces
{
    public interface IJogoRepository
    {
        IEnumerable<Jogo> Jogos { get; }
        Jogo GetJogosById(int jogoId);
    }
}
