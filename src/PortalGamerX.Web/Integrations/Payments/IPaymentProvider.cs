using PortalGamerX.Domain;

namespace PortalGamerX.Integrations.Payments;

public record PaymentRequest(
    string OrderNumber,
    string Description,
    decimal Total,
    string PayerEmail,
    string PayerName,
    string SuccessUrl,
    string FailureUrl,
    string PendingUrl,
    string NotificationUrl,
    DateTime? ExpiresAtUtc,
    string IdempotencyKey);

public record CheckoutSession(string PreferenceId, string CheckoutUrl, bool IsSandbox);

public record ProviderPayment(
    string ExternalId,
    PaymentStatus Status,
    string RawStatus,
    string? StatusDetail,
    string? ExternalReference,
    decimal Amount,
    string? PaymentMethod,
    string? LastUpdated,
    decimal RefundedAmount);

public interface IPaymentProvider
{
    string Name { get; }
    Task<bool> IsConfiguredAsync();
    Task<ProviderResult<CheckoutSession>> CreateCheckoutAsync(PaymentRequest request, CancellationToken ct = default);
    /// <summary>Consulta autenticada ao provedor: é a fonte de verdade do estado do pagamento.</summary>
    Task<ProviderResult<ProviderPayment>> GetPaymentAsync(string externalPaymentId, CancellationToken ct = default);
    Task<ProviderResult<string>> RefundAsync(string externalPaymentId, decimal? amount, string idempotencyKey, CancellationToken ct = default);
    Task<ProviderResult<string>> TestConnectionAsync(CancellationToken ct = default);
    /// <summary>Valida a autenticidade da notificação (assinatura HMAC) conforme o provedor.</summary>
    Task<bool> ValidateWebhookAsync(string? signatureHeader, string? requestId, string? dataId);
}
