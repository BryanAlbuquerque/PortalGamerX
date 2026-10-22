using System.Security.Cryptography;
using EcommerceBase.Web.Data;
using EcommerceBase.Web.Domain.Entities;
using EcommerceBase.Web.Domain.Rules;
using EcommerceBase.Web.Infrastructure;
using EcommerceBase.Web.Integrations.Shipping;
using EcommerceBase.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EcommerceBase.Web.Services;

public interface ICartService
{
    Task<CartView> GetCartAsync();
    Task<int> CountAsync();
    Task<CartResult> AddAsync(int productId, int quantity);
    Task<CartResult> SetQuantityAsync(int productId, int quantity);
    Task<CartResult> RemoveAsync(int productId);
    Task<CartResult> ApplyCouponAsync(string code);
    Task RemoveCouponAsync();
    Task MergeAnonymousIntoUserAsync(Guid userId, string? anonymousToken);
    /// <summary>Itens do carrinho atual (dimensões/peso do banco) e subtotal já com desconto, para estimar frete.</summary>
    Task<(List<ShippingPackageItem> Items, decimal Subtotal)> GetShippingPackageAsync();
}

public class CartService : ICartService
{
    public const string CookieName = "eb_cart";
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly ICouponService _coupons;
    private readonly IStoreSettingsService _store;

    public CartService(AppDbContext db, IHttpContextAccessor http, ICouponService coupons, IStoreSettingsService store)
    {
        _db = db;
        _http = http;
        _coupons = coupons;
        _store = store;
    }

    private HttpContext Ctx => _http.HttpContext ?? throw new InvalidOperationException("Sem contexto HTTP.");
    private Guid? UserId => Ctx.User.GetUserId();

    private async Task<Cart?> FindCartAsync(bool create)
    {
        var uid = UserId;
        if (uid.HasValue)
        {
            var c = await _db.Carts.Include(x => x.Items).FirstOrDefaultAsync(x => x.UserId == uid);
            if (c == null && create)
            {
                c = new Cart { UserId = uid };
                _db.Carts.Add(c);
                await _db.SaveChangesAsync();
            }
            return c;
        }

        var token = Ctx.Request.Cookies[CookieName];
        Cart? cart = null;
        if (!string.IsNullOrEmpty(token) && token.Length <= 64)
            cart = await _db.Carts.Include(x => x.Items).FirstOrDefaultAsync(x => x.AnonymousToken == token);

        if (cart == null && create)
        {
            token = RandomNumberGenerator.GetBytes(32).ToBase64Url();
            cart = new Cart { AnonymousToken = token };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
            Ctx.Response.Cookies.Append(CookieName, token, new CookieOptions
            {
                HttpOnly = true,
                Secure = Ctx.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromDays(30),
                IsEssential = true
            });
        }
        return cart;
    }

    public async Task<int> CountAsync()
    {
        var cart = await FindCartAsync(false);
        return cart?.Items.Sum(i => i.Quantity) ?? 0;
    }

    public async Task<CartView> GetCartAsync()
    {
        var view = new CartView();
        var cart = await FindCartAsync(false);
        if (cart == null || cart.Items.Count == 0) return view;

        var ids = cart.Items.Select(i => i.ProductId).ToList();
        var products = await _db.Products.AsNoTracking()
            .Include(p => p.Images)
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var pricing = new List<PricingLine>();
        foreach (var item in cart.Items.OrderBy(i => i.Id))
        {
            if (!products.TryGetValue(item.ProductId, out var p)) continue;
            var line = new CartLineView
            {
                ProductId = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                Sku = p.Sku,
                ImagePath = p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder).Select(i => i.Path).FirstOrDefault(),
                UnitPrice = p.EffectivePrice,
                OriginalPrice = p.Price,
                Quantity = item.Quantity,
                AvailableStock = p.StockQuantity,
                LineTotal = Money.Round(p.EffectivePrice * item.Quantity)
            };
            if (!p.IsActive || !p.IsAvailable) line.Issue = "Produto indisponível no momento. Remova-o para continuar.";
            else if (p.StockQuantity <= 0) line.Issue = "Produto sem estoque. Remova-o para continuar.";
            else if (item.Quantity > p.StockQuantity) line.Issue = $"Estoque insuficiente: restam {p.StockQuantity} unidade(s). Ajuste a quantidade.";
            view.Lines.Add(line);
            pricing.Add(new PricingLine(p.Id, p.CategoryId, p.Sku, p.EffectivePrice, item.Quantity));
        }

        decimal discount = 0;
        if (!string.IsNullOrWhiteSpace(cart.CouponCode))
        {
            var eval = await _coupons.EvaluateAsync(cart.CouponCode, pricing, UserId);
            if (eval.IsValid) { discount = eval.Discount; view.CouponCode = cart.CouponCode; }
            else view.CouponError = eval.Error;
        }

