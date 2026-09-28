namespace PortalGamerX.Models.Entities
{
    public class Pedido
    {
        public Guid Id { get; set; }

        public Cliente Usuario { get; set; } = null!;

        public string NumeroPedido { get; set; } = string.Empty;

        public DateTime DataPedido { get; set; }
            = DateTime.Now;


        // Valores
        public string FormaPagamento { get; set; }
            = string.Empty;

        public decimal ValorProdutos { get; set; }

        public decimal ValorDesconto { get; set; }

        public decimal ValorTotal { get; set; }


        // Dados fiscais do cliente
        public string CPFCliente { get; set; }
            = string.Empty;

        public string NomeCliente { get; set; }
            = string.Empty;

        public string EmailCliente { get; set; }
            = string.Empty;


        // SOLICITAÇÃO DE REEMBOLSO / REMOÇÃO JOGO DA CONTA DO USUÁRIO
        public bool ReembolsoSolicitado { get; set; }

        public DateTime? DataSolicitacaoReembolso { get; set; }

        public string? MotivoReembolso { get; set; }


        // RELACIONAMENTOS
        public ICollection<ItemPedido> Itens { get; set; }
            = new List<ItemPedido>();

        public ICollection<Pagamento> Pagamentos { get; set; }
            = new List<Pagamento>();

        public ICollection<HistoricoCompra> HistoricosCompras { get; set; }
            = new List<HistoricoCompra>();
    }
}
