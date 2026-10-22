namespace EcommerceBase.Web.Domain;

public enum OrderStatus { PendingPayment, Paid, Processing, Shipped, Delivered, Cancelled }

public enum PaymentStatus { Pending, InProcess, Approved, Rejected, Cancelled, Refunded, ChargedBack }

public enum DiscountType { Percentage, FixedAmount }

public enum StockMovementType { Entry, Exit, Adjustment, OrderReservation, OrderRelease }

public enum EmailStatus { Pending, Sent, Failed }

public enum FiscalStatus { NotRequested, Processing, Authorized, Rejected, Cancelled, Error }

public static class Roles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";
}

public static class Policies
{
    public const string AdminOnly = "AdminOnly";
}

public static class EnumLabels
{
    public static string Label(this OrderStatus s) => s switch
    {
        OrderStatus.PendingPayment => "Aguardando pagamento",
        OrderStatus.Paid => "Pago",
        OrderStatus.Processing => "Em preparação",
        OrderStatus.Shipped => "Enviado",
        OrderStatus.Delivered => "Entregue",
        OrderStatus.Cancelled => "Cancelado",
        _ => s.ToString()
    };

    public static string Label(this PaymentStatus s) => s switch
    {
        PaymentStatus.Pending => "Pendente",
        PaymentStatus.InProcess => "Em análise",
        PaymentStatus.Approved => "Aprovado",
        PaymentStatus.Rejected => "Recusado",
        PaymentStatus.Cancelled => "Cancelado",
        PaymentStatus.Refunded => "Reembolsado",
        PaymentStatus.ChargedBack => "Estornado (chargeback)",
        _ => s.ToString()
    };

    public static string Label(this StockMovementType t) => t switch
    {
        StockMovementType.Entry => "Entrada",
        StockMovementType.Exit => "Saída",
        StockMovementType.Adjustment => "Ajuste",
        StockMovementType.OrderReservation => "Reserva (pedido)",
        StockMovementType.OrderRelease => "Devolução ao estoque",
        _ => t.ToString()
    };

    public static string Label(this FiscalStatus s) => s switch
    {
        FiscalStatus.NotRequested => "Não solicitada",
        FiscalStatus.Processing => "Em processamento",
        FiscalStatus.Authorized => "Autorizada",
        FiscalStatus.Rejected => "Rejeitada",
        FiscalStatus.Cancelled => "Cancelada",
        FiscalStatus.Error => "Erro",
        _ => s.ToString()
    };

    public static string Badge(this OrderStatus s) => s switch
    {
        OrderStatus.PendingPayment => "warning",
        OrderStatus.Paid => "success",
        OrderStatus.Processing => "info",
        OrderStatus.Shipped => "primary",
        OrderStatus.Delivered => "success",
        OrderStatus.Cancelled => "secondary",
        _ => "light"
    };

    public static string Badge(this PaymentStatus s) => s switch
    {
        PaymentStatus.Approved => "success",
        PaymentStatus.Pending or PaymentStatus.InProcess => "warning",
        PaymentStatus.Rejected or PaymentStatus.ChargedBack => "danger",
        _ => "secondary"
    };
}
