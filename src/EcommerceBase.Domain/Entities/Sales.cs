using EcommerceBase.Domain.Enums;

namespace EcommerceBase.Domain.Entities;

public class Address
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Label { get; set; } = "Casa";
    public string RecipientName { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string? Complement { get; set; }
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = "BR";
    public bool IsDefault { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class Cart
{
    public int Id { get; set; }
    /// <summary>Dono autenticado (quando houver).</summary>
    public Guid? UserId { get; set; }
    /// <summary>Token do cookie para carrinhos de visitantes.</summary>
    public string? AnonymousToken { get; set; }
    public string? CouponCode { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}

public class CartItem
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public Cart? Cart { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public DateTime AddedAtUtc { get; set; }
}

public class Coupon
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal Value { get; set; }
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public int? MaxUses { get; set; }
    public int? MaxUsesPerCustomer { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; } = true;
    public int? CategoryId { get; set; }
    public int? ProductId { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<CouponUsage> Usages { get; set; } = new List<CouponUsage>();
}

public class CouponUsage
{
    public int Id { get; set; }
    public int CouponId { get; set; }
    public Coupon? Coupon { get; set; }
    public Guid UserId { get; set; }
    public int OrderId { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTime UsedAtUtc { get; set; }
}

public class Order
{
    public int Id { get; set; }
    /// <summary>Número público do pedido (não sequencial, não adivinhável).</summary>
    public string Number { get; set; } = string.Empty;
    public Guid UserId { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal ShippingTotal { get; set; }
    public decimal Total { get; set; }

    public int? CouponId { get; set; }
    public string? CouponCode { get; set; }

    public string ShippingProvider { get; set; } = string.Empty;
    public string ShippingOptionCode { get; set; } = string.Empty;
    public string ShippingOptionName { get; set; } = string.Empty;
    public int? ShippingDeadlineDays { get; set; }

    // Snapshot dos dados do cliente e do endereço no momento da compra
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string CustomerDocument { get; set; } = string.Empty;

    public string RecipientName { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string AddressNumber { get; set; } = string.Empty;
    public string? Complement { get; set; }
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;

    public string? CustomerNotes { get; set; }
    public string? AdminNotes { get; set; }
    public string? TrackingCode { get; set; }

    /// <summary>Chave de idempotência enviada pelo formulário de checkout (única por cliente).</summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    public bool CancellationRequested { get; set; }
    public string? CancellationRequestReason { get; set; }
    public string? CancellationReason { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public DateTime? ShippedAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<OrderStatusHistory> History { get; set; } = new List<OrderStatusHistory>();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    // Snapshot: o histórico não muda se o produto for editado depois
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }

    public string? Ncm { get; set; }
    public string? Cest { get; set; }
    public string CommercialUnit { get; set; } = "UN";
    public int OriginCode { get; set; }
}

public class OrderStatusHistory
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public OrderStatus? FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public string? Note { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public string Provider { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public decimal Amount { get; set; }
    public decimal RefundedAmount { get; set; }
    /// <summary>Referência enviada ao provedor (número do pedido).</summary>
    public string ExternalReference { get; set; } = string.Empty;
    public string? ExternalPreferenceId { get; set; }
    public string? ExternalPaymentId { get; set; }
    public string? CheckoutUrl { get; set; }
    public string? PaymentMethod { get; set; }
    public string? StatusDetail { get; set; }
    public string? FailureReason { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }

    public ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
}

public class PaymentTransaction
{
    public long Id { get; set; }
    public int PaymentId { get; set; }
    public Payment? Payment { get; set; }
    /// <summary>Ex.: checkout_created, webhook_received, status_synced, refund_requested.</summary>
    public string Type { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public string? Status { get; set; }
    public decimal? Amount { get; set; }
    /// <summary>Resumo curto e sem dados sensíveis.</summary>
    public string? Details { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
