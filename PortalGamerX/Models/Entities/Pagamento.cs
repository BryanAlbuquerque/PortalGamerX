namespace PortalGamerX.Models.Entities
{
    public class Pagamento
    {
        public Guid Id { get; set; }

        public Guid PedidoId { get; set; }

        public int? UsuarioId { get; set; }

        public Cliente? Usuario { get; set; }

        public Pedido Pedido { get; set; } = null!;

        // PAGAMENTOS
        public string MetodoPagamento { get; set; }
            = string.Empty;

        // Pendente, Pago, Cancelado...
        public string StatusPagamento { get; set; }
            = string.Empty;

        public decimal Valor { get; set; }

        public DateTime? DataPagamento { get; set; }

        // PIX
        public string? ChavePix { get; set; }

        public string? QrCodePix { get; set; }

        public string? CopiaEColaPix { get; set; }

        // Cartão
        public string? Ultimos4DigitosCartao { get; set; }

        public string? BandeiraCartao { get; set; }

        public int? Parcelas { get; set; }
    }
}