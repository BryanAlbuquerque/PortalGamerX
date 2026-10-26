namespace PortalGamerX.Integrations.Fiscal;

/// <summary>
/// Item da NFS-e (licença de uso de jogo). ServiceCode = item da lista de serviços (LC 116/2003, padrão 01.05);
/// MunicipalTaxCode/Cnae/IssRatePercent vêm da configuração fiscal (podem variar por município).
/// </summary>
public record FiscalItem(
    string Description, string Sku, int Quantity, decimal UnitPrice,
    string ServiceCode, string? MunicipalTaxCode, string? Cnae, decimal IssRatePercent);

public record FiscalIssueRequest(
    string OrderNumber,
    string DocumentType,
    // ---- Tomador (cliente) ----
    string RecipientName,
    string? RecipientDocument,
    string RecipientEmail,
    string PostalCode, string Street, string Number, string? Complement, string District, string City, string State,
    // ---- Serviço ----
    IReadOnlyList<FiscalItem> Items,
    decimal DiscountTotal,
    decimal Total,
    // ---- Prestador (empresa) ----
    string? IssuerCnpj = null,
    string? IssuerMunicipalRegistration = null,
    string? IssuerCity = null,
    string? IssuerState = null,
    string? IssuerPostalCode = null);

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
/// Contrato de emissão fiscal para a venda de keys de jogos (NFS-e). A emissão real depende de um provedor contratado
/// (ou da integração direta com o município / NFS-e Nacional), certificado digital, cadastro municipal e homologação.
/// Nenhum adaptador real é distribuído nesta base.
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
