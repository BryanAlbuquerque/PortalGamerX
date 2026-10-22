using EcommerceBase.Web.Data;
using EcommerceBase.Web.Domain;
using EcommerceBase.Web.Domain.Entities;
using EcommerceBase.Web.Domain.Rules;
using EcommerceBase.Web.Infrastructure;
using EcommerceBase.Web.Integrations.Shipping;
using Microsoft.EntityFrameworkCore;

namespace EcommerceBase.Web.Services;

public record PlaceOrderRequest(Guid UserId, Guid IdempotencyKey, int AddressId, string ShippingServiceCode, string? Notes);

public record PlaceOrderResult(bool Success, Guid? OrderId, string? Error, bool Duplicate = false)
{
    public static PlaceOrderResult Fail(string error) => new(false, null, error);
}

public interface ICheckoutService
{
    /// <summary>Cotação de frete a partir do CARRINHO do usuário e do endereço (valores calculados no servidor).</summary>
    Task<ShippingQuoteResult> QuoteShippingAsync(Guid userId, int addressId);
    Task<PlaceOrderResult> PlaceOrderAsync(PlaceOrderRequest request);
}

public class CheckoutService : ICheckoutService
{
    private readonly AppDbContext _db;
    private readonly ICouponService _coupons;
    private readonly IShippingService _shipping;
    private readonly IStockService _stock;
    private readonly IStoreSettingsService _store;
    private readonly IEmailService _email;
    private readonly IPublicUrl _url;
    private readonly ILogger<CheckoutService> _logger;

    public CheckoutService(AppDbContext db, ICouponService coupons, IShippingService shipping, IStockService stock,
        IStoreSettingsService store, IEmailService email, IPublicUrl url, ILogger<CheckoutService> logger)
    {
        _db = db;
        _coupons = coupons;
        _shipping = shipping;
        _stock = stock;
        _store = store;
        _email = email;
        _url = url;
        _logger = logger;
    }

    private sealed record CartSnapshot(Cart Cart, Dictionary<int, Product> Products, List<PricingLine> Lines, decimal Discount, string? CouponCode, int? CouponId, string? Error);

    private async Task<CartSnapshot> LoadCartAsync(Guid userId)
    {
        var cart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart == null || cart.Items.Count == 0)
            return new CartSnapshot(cart ?? new Cart(), new(), new(), 0, null, null, "Seu carrinho está vazio.");

