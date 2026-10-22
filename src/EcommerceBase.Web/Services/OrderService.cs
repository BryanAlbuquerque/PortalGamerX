using EcommerceBase.Web.Data;
using EcommerceBase.Web.Domain;
using EcommerceBase.Web.Domain.Entities;
using EcommerceBase.Web.Domain.Rules;
using EcommerceBase.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EcommerceBase.Web.Services;

public interface IOrderService
{
    /// <summary>Transição manual (administrador). "Pago" não é permitido aqui: só o fluxo de pagamento confirmado marca como pago.</summary>
    Task<OperationResult> ChangeStatusAsync(Guid orderId, OrderStatus target, string? note, Actor actor, string? trackingCode = null);
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

    public async Task<OperationResult> ChangeStatusAsync(Guid orderId, OrderStatus target, string? note, Actor actor, string? trackingCode = null)
    {
        var order = await LoadAsync(orderId);
        if (order == null) return OperationResult.Fail("Pedido não encontrado.");
        if (target == OrderStatus.Paid) return OperationResult.Fail("O status \"Pago\" só é definido após a confirmação do pagamento pelo provedor.");
        if (!OrderStateMachine.AdminTargets(order.Status).Contains(target))
            return OperationResult.Fail($"Transição não permitida: {order.Status.Label()} → {target.Label()}.");
        if (target == OrderStatus.Shipped && !string.IsNullOrWhiteSpace(trackingCode)) order.TrackingCode = trackingCode.Trim().Truncate(60);
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

        if (target == OrderStatus.Cancelled) await ReleaseOrderResourcesAsync(order);

        await TryNotifyAsync(order, target);
        return OperationResult.Ok();
    }

    /// <summary>Devolve estoque e libera o uso do cupom de um pedido cancelado.</summary>
    private async Task ReleaseOrderResourcesAsync(Order order)
    {
        foreach (var item in order.Items)
            await _stock.ReleaseAsync(item.ProductId, item.Quantity, order.Id, $"Cancelamento do pedido {order.Number}");

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
            var vars = new Dictionary<string, string>
            {
                ["OrderNumber"] = order.Number,
                ["Status"] = target.Label(),
                ["Total"] = order.Total.ToBrl(),
                ["Link"] = _url.Absolute("/conta/pedidos/" + order.Id),
                ["Tracking"] = !string.IsNullOrWhiteSpace(order.TrackingCode) ? "Código de rastreio: " + order.TrackingCode : string.Empty
            };
            var kind = target == OrderStatus.Paid ? EmailKinds.PaymentUpdate : EmailKinds.ShippingUpdate;
            if (target is OrderStatus.Paid or OrderStatus.Processing or OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Cancelled)
                await _email.EnqueueAsync(kind, order.CustomerEmail, order.CustomerName, vars);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enfileirar e-mail do pedido {Order}.", order.Number);
        }
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

/// <summary>Cancela pedidos não pagos após o prazo configurado, devolvendo o estoque reservado.</summary>
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
            }
            catch (Exception ex) { _logger.LogError(ex, "Erro ao expirar pedidos."); }

            try { await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); } catch (OperationCanceledException) { break; }
        }
    }
}
