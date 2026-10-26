using PortalGamerX.Data;
using PortalGamerX.Domain;
using PortalGamerX.Domain.Entities;
using PortalGamerX.Domain.Rules;
using PortalGamerX.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace PortalGamerX.Services;

public interface IOrderService
{
    /// <summary>Transição manual (administrador): apenas cancelar um pedido ainda não entregue. "Pago" e "Entregue" são automáticos.</summary>
    Task<OperationResult> ChangeStatusAsync(Guid orderId, OrderStatus target, string? note, Actor actor);
    /// <summary>
    /// Entrega as keys de um pedido PAGO (Reservada → Vendida), muda o status para Entregue e envia o e-mail com as keys.
    /// Idempotente: pode ser chamado de novo sem duplicar nada (reprocesso de webhook, botão do administrador, tarefa de fundo).
    /// </summary>
    Task<OperationResult> DeliverKeysAsync(Guid orderId, Actor actor);
    /// <summary>Tenta entregar pedidos pagos que ficaram sem entrega (falha transitória). Retorna quantos foram entregues.</summary>
    Task<int> DeliverPendingAsync();
    /// <summary>Aplica uma transição em um pedido já carregado (com Items). Uso interno do fluxo de pagamento.</summary>
    Task<OperationResult> ApplyTransitionAsync(Order order, OrderStatus target, string? note, Actor actor);
    Task<OperationResult> CancelByCustomerAsync(Guid orderId, Guid userId);
    Task<OperationResult> RequestCancellationAsync(Guid orderId, Guid userId);
    Task<OperationResult> SaveAdminNotesAsync(Guid orderId, string? notes);
    Task<int> CancelExpiredAsync();
}

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    private readonly IStockService _stock;
    private readonly IEmailService _email;
    private readonly IPublicUrl _url;
    private readonly ILogger<OrderService> _logger;

    public OrderService(AppDbContext db, IStockService stock, IEmailService email, IPublicUrl url, ILogger<OrderService> logger)
    {
        _db = db;
        _stock = stock;
        _email = email;
        _url = url;
        _logger = logger;
    }

    private Task<Order?> LoadAsync(Guid id) =>
        _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);

    public async Task<OperationResult> ChangeStatusAsync(Guid orderId, OrderStatus target, string? note, Actor actor)
    {
        var order = await LoadAsync(orderId);
        if (order == null) return OperationResult.Fail("Pedido não encontrado.");
        if (target == OrderStatus.Paid) return OperationResult.Fail("O status \"Pago\" só é definido após a confirmação do pagamento pelo provedor.");
        if (target == OrderStatus.Delivered) return OperationResult.Fail("A entrega é automática após o pagamento. Use \"Entregar keys\" se ela não ocorreu.");
        if (!OrderStateMachine.AdminTargets(order.Status).Contains(target))
            return OperationResult.Fail($"Transição não permitida: {order.Status.Label()} → {target.Label()}.");
        return await ApplyTransitionAsync(order, target, note, actor);
    }

    public async Task<OperationResult> ApplyTransitionAsync(Order order, OrderStatus target, string? note, Actor actor)
    {
        if (!OrderStateMachine.CanTransition(order.Status, target))
            return OperationResult.Fail($"Transição não permitida: {order.Status.Label()} → {target.Label()}.");

        var from = order.Status;
        order.Status = target;
        order.UpdatedAtUtc = DateTime.UtcNow;
        if (target == OrderStatus.Paid) order.PaidAtUtc = DateTime.UtcNow;
        if (target == OrderStatus.Cancelled) order.CancellationRequested = false;

        _db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = from,
            ToStatus = target,
            Note = note?.Truncate(500),
            ChangedByUserId = actor.UserId,
            ChangedByName = actor.Name
        });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult.Fail("O pedido foi alterado por outra operação. Recarregue a página e tente novamente.");
        }

        if (target == OrderStatus.Cancelled) await ReleaseOrderResourcesAsync(order, from);

        await TryNotifyAsync(order, target);

        // Pagamento confirmado → entrega imediata das keys (produto digital).
        if (target == OrderStatus.Paid)
        {
            var delivery = await DeliverKeysCoreAsync(order, actor);
            if (!delivery.Success) _logger.LogError("Pedido {Order} pago, mas as keys não foram entregues: {Error}", order.Number, delivery.Error);
        }
        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeliverKeysAsync(Guid orderId, Actor actor)
    {
        var order = await LoadAsync(orderId);
        if (order == null) return OperationResult.Fail("Pedido não encontrado.");
        if (order.Status == OrderStatus.Delivered) return OperationResult.Ok();
        if (order.Status != OrderStatus.Paid) return OperationResult.Fail("Só é possível entregar as keys de um pedido pago.");
        return await DeliverKeysCoreAsync(order, actor);
    }

    private async Task<OperationResult> DeliverKeysCoreAsync(Order order, Actor actor)
    {
        var expected = order.Items.Sum(i => i.Quantity);
        var sold = await _stock.MarkSoldAsync(order.Id);
        if (sold < expected)
        {
            var msg = $"ATENÇÃO: pedido pago, mas só {sold} de {expected} key(s) estão reservadas. Verifique o estoque de keys e cancele/reembolse se necessário.";
            // A tarefa de fundo repete a tentativa: registra o alerta uma única vez.
            if (!await _db.OrderStatusHistories.AnyAsync(h => h.OrderId == order.Id && h.Note == msg))
            {
                _db.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = order.Id, FromStatus = order.Status, ToStatus = order.Status, Note = msg, ChangedByName = "sistema"
                });
                await _db.SaveChangesAsync();
            }
            return OperationResult.Fail(msg);
        }
        return await ApplyTransitionAsync(order, OrderStatus.Delivered, "Keys liberadas ao cliente", actor);
    }

    public async Task<int> DeliverPendingAsync()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-2);
        var ids = await _db.Orders
            .Where(o => o.Status == OrderStatus.Paid && o.UpdatedAtUtc < cutoff)
            .Select(o => o.Id).Take(25).ToListAsync();
        var count = 0;
        foreach (var id in ids)
        {
            var r = await DeliverKeysAsync(id, Actor.System);
            if (r.Success) count++;
            _db.ChangeTracker.Clear();
        }
        return count;
    }

    /// <summary>
    /// Pedido cancelado: devolve ao estoque as keys ainda reservadas, invalida as keys já entregues (reembolso/estorno)
    /// e libera o uso do cupom.
    /// </summary>
    private async Task ReleaseOrderResourcesAsync(Order order, OrderStatus from)
    {
        foreach (var jogoId in order.Items.Select(i => i.JogoId).Distinct())
            await _stock.ReleaseAsync(jogoId, order.Id, $"Cancelamento do pedido {order.Number}");

        var voided = await _stock.VoidSoldKeysAsync(order.Id, $"Pedido {order.Number} cancelado/reembolsado após a entrega");
        if (voided > 0)
        {
            _db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id, FromStatus = order.Status, ToStatus = order.Status, ChangedByName = "sistema",
                Note = $"{voided} key(s) já entregue(s) foram invalidadas. Se possível, bloqueie-as também na loja de origem."
            });
            await _db.SaveChangesAsync();
        }

        var usages = await _db.CouponUsages.Where(u => u.OrderId == order.Id).ToListAsync();
        if (usages.Count > 0)
        {
            _db.CouponUsages.RemoveRange(usages);
            await _db.SaveChangesAsync();
        }
    }

    private async Task TryNotifyAsync(Order order, OrderStatus target)
    {
        try
        {
            var link = _url.Absolute("/conta/pedidos/" + order.Id);
            if (target == OrderStatus.Delivered)
            {
                var keys = await _stock.GetDeliveredKeysAsync(order.Id, includeCode: true);
                var vars = new Dictionary<string, string>
                {
                    ["OrderNumber"] = order.Number,
                    ["Link"] = link,
                    ["KeysHtml"] = BuildKeysHtml(keys)
                };
                // sensitive = true: o corpo (com as keys) é apagado do banco depois do envio.
                await _email.EnqueueAsync(EmailKinds.KeysDelivered, order.CustomerEmail, order.CustomerName, vars, sensitive: true);
                return;
            }

            var common = new Dictionary<string, string>
            {
                ["OrderNumber"] = order.Number,
                ["Status"] = target.Label(),
                ["Total"] = order.Total.ToBrl(),
                ["Link"] = link
            };
            if (target == OrderStatus.Paid) await _email.EnqueueAsync(EmailKinds.PaymentUpdate, order.CustomerEmail, order.CustomerName, common);
            else if (target == OrderStatus.Cancelled) await _email.EnqueueAsync(EmailKinds.OrderUpdate, order.CustomerEmail, order.CustomerName, common);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enfileirar e-mail do pedido {Order}.", order.Number);
        }
    }

    /// <summary>HTML das keys para o e-mail. Todo texto é codificado aqui (a variável "KeysHtml" é inserida sem nova codificação).</summary>
    public static string BuildKeysHtml(IEnumerable<DeliveredKey> keys)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var g in keys.GroupBy(k => k.JogoId))
        {
            var first = g.First();
            sb.Append("<p><strong>").Append(System.Net.WebUtility.HtmlEncode(first.JogoName)).Append("</strong><br />")
              .Append(System.Net.WebUtility.HtmlEncode($"Ativar em: {first.Store.Label()} · Região: {first.Region.Label()}")).Append("</p><ul>");
            foreach (var k in g)
                sb.Append("<li><code style=\"font-size:15px\">").Append(System.Net.WebUtility.HtmlEncode(k.Code ?? string.Empty)).Append("</code></li>");
            sb.Append("</ul>");
            if (!string.IsNullOrWhiteSpace(first.Instructions))
                sb.Append("<p style=\"color:#555\">").Append(System.Net.WebUtility.HtmlEncode(first.Instructions).Replace("\n", "<br />")).Append("</p>");
        }
        return sb.ToString();
    }

    public async Task<OperationResult> CancelByCustomerAsync(Guid orderId, Guid userId)
    {
        var order = await LoadAsync(orderId);
        if (order == null || order.UserId != userId) return OperationResult.Fail("Pedido não encontrado.");
        if (!OrderStateMachine.CustomerCanCancelDirectly(order.Status))
            return OperationResult.Fail("Este pedido não pode ser cancelado diretamente. Use \"Solicitar cancelamento\".");
        return await ApplyTransitionAsync(order, OrderStatus.Cancelled, "Cancelado pelo cliente", new Actor(userId, order.CustomerName));
    }

    public async Task<OperationResult> RequestCancellationAsync(Guid orderId, Guid userId)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null || order.UserId != userId) return OperationResult.Fail("Pedido não encontrado.");
        if (!OrderStateMachine.CustomerCanRequestCancellation(order.Status))
            return OperationResult.Fail("O cancelamento não pode ser solicitado neste status.");
        if (order.CancellationRequested) return OperationResult.Fail("O cancelamento já foi solicitado e está em análise.");

        order.CancellationRequested = true;
        order.CancellationRequestedAtUtc = DateTime.UtcNow;
        _db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = order.Status,
            ToStatus = order.Status,
            Note = "Cliente solicitou cancelamento",
            ChangedByUserId = userId,
            ChangedByName = order.CustomerName
        });
        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<OperationResult> SaveAdminNotesAsync(Guid orderId, string? notes)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return OperationResult.Fail("Pedido não encontrado.");
        order.AdminNotes = notes.NullIfEmpty()?.Truncate(2000);
        order.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<int> CancelExpiredAsync()
    {
        var now = DateTime.UtcNow;
        var ids = await _db.Orders
            .Where(o => o.Status == OrderStatus.PendingPayment
                        && o.ExpiresAtUtc != null && o.ExpiresAtUtc < now
                        && !o.Payments.Any(p => p.Status == PaymentStatus.InProcess))
            .Select(o => o.Id)
            .Take(50)
            .ToListAsync();

        var count = 0;
        foreach (var id in ids)
        {
            var order = await LoadAsync(id);
            if (order == null || order.Status != OrderStatus.PendingPayment) continue;
            var r = await ApplyTransitionAsync(order, OrderStatus.Cancelled, "Expirado por falta de pagamento", Actor.System);
            if (r.Success) count++;
            _db.ChangeTracker.Clear();
        }
        return count;
    }
}

/// <summary>Cancela pedidos não pagos após o prazo configurado (devolvendo as keys reservadas) e conclui entregas pendentes.</summary>
public class OrderExpirationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<OrderExpirationService> _logger;

    public OrderExpirationService(IServiceScopeFactory scopes, ILogger<OrderExpirationService> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); } catch (OperationCanceledException) { return; }
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var orders = scope.ServiceProvider.GetRequiredService<IOrderService>();
                var n = await orders.CancelExpiredAsync();
                if (n > 0) _logger.LogInformation("{Count} pedido(s) expirado(s) cancelado(s).", n);
                var delivered = await orders.DeliverPendingAsync();
                if (delivered > 0) _logger.LogInformation("{Count} pedido(s) pago(s) tiveram as keys entregues pela tarefa de fundo.", delivered);
            }
            catch (Exception ex) { _logger.LogError(ex, "Erro ao expirar pedidos."); }

            try { await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); } catch (OperationCanceledException) { break; }
        }
    }
}
