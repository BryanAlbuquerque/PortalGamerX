namespace EcommerceBase.Web.Integrations.Fiscal;

public record FiscalItem(string Description, string Sku, string? Ncm, int Quantity, decimal UnitPrice);

public record FiscalIssueRequest(
    string OrderNumber,
    string DocumentType,
    string RecipientName,
    string? RecipientDocument,
    string RecipientEmail,
    string PostalCode, string Street, string Number, string? Complement, string District, string City, string State,
    IReadOnlyList<FiscalItem> Items,
    decimal ShippingTotal,
    decimal DiscountTotal,
    decimal Total);

public record FiscalProviderResponse(
    string ExternalReference,
    string Status,            // processing | authorized | rejected | cancelled | error
    string? Number,
    string? Series,
    string? AccessKey,
    string? Protocol,
    string? XmlUrl,
    string? PdfUrl,
    string? Message);

/// <summary>
/// Contrato de emissão fiscal. A emissão real depende de um provedor contratado (ex.: serviços de emissão de NF-e/NFC-e),
/// certificado digital, credenciamento na SEFAZ e homologação. Nenhum adaptador real é distribuído nesta base.
/// </summary>
public interface IFiscalProvider
{
    string Name { get; }
    Task<bool> IsConfiguredAsync();
    Task<ProviderResult<FiscalProviderResponse>> IssueAsync(FiscalIssueRequest request, CancellationToken ct = default);
    Task<ProviderResult<FiscalProviderResponse>> QueryAsync(string externalReference, CancellationToken ct = default);
    Task<ProviderResult<FiscalProviderResponse>> CancelAsync(string externalReference, string reason, CancellationToken ct = default);
}

public class NotConfiguredFiscalProvider : IFiscalProvider
{
    public const string Message = "Integração fiscal NÃO configurada: nenhum provedor fiscal foi implementado/contratado. Nenhum documento fiscal é emitido.";
    public string Name => "None";
    public Task<bool> IsConfiguredAsync() => Task.FromResult(false);
    public Task<ProviderResult<FiscalProviderResponse>> IssueAsync(FiscalIssueRequest request, CancellationToken ct = default) =>
        Task.FromResult(ProviderResult<FiscalProviderResponse>.Fail(Message));
    public Task<ProviderResult<FiscalProviderResponse>> QueryAsync(string externalReference, CancellationToken ct = default) =>
        Task.FromResult(ProviderResult<FiscalProviderResponse>.Fail(Message));
    public Task<ProviderResult<FiscalProviderResponse>> CancelAsync(string externalReference, string reason, CancellationToken ct = default) =>
        Task.FromResult(ProviderResult<FiscalProviderResponse>.Fail(Message));
}
