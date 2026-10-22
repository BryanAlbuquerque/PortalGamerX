using EcommerceBase.Web.Data;
using EcommerceBase.Web.Domain;
using EcommerceBase.Web.Domain.Entities;
using EcommerceBase.Web.Infrastructure;
using EcommerceBase.Web.Services;
using EcommerceBase.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcommerceBase.Web.Areas.Admin.Controllers;

public class CouponsController : AdminBaseController
{
    private readonly AppDbContext _db;
    private readonly IStoreSettingsService _store;
    private readonly IAuditService _audit;

    public CouponsController(AppDbContext db, IStoreSettingsService store, IAuditService audit)
    {
        _db = db;
        _store = store;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _db.Coupons.AsNoTracking().OrderByDescending(c => c.Id).ToListAsync();
        ViewBag.Usage = await _db.CouponUsages.GroupBy(u => u.CouponId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        return View(items);
    }

    private async Task FillAsync(CouponForm f)
    {
        f.Categories = await _db.Categories.AsNoTracking().OrderBy(c => c.Name).Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToListAsync();
    }

    public async Task<IActionResult> Create()
    {
        var f = new CouponForm();
        await FillAsync(f);
        return View("Form", f);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CouponForm f)
    {
        var skus = ParseSkus(f.Skus);
        var products = await ResolveProductsAsync(skus, f);
        await ValidateAsync(f);
        if (!ModelState.IsValid) { await FillAsync(f); return View("Form", f); }

        var c = new Coupon();
        await ApplyAsync(c, f, products);
        _db.Coupons.Add(c);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("coupon.create", "Coupon", c.Id.ToString(), c.Code);
        TempData["Success"] = "Cupom criado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var c = await _db.Coupons.AsNoTracking().Include(x => x.Categories).Include(x => x.Products).FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();
        var s = await _store.GetAsync();
        var f = new CouponForm
        {
            Id = c.Id, Code = c.Code, Description = c.Description, DiscountType = c.DiscountType, Value = c.Value, MaxDiscountAmount = c.MaxDiscountAmount,
            StartsAtLocal = c.StartsAtUtc.HasValue ? _store.ToLocal(c.StartsAtUtc.Value, s) : null,
            EndsAtLocal = c.EndsAtUtc.HasValue ? _store.ToLocal(c.EndsAtUtc.Value, s) : null,
            MinOrderAmount = c.MinOrderAmount, MaxUses = c.MaxUses, MaxUsesPerCustomer = c.MaxUsesPerCustomer, IsActive = c.IsActive,
            CategoryIds = c.Categories.Select(x => x.Id).ToList(),
            Skus = string.Join(", ", c.Products.Select(p => p.Sku))
        };
        await FillAsync(f);
        return View("Form", f);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CouponForm f)
    {
        var c = await _db.Coupons.Include(x => x.Categories).Include(x => x.Products).FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();
        f.Id = id;
        var products = await ResolveProductsAsync(ParseSkus(f.Skus), f);
        await ValidateAsync(f);
        if (!ModelState.IsValid) { await FillAsync(f); return View("Form", f); }
        await ApplyAsync(c, f, products);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("coupon.update", "Coupon", id.ToString(), c.Code);
        TempData["Success"] = "Cupom atualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var c = await _db.Coupons.FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();
        c.IsActive = !c.IsActive;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(c.IsActive ? "coupon.activate" : "coupon.deactivate", "Coupon", id.ToString(), c.Code);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Coupons.FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();
        if (await _db.CouponUsages.AnyAsync(u => u.CouponId == id))
        {
            TempData["Error"] = "O cupom já foi utilizado e não pode ser excluído. Desative-o.";
            return RedirectToAction(nameof(Index));
        }
        _db.Coupons.Remove(c);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("coupon.delete", "Coupon", id.ToString(), c.Code);
        TempData["Success"] = "Cupom excluído.";
        return RedirectToAction(nameof(Index));
    }

    private static List<string> ParseSkus(string? raw) =>
        (raw ?? string.Empty).Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private async Task<List<Product>> ResolveProductsAsync(List<string> skus, CouponForm f)
    {
        if (skus.Count == 0) return new List<Product>();
        var found = await _db.Products.Where(p => skus.Contains(p.Sku)).ToListAsync();
        var missing = skus.Where(s => !found.Any(p => p.Sku.Equals(s, StringComparison.OrdinalIgnoreCase))).ToList();
        if (missing.Count > 0) ModelState.AddModelError(nameof(f.Skus), "SKUs não encontrados: " + string.Join(", ", missing));
        return found;
    }

    private async Task ValidateAsync(CouponForm f)
    {
        var code = f.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (code.Length > 0 && await _db.Coupons.AnyAsync(c => c.Code == code && c.Id != f.Id))
            ModelState.AddModelError(nameof(f.Code), "Já existe um cupom com este código.");
    }

    private async Task ApplyAsync(Coupon c, CouponForm f, List<Product> products)
    {
        var s = await _store.GetAsync();
        c.Code = f.Code.Trim().ToUpperInvariant();
        c.Description = f.Description.NullIfEmpty();
        c.DiscountType = f.DiscountType;
        c.Value = f.Value;
        c.MaxDiscountAmount = f.DiscountType == DiscountType.Percentage ? f.MaxDiscountAmount : null;
        c.StartsAtUtc = f.StartsAtLocal.HasValue ? _store.ToUtc(f.StartsAtLocal.Value, s) : null;
        c.EndsAtUtc = f.EndsAtLocal.HasValue ? _store.ToUtc(f.EndsAtLocal.Value, s) : null;
        c.MinOrderAmount = f.MinOrderAmount;
        c.MaxUses = f.MaxUses;
        c.MaxUsesPerCustomer = f.MaxUsesPerCustomer;
        c.IsActive = f.IsActive;

        c.Categories.Clear();
        if (f.CategoryIds.Count > 0)
            c.Categories.AddRange(await _db.Categories.Where(x => f.CategoryIds.Contains(x.Id)).ToListAsync());
        c.Products.Clear();
        c.Products.AddRange(products);
    }
}