        var ids = cart.Items.Select(i => i.ProductId).ToList();
        var products = await _db.Products.AsNoTracking().Include(p => p.Images)
            .Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        var lines = new List<PricingLine>();
        foreach (var item in cart.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var p))
                return new CartSnapshot(cart, products, lines, 0, null, null, "Um item do carrinho não existe mais. Revise o carrinho.");
            if (!p.IsActive || !p.IsAvailable)
                return new CartSnapshot(cart, products, lines, 0, null, null, $"O produto \"{p.Name}\" está indisponível.");
            if (item.Quantity > p.StockQuantity)
                return new CartSnapshot(cart, products, lines, 0, null, null, $"Estoque insuficiente para \"{p.Name}\" (disponível: {p.StockQuantity}).");
            lines.Add(new PricingLine(p.Id, p.CategoryId, p.Sku, p.EffectivePrice, item.Quantity));
        }

        decimal discount = 0;
        string? couponCode = null;
        int? couponId = null;
        if (!string.IsNullOrWhiteSpace(cart.CouponCode))
        {
            var eval = await _coupons.EvaluateAsync(cart.CouponCode, lines, userId);
            if (!eval.IsValid) return new CartSnapshot(cart, products, lines, 0, null, null, $"Cupom: {eval.Error}");
            discount = eval.Discount;
            couponCode = CouponService.Normalize(cart.CouponCode);
            couponId = await _coupons.FindCouponIdAsync(couponCode);
        }
        return new CartSnapshot(cart, products, lines, discount, couponCode, couponId, null);
    }

    private static List<ShippingPackageItem> ToPackage(CartSnapshot snap) =>
        snap.Lines.Select(l =>
        {
            var p = snap.Products[l.ProductId];
            return new ShippingPackageItem(p.Sku, l.Quantity, p.WeightKg, p.HeightCm, p.WidthCm, p.LengthCm, l.UnitPrice);
        }).ToList();

    public async Task<ShippingQuoteResult> QuoteShippingAsync(Guid userId, int addressId)
    {
        var address = await _db.Addresses.AsNoTracking().FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);
        if (address == null) return ShippingQuoteResult.Fail("Endereço inválido.");
        var snap = await LoadCartAsync(userId);
        if (snap.Error != null) return ShippingQuoteResult.Fail(snap.Error);
        var subtotal = PricingCalculator.Calculate(snap.Lines, snap.Discount, 0m);
        return await _shipping.QuoteAsync(address.PostalCode, ToPackage(snap), subtotal.Subtotal - subtotal.Discount);
    }

    public async Task<PlaceOrderResult> PlaceOrderAsync(PlaceOrderRequest req)
    {
        // 1) Idempotência: a mesma chave nunca gera dois pedidos.
        var existing = await _db.Orders.AsNoTracking()
            .Where(o => o.IdempotencyKey == req.IdempotencyKey)
            .Select(o => new { o.Id, o.UserId }).FirstOrDefaultAsync();
        if (existing != null)
            return existing.UserId == req.UserId ? new PlaceOrderResult(true, existing.Id, null, true) : PlaceOrderResult.Fail("Requisição inválida.");

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == req.UserId);
        if (user == null || !user.IsActive) return PlaceOrderResult.Fail("Conta indisponível.");

        var address = await _db.Addresses.AsNoTracking().FirstOrDefaultAsync(a => a.Id == req.AddressId && a.UserId == req.UserId);
        if (address == null) return PlaceOrderResult.Fail("Selecione um endereço de entrega válido.");

        // 2) Recalcula tudo no servidor a partir do banco.
        var snap = await LoadCartAsync(req.UserId);
        if (snap.Error != null) return PlaceOrderResult.Fail(snap.Error);

        var preTotals = PricingCalculator.Calculate(snap.Lines, snap.Discount, 0m);
        var quote = await _shipping.QuoteAsync(address.PostalCode, ToPackage(snap), preTotals.Subtotal - preTotals.Discount);
        if (!quote.Success) return PlaceOrderResult.Fail(quote.Error ?? "Frete indisponível.");
        var option = quote.Options.FirstOrDefault(o => o.Code == req.ShippingServiceCode);
        if (option == null) return PlaceOrderResult.Fail("Modalidade de frete inválida ou indisponível. Recalcule o frete.");

        var totals = PricingCalculator.Calculate(snap.Lines, snap.Discount, option.Price);
        var settings = await _store.GetAsync();
        var now = DateTime.UtcNow;

        var number = OrderNumberGenerator.Generate(now);
        for (var i = 0; i < 5 && await _db.Orders.AnyAsync(o => o.Number == number); i++) number = OrderNumberGenerator.Generate(now);

        var order = new Order
        {
            Number = number,
            IdempotencyKey = req.IdempotencyKey,
            UserId = req.UserId,
            Status = OrderStatus.PendingPayment,
            PaymentStatus = PaymentStatus.Pending,
            Subtotal = totals.Subtotal,
            DiscountTotal = totals.Discount,
            ShippingTotal = totals.Shipping,
            Total = totals.Total,
            CouponCode = snap.CouponCode,
            ShippingServiceCode = option.Code,
            ShippingServiceName = option.Name,
            ShippingCarrier = option.Carrier,
            ShippingDeliveryDays = option.DeliveryDays,
            ShippingIsSimulated = option.IsSimulated,
            CustomerName = string.IsNullOrWhiteSpace(user.FullName) ? address.RecipientName : user.FullName,
            CustomerEmail = user.Email ?? string.Empty,
            CustomerPhone = user.PhoneNumber ?? address.Phone,
            CustomerDocument = user.Document,
            ShipRecipient = address.RecipientName,
            ShipPostalCode = address.PostalCode,
            ShipStreet = address.Street,
            ShipNumber = address.Number,
            ShipComplement = address.Complement,
            ShipDistrict = address.District,
            ShipCity = address.City,
            ShipState = address.State,
            CustomerNotes = req.Notes.NullIfEmpty()?.Truncate(1000),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(Math.Max(settings.PendingOrderExpirationMinutes, 15))
        };
        foreach (var l in snap.Lines)
        {
            var p = snap.Products[l.ProductId];
            order.Items.Add(new OrderItem
            {
                ProductId = p.Id,
                ProductName = p.Name,
                Sku = p.Sku,
                ImagePath = p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder).Select(i => i.Path).FirstOrDefault(),
                UnitPrice = l.UnitPrice,
                Quantity = l.Quantity
            });
        }
        order.History.Add(new OrderStatusHistory
        {
            FromStatus = null,
            ToStatus = OrderStatus.PendingPayment,
            Note = "Pedido criado",
            ChangedByUserId = req.UserId,
            ChangedByName = order.CustomerName
        });

        // 3) Transação: pedido + baixa atômica de estoque + cupom + limpeza do carrinho.
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            foreach (var item in order.Items)
            {
                var ok = await _stock.TryReserveAsync(item.ProductId, item.Quantity, order.Id, $"Reserva do pedido {order.Number}");
                if (!ok)
                {
                    await tx.RollbackAsync();
                    _db.ChangeTracker.Clear();
                    return PlaceOrderResult.Fail($"Estoque insuficiente para \"{item.ProductName}\". Revise seu carrinho.");
                }
            }

            if (snap.CouponId.HasValue)
            {
                _db.CouponUsages.Add(new CouponUsage { CouponId = snap.CouponId.Value, UserId = req.UserId, OrderId = order.Id, DiscountAmount = totals.Discount });
                await _db.SaveChangesAsync();

                var limits = await _db.Coupons.AsNoTracking().Where(c => c.Id == snap.CouponId.Value)
                    .Select(c => new { c.MaxUses, c.MaxUsesPerCustomer }).FirstAsync();
                var total = await _db.CouponUsages.CountAsync(u => u.CouponId == snap.CouponId.Value);
                var mine = await _db.CouponUsages.CountAsync(u => u.CouponId == snap.CouponId.Value && u.UserId == req.UserId);
                if ((limits.MaxUses.HasValue && total > limits.MaxUses.Value) || (limits.MaxUsesPerCustomer.HasValue && mine > limits.MaxUsesPerCustomer.Value))
                {
                    await tx.RollbackAsync();
                    _db.ChangeTracker.Clear();
                    return PlaceOrderResult.Fail("O cupom atingiu o limite de utilização. Remova-o e tente novamente.");
                }
            }

            var cart = await _db.Carts.Include(c => c.Items).FirstAsync(c => c.UserId == req.UserId);
            _db.CartItems.RemoveRange(cart.Items);
            cart.CouponCode = null;
            cart.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await tx.CommitAsync();
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync();
            _db.ChangeTracker.Clear();
            // Corrida na mesma chave de idempotência: devolve o pedido já criado.
            var dup = await _db.Orders.AsNoTracking().Where(o => o.IdempotencyKey == req.IdempotencyKey && o.UserId == req.UserId)
                .Select(o => (Guid?)o.Id).FirstOrDefaultAsync();
            if (dup.HasValue) return new PlaceOrderResult(true, dup, null, true);
            _logger.LogError(ex, "Falha ao gravar pedido.");
            return PlaceOrderResult.Fail("Não foi possível registrar o pedido. Tente novamente.");
        }

        await NotifyAsync(order, settings);
        return new PlaceOrderResult(true, order.Id, null);
    }

    private async Task NotifyAsync(Order order, StoreSettings settings)
    {
        try
        {
            var vars = new Dictionary<string, string>
            {
                ["OrderNumber"] = order.Number,
                ["Total"] = order.Total.ToBrl(),
                ["Link"] = _url.Absolute("/conta/pedidos/" + order.Id)
            };
            await _email.EnqueueAsync(EmailKinds.OrderConfirmation, order.CustomerEmail, order.CustomerName, vars);

            if (!string.IsNullOrWhiteSpace(settings.Email))
            {
                var adminVars = new Dictionary<string, string>(vars)
                {
                    ["Link"] = _url.Absolute("/admin/orders/details/" + order.Id),
                    ["CustomerName"] = order.CustomerName
                };
                await _email.EnqueueAsync(EmailKinds.AdminNewOrder, settings.Email!, settings.StoreName, adminVars);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enfileirar e-mails do pedido {Order}.", order.Number);
        }
    }
}
