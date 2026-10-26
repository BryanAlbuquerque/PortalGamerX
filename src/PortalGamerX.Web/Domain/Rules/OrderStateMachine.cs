namespace PortalGamerX.Domain.Rules;

/// <summary>
/// Fluxo de pedido de keys digitais: Aguardando pagamento → Pago → Entregue (keys liberadas automaticamente).
/// "Em preparação" e "Enviado" existem só por compatibilidade com pedidos antigos (venda de mídia física).
/// </summary>
public static class OrderStateMachine
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Transitions = new()
    {
        [OrderStatus.PendingPayment] = new[] { OrderStatus.Paid, OrderStatus.Cancelled },
        [OrderStatus.Paid] = new[] { OrderStatus.Delivered, OrderStatus.Cancelled },
        [OrderStatus.Processing] = new[] { OrderStatus.Delivered, OrderStatus.Cancelled },
        [OrderStatus.Shipped] = new[] { OrderStatus.Delivered },
        // Delivered → Cancelled só ocorre pelo fluxo de pagamento (reembolso/chargeback): as keys já entregues são invalidadas.
        [OrderStatus.Delivered] = new[] { OrderStatus.Cancelled },
        [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
    };

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        Transitions.TryGetValue(from, out var targets) && targets.Contains(to);

    /// <summary>
    /// Destinos que um administrador pode escolher manualmente: apenas cancelar um pedido ainda não entregue.
    /// "Pago" e "Entregue" são exclusivos do fluxo automático; pedido entregue só é desfeito por reembolso.
    /// </summary>
    public static IReadOnlyList<OrderStatus> AdminTargets(OrderStatus from) =>
        from is OrderStatus.PendingPayment or OrderStatus.Paid
            ? new List<OrderStatus> { OrderStatus.Cancelled }
            : new List<OrderStatus>();

    public static bool IsFinal(OrderStatus s) => s is OrderStatus.Delivered or OrderStatus.Cancelled;

    /// <summary>Cliente só pode cancelar diretamente enquanto aguarda pagamento.</summary>
    public static bool CustomerCanCancelDirectly(OrderStatus s) => s == OrderStatus.PendingPayment;

    /// <summary>A entrega é imediata após o pagamento; não há janela de "solicitar cancelamento" para keys.</summary>
    public static bool CustomerCanRequestCancellation(OrderStatus s) => false;
}
