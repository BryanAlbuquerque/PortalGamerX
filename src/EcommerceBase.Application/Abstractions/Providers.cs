using EcommerceBase.Application.Dtos;
using EcommerceBase.Domain.Enums;

namespace EcommerceBase.Application.Abstractions;

// ===================== PAGAMENTO =====================

public sealed record PaymentCheckoutRequest(
    string OrderNumber,
    decimal Amount,
    string Description,
    string PayerEmail,
    string PayerName,
    string SuccessUrl,
    string FailureUrl,
    string PendingUrl,
    string NotificationUrl,
    DateTime? ExpiresAtUtc,
    string IdempotencyKey,
    IReadOnlyList<PaymentCheckoutItem> Items);

public sealed record PaymentCheckoutItem(string Id, string Title, int Quantity, decimal UnitPrice);

public sealed record PaymentCheckoutResult(
    bool Succeeded,
    string? PreferenceId,
    string? CheckoutUrl,
    string? Error);

/// <summary>Estado do pagamento conforme informado pelo provedor (consulta autenticada).</summary>
public sealed record ProviderPaymentInfo(
    string ExternalPaymentId,
    string? ExternalReference,
    PaymentStatus Status,
    string? StatusDetail,
    string? PaymentMethod,
    decimal Amount,
    decimal RefundedAmount);

public sealed record ProviderPaymentLookup(bool Succeeded, ProviderPaymentInfo? Payment, string? Error, bool NotFound = false);

public sealed record WebhookHeaders(string? Signature, string? RequestId, string? DataId);

public sealed record RefundResult(bool Succeeded, string? Error);

public interface IPaymentProvider
{
    /// <summary>Chave estável do provedor (ex.: "mercadopago").</summary>
    string Key { get; }

    /// <summary>True se o provedor está habilitado e com as credenciais mínimas.</summary>
    Task<bool> IsConfiguredAsync(CancellationToken ct = default);

    Task<PaymentCheckoutResult> CreateCheckoutAsync(PaymentCheckoutRequest request, CancellationToken ct = default);

    Task<ProviderPaymentLookup> GetPaymentAsync(string externalPaymentId, CancellationToken ct = default);

    /// <summary>Busca o pagamento mais recente pela referência externa (número do pedido).</summary>
    Task<ProviderPaymentLookup> FindPaymentByReferenceAsync(string externalReference, CancellationToken ct = default);

    /// <summary>Valida a autenticidade da notificação conforme o mecanismo do provedor.</summary>
    Task<bool> ValidateWebhookAsync(WebhookHeaders headers, CancellationToken ct = default);

    Task<RefundResult> RefundAsync(string externalPaymentId, decimal? amount, string idempotencyKey, CancellationToken ct = default);

    Task<ProviderTestResult> TestConnectionAsync(CancellationToken ct = default);
}

public sealed record ProviderTestResult(bool Succeeded, string Message);

// ===================== FRETE =====================

public interface IShippingProvider
{
    string Key { get; }
    Task<bool> IsConfiguredAsync(CancellationToken ct = default);

    /// <summary>Cota fretes reais. Nunca devolve valores inventados: falhas voltam como Unavailable.</summary>
    Task<ShippingQuoteResultDto> QuoteAsync(ShippingQuoteRequest request, CancellationToken ct = default);

    Task<ProviderTestResult> TestConnectionAsync(CancellationToken ct = default);
}

// ===================== E-MAIL =====================

public sealed record EmailSendRequest(string To, string Subject, string BodyHtml);
public sealed record EmailSendResult(bool Succeeded, string? Error);

public interface IEmailProvider
{
    string Key { get; }
    Task<bool> IsConfiguredAsync(CancellationToken ct = default);
    Task<EmailSendResult> SendAsync(EmailSendRequest request, CancellationToken ct = default);
}

// ===================== FISCAL =====================

public sealed record FiscalIssueRequest(
    string Reference,
    bool Homologation,
    FiscalCompany Company,
    FiscalRecipient Recipient,
    IReadOnlyList<FiscalItem> Items,
    decimal ShippingTotal,
    decimal DiscountTotal,
    decimal Total,
    DateTime IssuedAtUtc,
    string TimeZoneId);

public sealed record FiscalCompany(
    string LegalName, string TradeName, string Cnpj, string StateRegistration,
    int TaxRegimeCode, string PostalCode, string Street, string Number, string? Complement,
    string District, string City, string State, string? Phone);

public sealed record FiscalRecipient(
    string Name, string Document, string? Email, string? Phone,
    string PostalCode, string Street, string Number, string? Complement,
    string District, string City, string State);

public sealed record FiscalItem(
    int Number, string Sku, string Description, string Ncm, string? Cest, string Unit,
    int OriginCode, int Quantity, decimal UnitPrice, decimal Total);

public sealed record FiscalProviderResult(
    bool Succeeded,
    FiscalDocumentStatus Status,
    string? ProviderStatus,
    string? Number,
    string? Series,
    string? AccessKey,
    string? Protocol,
    string? XmlUrl,
    string? DanfeUrl,
    string? Message);

public interface IFiscalProvider
{
    string Key { get; }
    /// <summary>Documentos suportados por este adaptador (ex.: "NFe").</summary>
    IReadOnlyCollection<string> SupportedDocuments { get; }
    Task<bool> IsConfiguredAsync(CancellationToken ct = default);
    Task<FiscalProviderResult> IssueAsync(FiscalIssueRequest request, CancellationToken ct = default);
    Task<FiscalProviderResult> QueryAsync(string reference, bool homologation, CancellationToken ct = default);
    Task<FiscalProviderResult> CancelAsync(string reference, bool homologation, string justification, CancellationToken ct = default);
    Task<ProviderTestResult> TestConnectionAsync(CancellationToken ct = default);
}

// ===================== ARQUIVOS =====================

public sealed record StoredFileInfo(string Key, string ContentType, long Length);
public sealed record StoredFileContent(Stream Content, string ContentType, long Length);

public interface IFileStorage
{
    /// <summary>Salva o arquivo com chave gerada pelo servidor (nunca derivada do nome enviado).</summary>
    Task<StoredFileInfo> SaveAsync(Stream content, string contentType, string extension, CancellationToken ct = default);
    Task<StoredFileContent?> OpenReadAsync(string key, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}
