namespace PortalGamerX.Models.Entities
{
    public class ItemPedido
    {
        public Guid Id { get; set; }

        public Guid PedidoId { get; set; }

        public Pedido Pedido { get; set; } = null!;

        public int JogoId { get; set; }

        public Jogo Jogo { get; set; } = null!;

        public decimal Valor { get; set; }

    }
}
