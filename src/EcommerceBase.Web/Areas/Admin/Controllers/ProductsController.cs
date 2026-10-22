using EcommerceBase.Web.Data;
using EcommerceBase.Web.Domain;
using EcommerceBase.Web.Domain.Entities;
using EcommerceBase.Web.Domain.Rules;
using EcommerceBase.Web.Infrastructure;
using EcommerceBase.Web.Services;
using EcommerceBase.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcommerceBase.Web.Areas.Admin.Controllers;

public class ProductsController : AdminBaseController
{
    private readonly AppDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IAuditService _audit;
    private readonly IStockService _stock;

    public ProductsController(AppDbContext db, IFileStorage storage, IAuditService audit, IStockService stock)
    {
        _db = db;
        _storage = storage;
        _audit = audit;
        _stock = stock;
    }

    public async Task<IActionResult> Index(string? q, string? filter, int page = 1)
    {
        page = Math.Max(page, 1);
        var query = _db.Products.AsNoTracking().Include(p => p.Category).Include(p => p.Images).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = "%" + q.Trim().EscapeLike() + "%";
            query = query.Where(p => EF.Functions.ILike(p.Name, pattern, "\\") || EF.Functions.ILike(p.Sku, pattern, "\\"));
        }
        query = filter switch
        {
            "active" => query.Where(p => p.IsActive),
            "inactive" => query.Where(p => !p.IsActive),
            "featured" => query.Where(p => p.IsFeatured),
            "nostock" => query.Where(p => p.StockQuantity <= 0),
            _ => query
        };
        var total = await query.CountAsync();
        var items = await query.OrderBy(p => p.Name).Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
        return View(new AdminProductListVm { Items = items, Q = q, Filter = filter, Page = page, Total = total, TotalPages = TotalPagesFor(total) });
    }

    private async Task FillListsAsync(ProductForm f)
    {
        f.Categories = await _db.Categories.AsNoTracking().OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name + (c.IsActive ? "" : " (inativa)"), c.Id.ToString())).ToListAsync();
        f.Brands = await _db.Brands.AsNoTracking().OrderBy(b => b.Name)
            .Select(b => new SelectListItem(b.Name, b.Id.ToString())).ToListAsync();
    }

    public async Task<IActionResult> Create()
    {
        var f = new ProductForm();
        await FillListsAsync(f);
        return View("Form", f);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProductForm f)
    {
        await ValidateUniqueAsync(f);
        if (!ModelState.IsValid) { await FillListsAsync(f); return View("Form", f); }

        var slug = await UniqueSlugAsync(f.Name, null);
        var p = new Product { Slug = slug };
        Apply(p, f);
        p.StockQuantity = 0;
        _db.Products.Add(p);
        await _db.SaveChangesAsync();

        if (f.StockQuantity > 0)
            await _stock.AdminMoveAsync(p.Id, StockMovementType.Entry, f.StockQuantity, "Estoque inicial do cadastro", CurrentUserId, CurrentUserName);

        await _audit.LogAsync("product.create", "Product", p.Id.ToString(), $"{p.Sku} - {p.Name}");
        TempData["Success"] = "Produto criado. Agora você pode enviar as imagens.";
        return RedirectToAction(nameof(Edit), new { id = p.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var p = await _db.Products.AsNoTracking().Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        var f = ToForm(p);
        await FillListsAsync(f);
        return View("Form", f);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ProductForm f)
    {
        var p = await _db.Products.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        f.Id = id;
        await ValidateUniqueAsync(f);
        if (!ModelState.IsValid)
        {
            f.Images = p.Images.OrderBy(i => i.SortOrder).ToList();
            f.CurrentStock = p.StockQuantity;
            await FillListsAsync(f);
            return View("Form", f);
        }

        var oldPrice = p.Price;
        var oldPromo = p.PromotionalPrice;
        Apply(p, f); // estoque NÃO é alterado aqui (apenas por movimentações)
        await _db.SaveChangesAsync();
        await _audit.LogAsync("product.update", "Product", p.Id.ToString(),
            oldPrice != p.Price || oldPromo != p.PromotionalPrice
                ? $"{p.Sku}: preço {oldPrice} -> {p.Price}; promo {oldPromo} -> {p.PromotionalPrice}"
                : p.Sku);
        TempData["Success"] = "Produto atualizado.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        p.IsActive = !p.IsActive;
        p.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(p.IsActive ? "product.activate" : "product.deactivate", "Product", p.Id.ToString(), p.Sku);
        TempData["Success"] = p.IsActive ? "Produto ativado." : "Produto desativado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UploadImages(int id, List<IFormFile> files)
    {
        var p = await _db.Products.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        if (files == null || files.Count == 0) { TempData["Error"] = "Selecione ao menos uma imagem."; return RedirectToAction(nameof(Edit), new { id }); }
        if (p.Images.Count + files.Count > 10) { TempData["Error"] = "Limite de 10 imagens por produto."; return RedirectToAction(nameof(Edit), new { id }); }

        var errors = new List<string>();
        var order = p.Images.Count == 0 ? 0 : p.Images.Max(i => i.SortOrder) + 1;
        foreach (var file in files)
        {
            var r = await _storage.SaveImageAsync(file, "products");
            if (!r.Success) { errors.Add($"{Path.GetFileName(file.FileName)}: {r.Error}"); continue; }
            _db.ProductImages.Add(new ProductImage { ProductId = p.Id, Path = r.Path!, AltText = p.Name, SortOrder = order++, IsPrimary = p.Images.Count == 0 && !_db.ChangeTracker.Entries<ProductImage>().Any(e => e.State == EntityState.Added && e.Entity.IsPrimary) });
        }
        p.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("product.images.upload", "Product", p.Id.ToString());
        if (errors.Count > 0) TempData["Error"] = string.Join(" | ", errors); else TempData["Success"] = "Imagens enviadas.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteImage(int id, int imageId)
    {
        var img = await _db.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == id);
        if (img == null) return NotFound();
        var wasPrimary = img.IsPrimary;
        _db.ProductImages.Remove(img);
        await _db.SaveChangesAsync();
        await _storage.DeleteAsync(img.Path);
        if (wasPrimary)
        {
            var next = await _db.ProductImages.Where(i => i.ProductId == id).OrderBy(i => i.SortOrder).FirstOrDefaultAsync();
            if (next != null) { next.IsPrimary = true; await _db.SaveChangesAsync(); }
        }
        await _audit.LogAsync("product.images.delete", "Product", id.ToString());
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> SetPrimaryImage(int id, int imageId)
    {
        var imgs = await _db.ProductImages.Where(i => i.ProductId == id).ToListAsync();
        if (!imgs.Any(i => i.Id == imageId)) return NotFound();
        foreach (var i in imgs) i.IsPrimary = i.Id == imageId;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id });
    }

    // ---------- helpers ----------
    private static ProductForm ToForm(Product p) => new()
    {
        Id = p.Id, Name = p.Name, ShortDescription = p.ShortDescription, Description = p.Description, Price = p.Price,
        PromotionalPrice = p.PromotionalPrice, Sku = p.Sku, Barcode = p.Barcode, Ncm = p.Ncm, CategoryId = p.CategoryId, BrandId = p.BrandId,
        StockQuantity = p.StockQuantity, CurrentStock = p.StockQuantity, WeightKg = p.WeightKg, HeightCm = p.HeightCm, WidthCm = p.WidthCm,
        LengthCm = p.LengthCm, IsActive = p.IsActive, IsFeatured = p.IsFeatured, IsAvailable = p.IsAvailable,
        Images = p.Images.OrderBy(i => i.SortOrder).ToList()
    };

    /// <summary>Atribuição explícita campo a campo (sem binding direto na entidade).</summary>
    private static void Apply(Product p, ProductForm f)
    {
        p.Name = f.Name.Trim();
        p.ShortDescription = f.ShortDescription.NullIfEmpty();
        p.Description = f.Description.NullIfEmpty();
        p.Price = Money.Round(f.Price);
        p.PromotionalPrice = f.PromotionalPrice.HasValue ? Money.Round(f.PromotionalPrice.Value) : null;
        p.Sku = f.Sku.Trim();
        p.Barcode = f.Barcode.NullIfEmpty();
        p.Ncm = f.Ncm.NullIfEmpty();
        p.CategoryId = f.CategoryId;
        p.BrandId = f.BrandId;
        p.WeightKg = f.WeightKg;
        p.HeightCm = f.HeightCm;
        p.WidthCm = f.WidthCm;
        p.LengthCm = f.LengthCm;
        p.IsActive = f.IsActive;
        p.IsFeatured = f.IsFeatured;
        p.IsAvailable = f.IsAvailable;
        p.UpdatedAtUtc = DateTime.UtcNow;
    }

    private async Task ValidateUniqueAsync(ProductForm f)
    {
        var sku = f.Sku?.Trim() ?? string.Empty;
        if (sku.Length > 0 && await _db.Products.AnyAsync(p => p.Sku == sku && p.Id != f.Id))
            ModelState.AddModelError(nameof(f.Sku), "Já existe um produto com este SKU.");
        if (f.CategoryId > 0 && !await _db.Categories.AnyAsync(c => c.Id == f.CategoryId))
            ModelState.AddModelError(nameof(f.CategoryId), "Categoria inválida.");
        if (f.BrandId.HasValue && !await _db.Brands.AnyAsync(b => b.Id == f.BrandId.Value))
            ModelState.AddModelError(nameof(f.BrandId), "Marca inválida.");
    }

    private async Task<string> UniqueSlugAsync(string name, int? ignoreId)
    {
        var baseSlug = SlugHelper.Generate(name);
        var slug = baseSlug;
        var n = 2;
        while (await _db.Products.AnyAsync(p => p.Slug == slug && p.Id != ignoreId)) slug = $"{baseSlug}-{n++}";
        return slug;
    }
}
