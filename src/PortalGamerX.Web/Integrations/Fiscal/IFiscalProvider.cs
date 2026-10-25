namespace PortalGamerX.Integrations.Fiscal;

/// <summary>
/// Item da nota (jogo). Unit = unidade comercial; Origin = origem da mercadoria (tabela NF-e, 0 a 8);
/// Cfop já resolvido (5102 / 6108); Gtin = código de barras EAN/GTIN do jogo, se houver; Cest só quando aplicável.
/// </summary>
public record FiscalItem(
    string Description, string Sku, string? Ncm, int Quantity, decimal UnitPrice,
    string Cfop = "5102", int Origin = 0, string Unit = "UN", string? Cest = null, string? Gtin = null);

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
    decimal Total,
    // ---- Emitente e contexto da operação (venda online a consumidor final) ----
    string? IssuerCnpj = null,
    string? IssuerStateRegistration = null,
    string? IssuerState = null,
    bool IsInterstate = false,
    bool ConsumerFinal = true,       // indFinal = 1
    int PresenceIndicator = 2);      // indPres = 2 (operação não presencial, pela Internet)

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
/// Contrato de emissão fiscal para a venda de jogos (mídia física, NF-e modelo 55). A emissão real depende de um provedor contratado (ex.: serviços de emissão de NF-e/NFC-e),
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
