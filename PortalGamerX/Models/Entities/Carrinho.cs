namespace PortalGamerX.Models.Entities
{
    public class Carrinho
    {
        public Guid Id { get; set; }

        public int? UsuarioId { get; set; }

        public Cliente Usuario { get; set; } = null!;

        public DateTime DataCriacao { get; set; }
            = DateTime.Now;

        public DateTime DataUltimaAtualizacao { get; set; }
            = DateTime.Now;

        public bool Ativo { get; set; } = true;

        public ICollection<ItemCarrinho> Itens { get; set; }
            = new List<ItemCarrinho>();
    }
}
