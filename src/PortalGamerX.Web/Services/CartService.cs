using System.Security.Cryptography;
using PortalGamerX.Data;
using PortalGamerX.Domain.Entities;
using PortalGamerX.Domain.Rules;
using PortalGamerX.Infrastructure;
using PortalGamerX.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace PortalGamerX.Services;

public interface ICartService
{
    Task<CartView> GetCartAsync();
    Task<int> CountAsync();
    Task<CartResult> AddAsync(int jogoId, int quantity);
    Task<CartResult> SetQuantityAsync(int jogoId, int quantity);
    Task<CartResult> RemoveAsync(int jogoId);
    Task<CartResult> ApplyCouponAsync(string code);
    Task RemoveCouponAsync();
    Task MergeAnonymousIntoUserAsync(Guid userId, string? anonymousToken);
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

        var ids = cart.Items.Select(i => i.JogoId).ToList();
        var jogos = await _db.Jogos.AsNoTracking()
            .Include(p => p.Images)
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var pricing = new List<PricingLine>();
        foreach (var item in cart.Items.OrderBy(i => i.Id))
        {
            if (!jogos.TryGetValue(item.JogoId, out var p)) continue;
            var line = new CartLineView
            {
                JogoId = p.Id,
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
            if (!p.IsActive || !p.IsAvailable) line.Issue = "Jogo indisponível no momento. Remova-o para continuar.";
            else if (p.StockQuantity <= 0) line.Issue = "Esgotado: sem keys disponíveis. Remova-o para continuar.";
            else if (item.Quantity > p.StockQuantity) line.Issue = $"Keys insuficientes: restam {p.StockQuantity}. Ajuste a quantidade.";
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

        var totals = PricingCalculator.Calculate(pricing, discount);
        view.Subtotal = totals.Subtotal;
        view.Discount = totals.Discount;
        view.Total = totals.Total;
        return view;
    }

    public async Task<CartResult> AddAsync(int jogoId, int quantity)
    {
        if (quantity < 1) return new CartResult(false, "Quantidade inválida.");
        var jogo = await _db.Jogos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == jogoId);
        if (jogo == null || !jogo.IsActive || !jogo.IsAvailable) return new CartResult(false, "Jogo indisponível.");
        if (jogo.StockQuantity <= 0) return new CartResult(false, "Jogo esgotado (sem keys disponíveis).");

        var settings = await _store.GetAsync();
        var cart = await FindCartAsync(true);
        var item = cart!.Items.FirstOrDefault(i => i.JogoId == jogoId);
        var newQty = (item?.Quantity ?? 0) + quantity;
        var max = Math.Min(jogo.StockQuantity, settings.MaxItemQuantityPerJogo);
        if (newQty > max) return new CartResult(false, $"Quantidade máxima disponível para este jogo: {max}.");

        if (item == null) _db.CartItems.Add(new CartItem { CartId = cart.Id, JogoId = jogoId, Quantity = newQty });
        else item.Quantity = newQty;
        cart.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new CartResult(true);
    }

    public async Task<CartResult> SetQuantityAsync(int jogoId, int quantity)
    {
        var cart = await FindCartAsync(false);
        var item = cart?.Items.FirstOrDefault(i => i.JogoId == jogoId);
        if (cart == null || item == null) return new CartResult(false, "Item não encontrado no carrinho.");
        if (quantity <= 0) return await RemoveAsync(jogoId);

        var jogo = await _db.Jogos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == jogoId);
        if (jogo == null || !jogo.IsActive || !jogo.IsAvailable) return new CartResult(false, "Jogo indisponível.");
        var settings = await _store.GetAsync();
        var max = Math.Min(jogo.StockQuantity, settings.MaxItemQuantityPerJogo);
        if (quantity > max) return new CartResult(false, $"Quantidade máxima disponível para este jogo: {max}.");

        item.Quantity = quantity;
        cart.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new CartResult(true);
    }

    public async Task<CartResult> RemoveAsync(int jogoId)
    {
        var cart = await FindCartAsync(false);
        var item = cart?.Items.FirstOrDefault(i => i.JogoId == jogoId);
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

        var ids = cart.Items.Select(i => i.JogoId).ToList();
        var jogos = await _db.Jogos.AsNoTracking().Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        var lines = cart.Items.Where(i => jogos.ContainsKey(i.JogoId))
            .Select(i => new PricingLine(i.JogoId, jogos[i.JogoId].CategoryId, jogos[i.JogoId].Sku, jogos[i.JogoId].EffectivePrice, i.Quantity))
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
                var existing = userCart.Items.FirstOrDefault(i => i.JogoId == item.JogoId);
                if (existing == null) _db.CartItems.Add(new CartItem { CartId = userCart.Id, JogoId = item.JogoId, Quantity = item.Quantity });
                else existing.Quantity = Math.Max(existing.Quantity, item.Quantity);
            }
            userCart.CouponCode ??= anon.CouponCode;
            _db.Carts.Remove(anon);
        }
        await _db.SaveChangesAsync();
    }
}
