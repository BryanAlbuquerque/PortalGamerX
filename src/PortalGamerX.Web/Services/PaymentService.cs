using PortalGamerX.Data;
using PortalGamerX.Domain;
using PortalGamerX.Domain.Entities;
using PortalGamerX.Domain.Rules;
using PortalGamerX.Infrastructure;
using PortalGamerX.Integrations;
using PortalGamerX.Integrations.Payments;
using Microsoft.EntityFrameworkCore;

namespace PortalGamerX.Services;

public enum WebhookOutcome { Processed, Ignored, Unauthorized, BadRequest, Retry }

public interface IPaymentService
{
    Task<bool> IsEnabledAsync();
    Task<ProviderResult<string>> StartCheckoutAsync(Guid orderId, Guid? requesterUserId);
    Task<WebhookOutcome> HandleWebhookAsync(string? type, string? dataId, string? signature, string? requestId);
    Task<OperationResult> ReconcileAsync(string externalPaymentId, string source);
    Task<OperationResult> RefundAsync(Guid orderId);
    Task<ProviderResult<string>> TestConnectionAsync();
}

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly IEnumerable<IPaymentProvider> _providers;
    private readonly IIntegrationSettings _settings;
    private readonly IOrderService _orders;
    private readonly IEmailService _email;
    private readonly IStoreSettingsService _store;
    private readonly IPublicUrl _url;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(AppDbContext db, IEnumerable<IPaymentProvider> providers, IIntegrationSettings settings,
        IOrderService orders, IEmailService email, IStoreSettingsService store, IPublicUrl url, ILogger<PaymentService> logger)
    {
        _db = db;
        _providers = providers;
        _settings = settings;
        _orders = orders;
        _email = email;
        _store = store;
        _url = url;
        _logger = logger;
    }

    private async Task<IPaymentProvider?> ResolveAsync()
    {
        var name = await _settings.GetOrDefaultAsync(SettingKeys.PaymentProvider, MercadoPagoProvider.ProviderName);
        return _providers.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> IsEnabledAsync()
    {
        if (!await _settings.GetBoolAsync(SettingKeys.PaymentEnabled)) return false;
        var p = await ResolveAsync();
        return p != null && await p.IsConfiguredAsync();
    }

    public async Task<ProviderResult<string>> TestConnectionAsync()
    {
        var p = await ResolveAsync();
        return p == null ? ProviderResult<string>.Fail("Nenhum provedor de pagamento selecionado.") : await p.TestConnectionAsync();
    }

    public async Task<ProviderResult<string>> StartCheckoutAsync(Guid orderId, Guid? requesterUserId)
    {
        var order = await _db.Orders.Include(o => o.Payments).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null || (requesterUserId.HasValue && order.UserId != requesterUserId.Value))
            return ProviderResult<string>.Fail("Pedido não encontrado.");
        if (order.Status != OrderStatus.PendingPayment) return ProviderResult<string>.Fail("Este pedido não está aguardando pagamento.");
        if (!await IsEnabledAsync()) return ProviderResult<string>.Fail("O pagamento online não está disponível no momento. Entre em contato com a loja.");
        var provider = (await ResolveAsync())!;

        // Reaproveita a tentativa recente ainda sem pagamento associado (evita preferências duplicadas).
        var reusable = order.Payments
            .Where(p => p.Status == PaymentStatus.Pending && p.ExternalPaymentId == null && !string.IsNullOrEmpty(p.CheckoutUrl)
                        && p.CreatedAtUtc > DateTime.UtcNow.AddHours(-12))
            .OrderByDescending(p => p.CreatedAtUtc).FirstOrDefault();
        if (reusable != null) return ProviderResult<string>.Ok(reusable.CheckoutUrl!);

        var store = await _store.GetAsync();
        var payment = new Payment { OrderId = order.Id, Provider = provider.Name, Amount = order.Total, Status = PaymentStatus.Pending };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        var request = new PaymentRequest(
            order.Number,
            $"Pedido {order.Number} - {store.StoreName}".Truncate(250),
            order.Total,
            order.CustomerEmail,
            order.CustomerName,
            _url.Absolute("/pagamento/sucesso"),
            _url.Absolute("/pagamento/falha"),
            _url.Absolute("/pagamento/pendente"),
            _url.Absolute("/webhooks/mercadopago"),
            order.ExpiresAtUtc,
            payment.Id.ToString());

        var result = await provider.CreateCheckoutAsync(request);
        if (!result.Success || result.Value == null)
        {
            _db.Payments.Remove(payment);
            await _db.SaveChangesAsync();
            return ProviderResult<string>.Fail(result.Error ?? "Falha ao iniciar o pagamento.");
        }

        payment.ExternalPreferenceId = result.Value.PreferenceId;
        payment.CheckoutUrl = result.Value.CheckoutUrl.Truncate(1000);
        payment.UpdatedAtUtc = DateTime.UtcNow;
        _db.PaymentTransactions.Add(new PaymentTransaction
        {
            PaymentId = payment.Id,
            OrderId = order.Id,
            Provider = provider.Name,
            ExternalId = result.Value.PreferenceId,
            EventKey = $"pref:{result.Value.PreferenceId}",
            Kind = "checkout",
            Status = "created",
            Amount = order.Total,
            Message = result.Value.IsSandbox ? "Preferência criada (ambiente de testes)" : "Preferência criada"
        });
        await _db.SaveChangesAsync();
        return ProviderResult<string>.Ok(payment.CheckoutUrl);
    }

    public async Task<WebhookOutcome> HandleWebhookAsync(string? type, string? dataId, string? signature, string? requestId)
    {
        if (!string.IsNullOrEmpty(type) && !type.Equals("payment", StringComparison.OrdinalIgnoreCase)) return WebhookOutcome.Ignored;
        if (string.IsNullOrWhiteSpace(dataId) || dataId.Length > 30 || dataId.Any(c => !char.IsLetterOrDigit(c))) return WebhookOutcome.BadRequest;

        var provider = await ResolveAsync();
        if (provider == null) return WebhookOutcome.Ignored;
        if (!await provider.ValidateWebhookAsync(signature, requestId, dataId))
        {
            _logger.LogWarning("Webhook de pagamento rejeitado: assinatura inválida ou segredo não configurado.");
            return WebhookOutcome.Unauthorized;
        }

        // Mesmo com assinatura válida, o estado vem da CONSULTA autenticada ao provedor, não do corpo da notificação.
        var r = await ReconcileAsync(dataId, "webhook");
        return r.Success ? WebhookOutcome.Processed : WebhookOutcome.Retry;
    }

    public async Task<OperationResult> ReconcileAsync(string externalPaymentId, string source)
    {
        var provider = await ResolveAsync();
        if (provider == null || !await provider.IsConfiguredAsync()) return OperationResult.Fail("Provedor de pagamento não configurado.");

        var fetched = await provider.GetPaymentAsync(externalPaymentId);
        if (!fetched.Success || fetched.Value == null) return OperationResult.Fail(fetched.Error ?? "Falha na consulta do pagamento.");
        var pp = fetched.Value;

        var eventKey = $"{provider.Name}:{pp.ExternalId}:{pp.RawStatus}:{pp.LastUpdated}".Truncate(200);
        if (await _db.PaymentTransactions.AnyAsync(t => t.EventKey == eventKey)) return OperationResult.Ok(); // já processado

        var orderId = await _db.Orders.Where(o => o.Number == pp.ExternalReference).Select(o => (Guid?)o.Id).FirstOrDefaultAsync();
        if (orderId == null)
        {
            _logger.LogWarning("Pagamento {Id} sem pedido correspondente (referência desconhecida).", pp.ExternalId);
            _db.PaymentTransactions.Add(new PaymentTransaction
            {
                Provider = provider.Name, ExternalId = pp.ExternalId, EventKey = eventKey, Kind = source,
                Status = pp.RawStatus, Amount = pp.Amount, Message = "Pedido não encontrado para a referência informada."
            });
            await TrySaveAsync();
            return OperationResult.Ok();
        }

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                // Registro do evento + transição do pedido são atômicos: se algo falhar, nada é gravado e o evento pode ser reprocessado.
                await using var tx = await _db.Database.BeginTransactionAsync();
                await ApplyAsync(orderId.Value, provider.Name, pp, source, eventKey);
                await tx.CommitAsync();
                return OperationResult.Ok();
            }
            catch (Exception ex) when (ex is DbUpdateConcurrencyException or InvalidOperationException)
            {
                _db.ChangeTracker.Clear(); // recarrega e tenta novamente
            }
            catch (DbUpdateException ex)
            {
                _db.ChangeTracker.Clear();
                if (await _db.PaymentTransactions.AnyAsync(t => t.EventKey == eventKey)) return OperationResult.Ok(); // processado em paralelo
                _logger.LogError(ex, "Erro ao gravar atualização de pagamento.");
                return OperationResult.Fail("Erro ao gravar o pagamento.");
            }
        }
        return OperationResult.Fail("Conflito de concorrência ao atualizar o pagamento.");
    }

    private async Task TrySaveAsync()
    {
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException) { _db.ChangeTracker.Clear(); }
    }

    private async Task ApplyAsync(Guid orderId, string providerName, ProviderPayment pp, string source, string eventKey)
    {
        var order = await _db.Orders.Include(o => o.Items).Include(o => o.Payments).FirstAsync(o => o.Id == orderId);

        var payment = order.Payments.FirstOrDefault(p => p.ExternalPaymentId == pp.ExternalId)
                      ?? order.Payments.Where(p => p.ExternalPaymentId == null).OrderByDescending(p => p.CreatedAtUtc).FirstOrDefault();
        if (payment == null)
        {
            payment = new Payment { OrderId = order.Id, Provider = providerName, Amount = pp.Amount };
            _db.Payments.Add(payment);
            order.Payments.Add(payment);
        }

        var previous = payment.Status;
        var amountMismatch = pp.Status == PaymentStatus.Approved && Money.Round(pp.Amount) != order.Total;

        payment.ExternalPaymentId = pp.ExternalId;
        payment.StatusDetail = pp.StatusDetail?.Truncate(100);
        payment.PaymentMethod = pp.PaymentMethod?.Truncate(60);
        payment.Amount = Money.Round(pp.Amount);
        payment.UpdatedAtUtc = DateTime.UtcNow;
        // Valor divergente nunca é tratado como aprovado.
        payment.Status = amountMismatch ? PaymentStatus.InProcess : pp.Status;
        if (payment.Status == PaymentStatus.Approved) payment.ApprovedAtUtc ??= DateTime.UtcNow;

        order.PaymentStatus = order.Payments.Any(p => p.Status == PaymentStatus.Approved) ? PaymentStatus.Approved : payment.Status;
        order.UpdatedAtUtc = DateTime.UtcNow;

        _db.PaymentTransactions.Add(new PaymentTransaction
        {
            PaymentId = payment.Id,
            OrderId = order.Id,
            Provider = providerName,
            ExternalId = pp.ExternalId,
            EventKey = eventKey,
            Kind = source,
            Status = pp.RawStatus,
            StatusDetail = pp.StatusDetail?.Truncate(100),
            Amount = pp.Amount,
            Message = amountMismatch ? "Valor pago diverge do total do pedido: requer análise manual." : null
        });
        await _db.SaveChangesAsync();

        if (amountMismatch)
        {
            _logger.LogError("Pagamento {Pay} aprovado com valor divergente no pedido {Order}.", pp.ExternalId, order.Number);
            await AddNoteAsync(order, "Pagamento aprovado com valor divergente do pedido. Análise manual necessária.");
            return;
        }

        switch (pp.Status)
        {
            case PaymentStatus.Approved:
                if (order.Status == OrderStatus.PendingPayment)
                    await TransitionAsync(order, OrderStatus.Paid, $"Pagamento aprovado ({providerName} #{pp.ExternalId})");
                else if (order.Status == OrderStatus.Paid)
                    // Já pago mas sem entrega (falha transitória na tentativa anterior): reprocessa de forma idempotente.
                    await _orders.DeliverKeysAsync(order.Id, Actor.System);
                else if (order.Status == OrderStatus.Cancelled && previous != PaymentStatus.Approved)
                {
                    _logger.LogWarning("Pagamento aprovado após cancelamento do pedido {Order}.", order.Number);
                    await AddNoteAsync(order, "Pagamento aprovado APÓS o cancelamento do pedido: reembolse o cliente ou reative manualmente.");
                }
                break;

            case PaymentStatus.Refunded:
            case PaymentStatus.ChargedBack:
                if (previous != pp.Status)
                {
                    if (order.Status is OrderStatus.Paid or OrderStatus.Processing or OrderStatus.Delivered or OrderStatus.PendingPayment)
                        await TransitionAsync(order, OrderStatus.Cancelled, $"Pagamento {pp.Status.Label().ToLowerInvariant()}");
                    else
                        await AddNoteAsync(order, $"Pagamento {pp.Status.Label().ToLowerInvariant()} com o pedido já {order.Status.Label().ToLowerInvariant()}.");
                }
                break;
        }

        if (previous != payment.Status && payment.Status is PaymentStatus.Rejected or PaymentStatus.Cancelled)
            await NotifyPaymentAsync(order, payment.Status);
    }

    private async Task TransitionAsync(Order order, OrderStatus target, string note)
    {
        var r = await _orders.ApplyTransitionAsync(order, target, note, Actor.System);
        if (!r.Success) throw new InvalidOperationException(r.Error ?? "Transição recusada.");
    }

    private async Task AddNoteAsync(Order order, string note)
    {
        _db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id, FromStatus = order.Status, ToStatus = order.Status, Note = note.Truncate(500), ChangedByName = "sistema"
        });
        await _db.SaveChangesAsync();
    }

    private async Task NotifyPaymentAsync(Order order, PaymentStatus status)
    {
        try
        {
            await _email.EnqueueAsync(EmailKinds.PaymentUpdate, order.CustomerEmail, order.CustomerName, new Dictionary<string, string>
            {
                ["OrderNumber"] = order.Number,
                ["Status"] = status.Label(),
                ["Link"] = _url.Absolute("/conta/pedidos/" + order.Id)
            });
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Falha ao enfileirar e-mail de pagamento."); }
    }

    public async Task<OperationResult> RefundAsync(Guid orderId)
    {
        var order = await _db.Orders.Include(o => o.Payments).AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return OperationResult.Fail("Pedido não encontrado.");
        var payment = order.Payments.Where(p => p.Status == PaymentStatus.Approved && !string.IsNullOrEmpty(p.ExternalPaymentId))
            .OrderByDescending(p => p.CreatedAtUtc).FirstOrDefault();
        if (payment == null) return OperationResult.Fail("Não há pagamento aprovado para reembolsar.");

        var provider = await ResolveAsync();
        if (provider == null || !await provider.IsConfiguredAsync()) return OperationResult.Fail("Provedor de pagamento não configurado.");

        var refund = await provider.RefundAsync(payment.ExternalPaymentId!, null, $"refund-{payment.Id}");
        if (!refund.Success) return OperationResult.Fail(refund.Error ?? "Reembolso recusado.");

        _db.ChangeTracker.Clear();
        // Reembolso solicitado; atualizamos o estado agora se o provedor já refletir, senão o webhook concluirá.
        await ReconcileAsync(payment.ExternalPaymentId!, "refund");
        return OperationResult.Ok();
    }
}
