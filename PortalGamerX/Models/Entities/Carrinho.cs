namespace PortalGamerX.Models.Entities
{
    public class Carrinho
    {
        public Guid Id { get; set; }

        public string UsuarioId { get; set; } = string.Empty;

        public ApplicationUser Usuario { get; set; } = null!;

        public DateTime DataCriacao { get; set; }
            = DateTime.Now;

        public DateTime DataUltimaAtualizacao { get; set; }
            = DateTime.Now;

        public bool Ativo { get; set; } = true;

        public ICollection<ItemCarrinho> Itens { get; set; }
            = new List<ItemCarrinho>();
    }
}
