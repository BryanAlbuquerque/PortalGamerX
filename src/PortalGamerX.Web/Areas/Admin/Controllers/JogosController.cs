using PortalGamerX.Data;
using PortalGamerX.Domain;
using PortalGamerX.Domain.Entities;
using PortalGamerX.Domain.Rules;
using PortalGamerX.Infrastructure;
using PortalGamerX.Services;
using PortalGamerX.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace PortalGamerX.Areas.Admin.Controllers;

public class JogosController : AdminBaseController
{
    private readonly AppDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IAuditService _audit;
    private readonly IStockService _stock;

    public JogosController(AppDbContext db, IFileStorage storage, IAuditService audit, IStockService stock)
    {
        _db = db;
        _storage = storage;
        _audit = audit;
        _stock = stock;
    }

    public async Task<IActionResult> Index(string? q, string? filter, int page = 1)
    {
        page = Math.Max(page, 1);
        var query = _db.Jogos.AsNoTracking().Include(p => p.Category).Include(p => p.Images).AsQueryable();
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
        return View(new AdminJogoListVm { Items = items, Q = q, Filter = filter, Page = page, Total = total, TotalPages = TotalPagesFor(total) });
    }

    private async Task FillListsAsync(JogoForm f)
    {
        f.Categories = await _db.Categories.AsNoTracking().OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name + (c.IsActive ? "" : " (inativa)"), c.Id.ToString())).ToListAsync();
        f.Brands = await _db.Brands.AsNoTracking().OrderBy(b => b.Name)
            .Select(b => new SelectListItem(b.Name, b.Id.ToString())).ToListAsync();
    }

    public async Task<IActionResult> Create()
    {
        var f = new JogoForm();
        await FillListsAsync(f);
        return View("Form", f);
    }

    [HttpPost]
    public async Task<IActionResult> Create(JogoForm f)
    {
        await ValidateUniqueAsync(f);
        if (!ModelState.IsValid) { await FillListsAsync(f); return View("Form", f); }

        var slug = await UniqueSlugAsync(f.Name, null);
        var j = new Jogo { Slug = slug };
        Apply(j, f);
        j.StockQuantity = 0;
        _db.Jogos.Add(j);
        await _db.SaveChangesAsync();

        if (f.StockQuantity > 0)
            await _stock.AdminMoveAsync(j.Id, StockMovementType.Entry, f.StockQuantity, "Estoque inicial do cadastro", CurrentUserId, CurrentUserName);

        await _audit.LogAsync("jogo.create", "Jogo", j.Id.ToString(), $"{j.Sku} - {j.Name}");
        TempData["Success"] = "Jogo criado. Agora você pode enviar as imagens.";
        return RedirectToAction(nameof(Edit), new { id = j.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var j = await _db.Jogos.AsNoTracking().Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id);
        if (j == null) return NotFound();
        var f = ToForm(j);
        await FillListsAsync(f);
        return View("Form", f);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, JogoForm f)
    {
        var j = await _db.Jogos.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id);
        if (j == null) return NotFound();
        f.Id = id;
        await ValidateUniqueAsync(f);
        if (!ModelState.IsValid)
        {
            f.Images = j.Images.OrderBy(i => i.SortOrder).ToList();
            f.CurrentStock = j.StockQuantity;
            await FillListsAsync(f);
            return View("Form", f);
        }

        var oldPrice = j.Price;
        var oldPromo = j.PromotionalPrice;
        Apply(j, f); // estoque NÃO é alterado aqui (apenas por movimentações)
        await _db.SaveChangesAsync();
        await _audit.LogAsync("jogo.update", "Jogo", j.Id.ToString(),
            oldPrice != j.Price || oldPromo != j.PromotionalPrice
                ? $"{j.Sku}: preço {oldPrice} -> {j.Price}; promo {oldPromo} -> {j.PromotionalPrice}"
                : j.Sku);
        TempData["Success"] = "Jogo atualizado.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var j = await _db.Jogos.FirstOrDefaultAsync(x => x.Id == id);
        if (j == null) return NotFound();
        j.IsActive = !j.IsActive;
        j.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(j.IsActive ? "jogo.activate" : "jogo.deactivate", "Jogo", j.Id.ToString(), j.Sku);
        TempData["Success"] = j.IsActive ? "Jogo ativado." : "Jogo desativado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UploadImages(int id, List<IFormFile> files)
    {
        var j = await _db.Jogos.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id);
        if (j == null) return NotFound();
        if (files == null || files.Count == 0) { TempData["Error"] = "Selecione ao menos uma imagem."; return RedirectToAction(nameof(Edit), new { id }); }
        if (j.Images.Count + files.Count > 10) { TempData["Error"] = "Limite de 10 imagens por jogo."; return RedirectToAction(nameof(Edit), new { id }); }

        var errors = new List<string>();
        var order = j.Images.Count == 0 ? 0 : j.Images.Max(i => i.SortOrder) + 1;
        foreach (var file in files)
        {
            var r = await _storage.SaveImageAsync(file, "jogos");
            if (!r.Success) { errors.Add($"{Path.GetFileName(file.FileName)}: {r.Error}"); continue; }
            _db.JogoImages.Add(new JogoImage { JogoId = j.Id, Path = r.Path!, AltText = j.Name, SortOrder = order++, IsPrimary = j.Images.Count == 0 && !_db.ChangeTracker.Entries<JogoImage>().Any(e => e.State == EntityState.Added && e.Entity.IsPrimary) });
        }
        j.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("jogo.images.upload", "Jogo", j.Id.ToString());
        if (errors.Count > 0) TempData["Error"] = string.Join(" | ", errors); else TempData["Success"] = "Imagens enviadas.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteImage(int id, int imageId)
    {
        var img = await _db.JogoImages.FirstOrDefaultAsync(i => i.Id == imageId && i.JogoId == id);
        if (img == null) return NotFound();
        var wasPrimary = img.IsPrimary;
        _db.JogoImages.Remove(img);
        await _db.SaveChangesAsync();
        await _storage.DeleteAsync(img.Path);
        if (wasPrimary)
        {
            var next = await _db.JogoImages.Where(i => i.JogoId == id).OrderBy(i => i.SortOrder).FirstOrDefaultAsync();
            if (next != null) { next.IsPrimary = true; await _db.SaveChangesAsync(); }
        }
        await _audit.LogAsync("jogo.images.delete", "Jogo", id.ToString());
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> SetPrimaryImage(int id, int imageId)
    {
        var imgs = await _db.JogoImages.Where(i => i.JogoId == id).ToListAsync();
        if (!imgs.Any(i => i.Id == imageId)) return NotFound();
        foreach (var i in imgs) i.IsPrimary = i.Id == imageId;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id });
    }

    // ---------- helpers ----------
    private static JogoForm ToForm(Jogo p) => new()
    {
        Id = p.Id, Name = p.Name, ShortDescription = p.ShortDescription, Description = p.Description, Price = p.Price,
        PromotionalPrice = p.PromotionalPrice, Sku = p.Sku, Barcode = p.Barcode, Ncm = p.Ncm, Cest = p.Cest, OriginCode = p.OriginCode,
        Platform = p.Platform, MediaType = p.MediaType, AgeRating = p.AgeRating, ReleaseDate = p.ReleaseDate, CategoryId = p.CategoryId, BrandId = p.BrandId,
        StockQuantity = p.StockQuantity, CurrentStock = p.StockQuantity, WeightKg = p.WeightKg, HeightCm = p.HeightCm, WidthCm = p.WidthCm,
        LengthCm = p.LengthCm, IsActive = p.IsActive, IsFeatured = p.IsFeatured, IsAvailable = p.IsAvailable,
        Images = p.Images.OrderBy(i => i.SortOrder).ToList()
    };

    /// <summary>Atribuição explícita campo a campo (sem binding direto na entidade).</summary>
    private static void Apply(Jogo p, JogoForm f)
    {
        p.Name = f.Name.Trim();
        p.ShortDescription = f.ShortDescription.NullIfEmpty();
        p.Description = f.Description.NullIfEmpty();
        p.Price = Money.Round(f.Price);
        p.PromotionalPrice = f.PromotionalPrice.HasValue ? Money.Round(f.PromotionalPrice.Value) : null;
        p.Sku = f.Sku.Trim();
        p.Barcode = f.Barcode.NullIfEmpty();
        p.Ncm = f.Ncm.NullIfEmpty(); // vazio = NCM padrão do tipo de mídia (Jogo.EffectiveNcm)
        p.Cest = f.Cest.NullIfEmpty();
        p.OriginCode = f.OriginCode;
        p.Platform = f.Platform;
        p.MediaType = f.MediaType;
        p.AgeRating = f.AgeRating;
        p.ReleaseDate = f.ReleaseDate;
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

    private async Task ValidateUniqueAsync(JogoForm f)
    {
        var sku = f.Sku?.Trim() ?? string.Empty;
        if (sku.Length > 0 && await _db.Jogos.AnyAsync(p => p.Sku == sku && p.Id != f.Id))
            ModelState.AddModelError(nameof(f.Sku), "Já existe um jogo com este SKU.");
        if (f.CategoryId > 0 && !await _db.Categories.AnyAsync(c => c.Id == f.CategoryId))
            ModelState.AddModelError(nameof(f.CategoryId), "Categoria inválida.");
        if (f.BrandId.HasValue && !await _db.Brands.AnyAsync(b => b.Id == f.BrandId.Value))
            ModelState.AddModelError(nameof(f.BrandId), "Publicadora inválida.");
    }

    private async Task<string> UniqueSlugAsync(string name, int? ignoreId)
    {
        var baseSlug = SlugHelper.Generate(name);
        var slug = baseSlug;
        var n = 2;
        while (await _db.Jogos.AnyAsync(p => p.Slug == slug && p.Id != ignoreId)) slug = $"{baseSlug}-{n++}";
        return slug;
    }
}
