namespace PortalGamerX.Models.Entities
{
    public class ItemCarrinho
    {
        public Guid Id { get; set; }

        public Guid CarrinhoId { get; set; }

        public Carrinho Carrinho { get; set; } = null!;

        public int JogoId { get; set; }

        public Jogo Jogo { get; set; } = null!;

        public int Quantidade { get; set; }

        public decimal PrecoUnitario { get; set; }
        public decimal ValorTotal { get; set; }
    }
}
