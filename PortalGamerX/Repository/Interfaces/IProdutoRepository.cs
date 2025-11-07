using PortalGamerX.Models;

namespace PortalGamerX.Repository.Interfaces
{
    public interface IProdutoRepository
    {
        IEnumerable<Produto> Produtos { get; }

        Produto GetProdutoById(int produto);

    }
}
