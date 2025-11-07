using PortalGamerX.Models;

namespace PortalGamerX.Repository.Interfaces
{
    public interface ICategoriaRepository
    {
        IEnumerable<Categoria> Categorias { get; }
    }
}
