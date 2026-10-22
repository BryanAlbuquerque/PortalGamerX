namespace EcommerceBase.Domain.Enums;

/// <summary>Estado operacional do pedido. "Paid" só é atingido por confirmação do provedor de pagamento.</summary>
public enum OrderStatus
{
    PendingPayment = 0,
    Paid = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}

/// <summary>Estado do pagamento, espelhando o estado confirmado pelo provedor.</summary>
public enum PaymentStatus
{
    Pending = 0,
    InProcess = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4,
    Refunded = 5,
    ChargedBack = 6
}

public enum DiscountType
{
    Percentage = 0,
    FixedAmount = 1
}

public enum StockMovementType
{
    Entry = 0,
    Exit = 1,
    Adjustment = 2,
    SaleReservation = 3,
    SaleRelease = 4
}

public enum FiscalDocumentStatus
{
    Pending = 0,
    Processing = 1,
    Authorized = 2,
    Rejected = 3,
    Cancelled = 4,
    Error = 5
}

public enum EmailStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2
}

public enum IntegrationKind
{
    Payment = 0,
    Shipping = 1,
    Email = 2,
    Fiscal = 3
}

public enum ImageKind
{
    Product = 0,
    Logo = 1,
    Favicon = 2,
    Banner = 3
}
