namespace PortalGamerX.Models.Entities
{
    public class HistoricoCompra
    {
        public Guid Id { get; set; }

        public Guid PedidoId { get; set; }

        public Pedido Pedido { get; set; } = null!;

        public string? Descricao { get; set; }

        public string UsuarioId { get; set; } = string.Empty;

        public ApplicationUser Usuario { get; set; } = null!;

        public DateTime DataRegistro { get; set; }
            = DateTime.Now;
    }
}
