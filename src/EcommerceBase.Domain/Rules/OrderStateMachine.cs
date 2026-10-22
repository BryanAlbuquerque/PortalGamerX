using EcommerceBase.Domain.Enums;

namespace EcommerceBase.Domain.Rules;

/// <summary>
/// Regras de transição do pedido. A passagem para "Paid" NUNCA é manual: só o processamento
/// de pagamento confirmado pelo provedor pode fazê-la (<see cref="ApplyPaymentApproved"/>).
/// </summary>
public static class OrderStateMachine
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AdminTransitions = new()
    {
        [OrderStatus.PendingPayment] = new[] { OrderStatus.Cancelled },
        [OrderStatus.Paid] = new[] { OrderStatus.Processing, OrderStatus.Cancelled },
        [OrderStatus.Processing] = new[] { OrderStatus.Shipped, OrderStatus.Cancelled },
        [OrderStatus.Shipped] = new[] { OrderStatus.Delivered },
        [OrderStatus.Delivered] = Array.Empty<OrderStatus>(),
        [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
    };

    public static IReadOnlyList<OrderStatus> AdminAllowedNext(OrderStatus from) =>
        AdminTransitions.TryGetValue(from, out var next) ? next : Array.Empty<OrderStatus>();

    public static bool CanAdminTransition(OrderStatus from, OrderStatus to) =>
        AdminAllowedNext(from).Contains(to);

    /// <summary>Pagamento aprovado só aplica a pedidos aguardando pagamento.</summary>
    public static bool ApplyPaymentApproved(OrderStatus current, out OrderStatus next)
    {
        if (current == OrderStatus.PendingPayment)
        {
            next = OrderStatus.Paid;
            return true;
        }
        next = current;
        return false;
    }

    /// <summary>Cliente pode cancelar sozinho apenas enquanto o pagamento não foi confirmado.</summary>
    public static bool CustomerCanCancelDirectly(OrderStatus status) => status == OrderStatus.PendingPayment;

    /// <summary>Cliente pode solicitar cancelamento (análise do admin) até o pedido ser enviado.</summary>
    public static bool CustomerCanRequestCancellation(OrderStatus status) =>
        status is OrderStatus.Paid or OrderStatus.Processing;

    /// <summary>Estados em que o pedido conta como venda concluída/receita.</summary>
    public static bool IsRevenueStatus(OrderStatus status) =>
        status is OrderStatus.Paid or OrderStatus.Processing or OrderStatus.Shipped or OrderStatus.Delivered;

    public static bool IsTerminal(OrderStatus status) =>
        status is OrderStatus.Delivered or OrderStatus.Cancelled;
}