        var totals = PricingCalculator.Calculate(pricing, discount, 0m);
        view.Subtotal = totals.Subtotal;
        view.Discount = totals.Discount;
        view.Total = totals.Total;
        return view;
    }

    public async Task<CartResult> AddAsync(int productId, int quantity)
    {
        if (quantity < 1) return new CartResult(false, "Quantidade inválida.");
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null || !product.IsActive || !product.IsAvailable) return new CartResult(false, "Produto indisponível.");
        if (product.StockQuantity <= 0) return new CartResult(false, "Produto sem estoque.");

        var settings = await _store.GetAsync();
        var cart = await FindCartAsync(true);
        var item = cart!.Items.FirstOrDefault(i => i.ProductId == productId);
        var newQty = (item?.Quantity ?? 0) + quantity;
        var max = Math.Min(product.StockQuantity, settings.MaxItemQuantityPerProduct);
        if (newQty > max) return new CartResult(false, $"Quantidade máxima disponível para este produto: {max}.");

        if (item == null) _db.CartItems.Add(new CartItem { CartId = cart.Id, ProductId = productId, Quantity = newQty });
        else item.Quantity = newQty;
        cart.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new CartResult(true);
    }

    public async Task<CartResult> SetQuantityAsync(int productId, int quantity)
    {
        var cart = await FindCartAsync(false);
        var item = cart?.Items.FirstOrDefault(i => i.ProductId == productId);
        if (cart == null || item == null) return new CartResult(false, "Item não encontrado no carrinho.");
        if (quantity <= 0) return await RemoveAsync(productId);

        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null || !product.IsActive || !product.IsAvailable) return new CartResult(false, "Produto indisponível.");
        var settings = await _store.GetAsync();
        var max = Math.Min(product.StockQuantity, settings.MaxItemQuantityPerProduct);
        if (quantity > max) return new CartResult(false, $"Quantidade máxima disponível para este produto: {max}.");

        item.Quantity = quantity;
        cart.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new CartResult(true);
    }

    public async Task<CartResult> RemoveAsync(int productId)
    {
        var cart = await FindCartAsync(false);
        var item = cart?.Items.FirstOrDefault(i => i.ProductId == productId);
        if (cart == null || item == null) return new CartResult(true);
        _db.CartItems.Remove(item);
        cart.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new CartResult(true);
    }

    public async Task<CartResult> ApplyCouponAsync(string code)
    {
        var cart = await FindCartAsync(false);
        if (cart == null || cart.Items.Count == 0) return new CartResult(false, "Seu carrinho está vazio.");

        var ids = cart.Items.Select(i => i.ProductId).ToList();
        var products = await _db.Products.AsNoTracking().Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        var lines = cart.Items.Where(i => products.ContainsKey(i.ProductId))
            .Select(i => new PricingLine(i.ProductId, products[i.ProductId].CategoryId, products[i.ProductId].Sku, products[i.ProductId].EffectivePrice, i.Quantity))
            .ToList();

        var eval = await _coupons.EvaluateAsync(code, lines, UserId);
        if (!eval.IsValid) return new CartResult(false, eval.Error);

        cart.CouponCode = CouponService.Normalize(code);
        cart.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new CartResult(true);
    }

    public async Task RemoveCouponAsync()
    {
        var cart = await FindCartAsync(false);
        if (cart == null) return;
        cart.CouponCode = null;
        await _db.SaveChangesAsync();
    }

    /// <summary>Chamado após o login: junta o carrinho anônimo (cookie) ao carrinho do usuário.</summary>
    public async Task MergeAnonymousIntoUserAsync(Guid userId, string? anonymousToken)
    {
        if (string.IsNullOrEmpty(anonymousToken) || anonymousToken.Length > 64) return;
        var anon = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.AnonymousToken == anonymousToken);
        if (anon == null) return;

        var userCart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
        if (userCart == null)
        {
            anon.UserId = userId;
            anon.AnonymousToken = null;
        }
        else
        {
            foreach (var item in anon.Items)
            {
                var existing = userCart.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
                if (existing == null) _db.CartItems.Add(new CartItem { CartId = userCart.Id, ProductId = item.ProductId, Quantity = item.Quantity });
                else existing.Quantity = Math.Max(existing.Quantity, item.Quantity);
            }
            userCart.CouponCode ??= anon.CouponCode;
            _db.Carts.Remove(anon);
        }
        await _db.SaveChangesAsync();
    }

    public async Task<(List<ShippingPackageItem> Items, decimal Subtotal)> GetShippingPackageAsync()
    {
        var view = await GetCartAsync();
        if (view.IsEmpty) return (new List<ShippingPackageItem>(), 0m);
        var ids = view.Lines.Select(l => l.ProductId).ToList();
        var products = await _db.Products.AsNoTracking().Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        var items = view.Lines.Where(l => products.ContainsKey(l.ProductId)).Select(l =>
        {
            var p = products[l.ProductId];
            return new ShippingPackageItem(p.Sku, l.Quantity, p.WeightKg, p.HeightCm, p.WidthCm, p.LengthCm, l.UnitPrice);
        }).ToList();
        return (items, view.Subtotal - view.Discount);
    }
}
