using System.ComponentModel.DataAnnotations;

namespace PortalGamerX.Domain.Entities;

public class Cart
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    [MaxLength(64)] public string? AnonymousToken { get; set; }
    [MaxLength(50)] public string? CouponCode { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<CartItem> Items { get; set; } = new();
}

public class CartItem
{
    public int Id { get; set; }
    public Guid CartId { get; set; }
    public Cart? Cart { get; set; }
    public int JogoId { get; set; }
    public Jogo? Jogo { get; set; }
    public int Quantity { get; set; }
}

public class Coupon
{
    public int Id { get; set; }
    [MaxLength(50)] public string Code { get; set; } = string.Empty;
    [MaxLength(200)] public string? Description { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal Value { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public int? MaxUses { get; set; }
    public int? MaxUsesPerCustomer { get; set; }
    public bool IsActive { get; set; } = true;
    public List<Category> Categories { get; set; } = new();
    public List<Jogo> Jogos { get; set; } = new();
}

public class CouponUsage
{
    public int Id { get; set; }
    public int CouponId { get; set; }
    public Coupon? Coupon { get; set; }
    public Guid UserId { get; set; }
    public Guid OrderId { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTime UsedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(30)] public string Number { get; set; } = string.Empty;
    public Guid IdempotencyKey { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal Total { get; set; }
    [MaxLength(50)] public string? CouponCode { get; set; }

    [MaxLength(150)] public string CustomerName { get; set; } = string.Empty;
    [MaxLength(256)] public string CustomerEmail { get; set; } = string.Empty;
    [MaxLength(30)] public string? CustomerPhone { get; set; }
    [MaxLength(20)] public string? CustomerDocument { get; set; }

    // Endereço de cobrança (necessário para o documento fiscal: o ISS de licenciamento de software é devido no domicílio do tomador).
    [MaxLength(8)] public string BillingPostalCode { get; set; } = string.Empty;
    [MaxLength(200)] public string BillingStreet { get; set; } = string.Empty;
    [MaxLength(20)] public string BillingNumber { get; set; } = string.Empty;
    [MaxLength(100)] public string? BillingComplement { get; set; }
    [MaxLength(100)] public string BillingDistrict { get; set; } = string.Empty;
    [MaxLength(100)] public string BillingCity { get; set; } = string.Empty;
    [MaxLength(2)] public string BillingState { get; set; } = string.Empty;

    [MaxLength(1000)] public string? CustomerNotes { get; set; }
    [MaxLength(2000)] public string? AdminNotes { get; set; }

    public bool CancellationRequested { get; set; }
    public DateTime? CancellationRequestedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }

    /// <summary>Token de concorrência (xmin no PostgreSQL).</summary>
    public uint Version { get; set; }

    public List<OrderItem> Items { get; set; } = new();
    public List<OrderStatusHistory> History { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public int JogoId { get; set; }
    public Jogo? Jogo { get; set; }
    [MaxLength(200)] public string JogoName { get; set; } = string.Empty;
    [MaxLength(60)] public string Sku { get; set; } = string.Empty;
    [MaxLength(300)] public string? ImagePath { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => Domain.Rules.Money.Round(UnitPrice * Quantity);
}

public class OrderStatusHistory
{
    public int Id { get; set; }
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public OrderStatus? FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    [MaxLength(500)] public string? Note { get; set; }
    public Guid? ChangedByUserId { get; set; }
    [MaxLength(150)] public string? ChangedByName { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    [MaxLength(40)] public string Provider { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public decimal Amount { get; set; }
    [MaxLength(100)] public string? ExternalPreferenceId { get; set; }
    [MaxLength(100)] public string? ExternalPaymentId { get; set; }
    [MaxLength(1000)] public string? CheckoutUrl { get; set; }
    [MaxLength(100)] public string? StatusDetail { get; set; }
    [MaxLength(60)] public string? PaymentMethod { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAtUtc { get; set; }
    public uint Version { get; set; }
}

/// <summary>Registro de eventos/consultas do provedor. Não guarda dados de cartão.</summary>
public class PaymentTransaction
{
    public long Id { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? OrderId { get; set; }
    [MaxLength(40)] public string Provider { get; set; } = string.Empty;
    [MaxLength(100)] public string? ExternalId { get; set; }
    /// <summary>Chave única do evento (deduplicação / idempotência).</summary>
    [MaxLength(200)] public string EventKey { get; set; } = string.Empty;
    [MaxLength(30)] public string Kind { get; set; } = string.Empty;
    [MaxLength(40)] public string? Status { get; set; }
    [MaxLength(100)] public string? StatusDetail { get; set; }
    public decimal? Amount { get; set; }
    [MaxLength(500)] public string? Message { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
