using PortalGamerX.Models.Entities;

namespace PortalGamerX.Repository
{
    public interface ICategoriaRepository
    {
        IEnumerable<Categoria> Categorias { get; }
    }
}
