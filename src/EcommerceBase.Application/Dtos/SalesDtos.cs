using EcommerceBase.Domain.Enums;

namespace EcommerceBase.Application.Dtos;

public class CartLineDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ImageKey { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public int AvailableStock { get; set; }
    public bool IsPurchasable { get; set; }
    /// <summary>Aviso exibido ao cliente (estoque insuficiente, produto indisponível).</summary>
    public string? Warning { get; set; }
}

public class CartDto
{
    public int CartId { get; set; }
    public List<CartLineDto> Lines { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string? CouponCode { get; set; }
    public string? CouponMessage { get; set; }
    public int ItemCount { get; set; }
    public bool HasIssues { get; set; }
    public bool IsEmpty => Lines.Count == 0;
}

public class AddressDto
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string? Complement { get; set; }
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public class PaymentMethodDto
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class CheckoutSummaryDto
{
    public CartDto Cart { get; set; } = new();
    public List<AddressDto> Addresses { get; set; } = new();
    public List<PaymentMethodDto> PaymentMethods { get; set; } = new();
    public string? CustomerDocument { get; set; }
    public string? CustomerPhone { get; set; }
}

public class ShippingOptionDto
{
    /// <summary>Código estável da opção (ex.: "flat", "melhorenvio:1"). Revalidado no servidor ao fechar o pedido.</summary>
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? DeadlineDays { get; set; }
    public bool IsFree { get; set; }
}

public class ShippingQuoteResultDto
{
    public bool Available { get; set; }
    public string? Message { get; set; }
    public List<ShippingOptionDto> Options { get; set; } = new();

    public static ShippingQuoteResultDto Unavailable(string message) =>
        new() { Available = false, Message = message };
}

public sealed record ShippingItem(
    int ProductId,
    string Name,
    int Quantity,
    int WeightGrams,
    decimal HeightCm,
    decimal WidthCm,
    decimal LengthCm,
    decimal UnitPrice);

public sealed record ShippingQuoteRequest(
    string OriginPostalCode,
    string DestinationPostalCode,
    IReadOnlyList<ShippingItem> Items,
    decimal Subtotal);

public class CheckoutRequest
{
    public int AddressId { get; set; }
    public string ShippingOptionCode { get; set; } = string.Empty;
    public string PaymentMethodKey { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? CustomerNotes { get; set; }
    public string Document { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class CheckoutResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int OrderId { get; set; }
    public string? OrderNumber { get; set; }
    /// <summary>True quando o pedido já existia (reenvio do mesmo formulário).</summary>
    public bool WasDuplicate { get; set; }

    public static CheckoutResult Fail(string error) => new() { Success = false, Error = error };
}

public class OrderSummaryDto
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public bool CancellationRequested { get; set; }
}

public class OrderItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}

public class OrderPaymentDto
{
    public int Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public decimal RefundedAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? StatusDetail { get; set; }
    public string? ExternalPaymentId { get; set; }
    public string? CheckoutUrl { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
}

public class OrderHistoryDto
{
    public OrderStatus? FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class OrderDetailDto
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal ShippingTotal { get; set; }
    public decimal Total { get; set; }
    public string? CouponCode { get; set; }

    public string ShippingOptionName { get; set; } = string.Empty;
    public string ShippingProvider { get; set; } = string.Empty;
    public int? ShippingDeadlineDays { get; set; }
    public string? TrackingCode { get; set; }

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
    public bool CancellationRequested { get; set; }
    public string? CancellationRequestReason { get; set; }
    public string? CancellationReason { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }

    public List<OrderItemDto> Items { get; set; } = new();
    public List<OrderPaymentDto> Payments { get; set; } = new();
    public List<OrderHistoryDto> History { get; set; } = new();

    // Ações permitidas (calculadas no servidor)
    public bool CanCustomerCancelDirectly { get; set; }
    public bool CanCustomerRequestCancellation { get; set; }
    public bool CanPay { get; set; }
    public List<OrderStatus> AdminAllowedNextStatuses { get; set; } = new();
}

public class AdminOrderQuery
{
    public string? Search { get; set; }
    public OrderStatus? Status { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
