using PortalGamerX.Enums;

namespace PortalGamerX.Models.Entities
{
    public class HistoricoCompra
    {
        public Guid Id { get; set; }

        public Guid PedidoId { get; set; }

        public Pedido Pedido { get; set; } = null!;

        public StatusPedido StatusPedido { get; set; }

        public string Acao { get; set; } = string.Empty;

        public string? Descricao { get; set; }

        public int? UsuarioId { get; set; }

        public Cliente? Usuario { get; set; }

        public DateTime DataRegistro { get; set; }
            = DateTime.Now;
    }
}
