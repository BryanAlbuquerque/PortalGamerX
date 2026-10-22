namespace EcommerceBase.Web.Domain.Rules;

public static class OrderStateMachine
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Transitions = new()
    {
        [OrderStatus.PendingPayment] = new[] { OrderStatus.Paid, OrderStatus.Cancelled },
        [OrderStatus.Paid] = new[] { OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Cancelled },
        [OrderStatus.Processing] = new[] { OrderStatus.Shipped, OrderStatus.Cancelled },
        [OrderStatus.Shipped] = new[] { OrderStatus.Delivered },
        [OrderStatus.Delivered] = Array.Empty<OrderStatus>(),
        [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
    };

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        Transitions.TryGetValue(from, out var targets) && targets.Contains(to);

    /// <summary>Destinos que um administrador pode escolher manualmente. "Pago" é exclusivo do fluxo de pagamento confirmado.</summary>
    public static IReadOnlyList<OrderStatus> AdminTargets(OrderStatus from) =>
        Transitions.TryGetValue(from, out var targets)
            ? targets.Where(t => t != OrderStatus.Paid).ToList()
            : new List<OrderStatus>();

    public static bool IsFinal(OrderStatus s) => s is OrderStatus.Delivered or OrderStatus.Cancelled;

    /// <summary>Cliente só pode cancelar diretamente enquanto aguarda pagamento.</summary>
    public static bool CustomerCanCancelDirectly(OrderStatus s) => s == OrderStatus.PendingPayment;

    /// <summary>Cliente pode solicitar cancelamento (análise do admin) após o pagamento e antes do envio.</summary>
    public static bool CustomerCanRequestCancellation(OrderStatus s) => s is OrderStatus.Paid or OrderStatus.Processing;
}
