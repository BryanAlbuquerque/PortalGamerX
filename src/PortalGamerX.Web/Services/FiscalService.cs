using PortalGamerX.Data;
using PortalGamerX.Domain;
using PortalGamerX.Domain.Entities;
using PortalGamerX.Domain.Rules;
using PortalGamerX.Infrastructure;
using PortalGamerX.Integrations.Fiscal;
using Microsoft.EntityFrameworkCore;

namespace PortalGamerX.Services;

public interface IFiscalService
{
    Task<bool> IsConfiguredAsync();
    Task<OperationResult> RequestIssueAsync(Guid orderId);
    Task<OperationResult> RefreshAsync(int documentId);
}

/// <summary>
/// Orquestra a emissão via IFiscalProvider. Um documento só fica "Autorizado" se o provedor responder "authorized".
/// Sem provedor configurado, nada é gravado como emitido.
/// </summary>
public class FiscalService : IFiscalService
{
    private readonly AppDbContext _db;
    private readonly IEnumerable<IFiscalProvider> _providers;
    private readonly IIntegrationSettings _settings;
    private readonly IStoreSettingsService _store;

    public FiscalService(AppDbContext db, IEnumerable<IFiscalProvider> providers, IIntegrationSettings settings, IStoreSettingsService store)
    {
        _db = db;
        _providers = providers;
        _settings = settings;
        _store = store;
    }

    private async Task<IFiscalProvider> ResolveAsync()
    {
        var name = await _settings.GetOrDefaultAsync(SettingKeys.FiscalProvider, "None");
        return _providers.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
               ?? _providers.First(p => p.Name == "None");
    }

    public async Task<bool> IsConfiguredAsync() => await (await ResolveAsync()).IsConfiguredAsync();

    public async Task<OperationResult> RequestIssueAsync(Guid orderId)
    {
        var provider = await ResolveAsync();
        if (!await provider.IsConfiguredAsync()) return OperationResult.Fail(NotConfiguredFiscalProvider.Message);

        var order = await _db.Orders.Include(o => o.Items).ThenInclude(i => i.Jogo).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return OperationResult.Fail("Pedido não encontrado.");
        if (order.Status is OrderStatus.PendingPayment or OrderStatus.Cancelled)
            return OperationResult.Fail("Só é possível emitir documento fiscal para pedidos pagos.");
        if (await _db.FiscalDocuments.AnyAsync(f => f.OrderId == orderId && f.Status != FiscalStatus.Cancelled && f.Status != FiscalStatus.Rejected && f.Status != FiscalStatus.Error))
            return OperationResult.Fail("Já existe documento fiscal em andamento ou autorizado para este pedido.");

        // Pré-validação dos dados fiscais (empresa + jogos): evita criar documento que o provedor rejeitaria.
        var store = await _store.GetAsync();
        var problems = FiscalRules.CheckIssuer(store);
        foreach (var item in order.Items)
        {
            if (item.Jogo == null) { problems.Add($"\"{item.JogoName}\": jogo não está mais cadastrado."); continue; }
            problems.AddRange(FiscalRules.CheckItem(item.Jogo));
        }
        if (string.IsNullOrWhiteSpace(order.ShipState) || order.ShipState.Length != 2) problems.Add("UF de entrega do pedido inválida.");
        if (problems.Count > 0)
            return OperationResult.Fail("Dados fiscais incompletos: " + string.Join(" ", problems.Distinct()));

        var docType = await _settings.GetOrDefaultAsync(SettingKeys.FiscalDocumentType, "NFe");
        var doc = new FiscalDocument { OrderId = orderId, DocumentType = docType, Provider = provider.Name, Status = FiscalStatus.Processing };
        doc.Events.Add(new FiscalEvent { EventType = "issue_requested", Message = "Emissão solicitada ao provedor." });
        _db.FiscalDocuments.Add(doc);
        await _db.SaveChangesAsync();

        var cfop = FiscalRules.Cfop(store.State, order.ShipState)!;
        var items = order.Items.Select(i => new FiscalItem(
            FiscalRules.ItemDescription(i.JogoName, i.Jogo!.Platform), i.Sku, i.Jogo.EffectiveNcm, i.Quantity, i.UnitPrice,
            cfop, i.Jogo.OriginCode, FiscalRules.Unit, i.Jogo.Cest.NullIfEmpty(), i.Jogo.Barcode.NullIfEmpty())).ToList();

        var request = new FiscalIssueRequest(order.Number, docType, order.CustomerName, order.CustomerDocument, order.CustomerEmail,
            order.ShipPostalCode, order.ShipStreet, order.ShipNumber, order.ShipComplement, order.ShipDistrict, order.ShipCity, order.ShipState,
            items, order.ShippingTotal, order.DiscountTotal, order.Total,
            Documents.OnlyDigits(store.Cnpj), store.StateRegistration, store.State?.Trim().ToUpperInvariant(),
            FiscalRules.IsInterstate(store.State, order.ShipState));

        var result = await provider.IssueAsync(request);
        Apply(doc, result);
        await _db.SaveChangesAsync();
        return result.Success ? OperationResult.Ok() : OperationResult.Fail(result.Error ?? "Falha na emissão.");
    }

    public async Task<OperationResult> RefreshAsync(int documentId)
    {
        var doc = await _db.FiscalDocuments.Include(d => d.Events).FirstOrDefaultAsync(d => d.Id == documentId);
        if (doc == null) return OperationResult.Fail("Documento não encontrado.");
        if (string.IsNullOrEmpty(doc.ExternalReference)) return OperationResult.Fail("Documento sem referência do provedor.");
        var provider = await ResolveAsync();
        if (!await provider.IsConfiguredAsync()) return OperationResult.Fail(NotConfiguredFiscalProvider.Message);
        var result = await provider.QueryAsync(doc.ExternalReference);
        Apply(doc, result);
        await _db.SaveChangesAsync();
        return result.Success ? OperationResult.Ok() : OperationResult.Fail(result.Error ?? "Falha na consulta.");
    }

    private static void Apply(FiscalDocument doc, Integrations.ProviderResult<FiscalProviderResponse> result)
    {
        doc.UpdatedAtUtc = DateTime.UtcNow;
        if (!result.Success || result.Value == null)
        {
            doc.Status = FiscalStatus.Error;
            doc.RejectionMessage = result.Error?.Truncate(500);
            doc.Events.Add(new FiscalEvent { EventType = "error", Message = result.Error?.Truncate(500) });
            return;
        }
        var r = result.Value;
        doc.ExternalReference = r.ExternalReference.Truncate(100);
        doc.Number = r.Number; doc.Series = r.Series; doc.AccessKey = r.AccessKey; doc.Protocol = r.Protocol;
        doc.XmlUrl = r.XmlUrl; doc.PdfUrl = r.PdfUrl;
        doc.Status = r.Status switch
        {
            "authorized" => FiscalStatus.Authorized, // somente com confirmação do provedor
            "rejected" => FiscalStatus.Rejected,
            "cancelled" => FiscalStatus.Cancelled,
            "processing" => FiscalStatus.Processing,
            _ => FiscalStatus.Error
        };
        doc.RejectionMessage = doc.Status == FiscalStatus.Rejected ? r.Message?.Truncate(500) : null;
        doc.Events.Add(new FiscalEvent { EventType = r.Status, Message = r.Message?.Truncate(500) });
    }
}
