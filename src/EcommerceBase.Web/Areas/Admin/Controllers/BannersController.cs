using EcommerceBase.Web.Data;
using EcommerceBase.Web.Domain.Entities;
using EcommerceBase.Web.Infrastructure;
using EcommerceBase.Web.Services;
using EcommerceBase.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceBase.Web.Areas.Admin.Controllers;

public class BannersController : AdminBaseController
{
    private readonly AppDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IAuditService _audit;

    public BannersController(AppDbContext db, IFileStorage storage, IAuditService audit)
    {
        _db = db;
        _storage = storage;
        _audit = audit;
    }

    public async Task<IActionResult> Index() => View(await _db.Banners.AsNoTracking().OrderBy(b => b.SortOrder).ThenBy(b => b.Id).ToListAsync());

    public IActionResult Create() => View("Form", new BannerForm());

    [HttpPost]
    public async Task<IActionResult> Create(BannerForm f, IFormFile? image)
    {
        ValidateLink(f);
        if (image == null || image.Length == 0) ModelState.AddModelError("image", "Envie a imagem do banner.");
        if (!ModelState.IsValid) return View("Form", f);

        var up = await _storage.SaveImageAsync(image!, "banners");
        if (!up.Success) { ModelState.AddModelError("image", up.Error ?? "Imagem inválida."); return View("Form", f); }

        var b = new Banner { ImagePath = up.Path! };
        Apply(b, f);
        _db.Banners.Add(b);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("banner.create", "Banner", b.Id.ToString(), b.Title);
        TempData["Success"] = "Banner criado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var b = await _db.Banners.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();
        return View("Form", new BannerForm { Id = b.Id, Title = b.Title, Subtitle = b.Subtitle, LinkUrl = b.LinkUrl, ButtonText = b.ButtonText, SortOrder = b.SortOrder, IsActive = b.IsActive, CurrentImage = b.ImagePath });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, BannerForm f, IFormFile? image)
    {
        var b = await _db.Banners.FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();
        f.Id = id;
        f.CurrentImage = b.ImagePath;
        ValidateLink(f);
        if (!ModelState.IsValid) return View("Form", f);

        string? oldImage = null;
        if (image != null && image.Length > 0)
        {
            var up = await _storage.SaveImageAsync(image, "banners");
            if (!up.Success) { ModelState.AddModelError("image", up.Error ?? "Imagem inválida."); return View("Form", f); }
            oldImage = b.ImagePath;
            b.ImagePath = up.Path!;
        }
        Apply(b, f);
        await _db.SaveChangesAsync();
        if (oldImage != null) await _storage.DeleteAsync(oldImage);
        await _audit.LogAsync("banner.update", "Banner", id.ToString(), b.Title);
        TempData["Success"] = "Banner atualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var b = await _db.Banners.FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();
        b.IsActive = !b.IsActive;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var b = await _db.Banners.FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();
        _db.Banners.Remove(b);
        await _db.SaveChangesAsync();
        await _storage.DeleteAsync(b.ImagePath);
        await _audit.LogAsync("banner.delete", "Banner", id.ToString(), b.Title);
        TempData["Success"] = "Banner excluído.";
        return RedirectToAction(nameof(Index));
    }

    private void ValidateLink(BannerForm f)
    {
        if (!string.IsNullOrWhiteSpace(f.LinkUrl))
        {
            var l = f.LinkUrl.Trim();
            var ok = (l.StartsWith("/") && !l.StartsWith("//")) || l.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
            if (!ok) ModelState.AddModelError(nameof(f.LinkUrl), "Use um caminho interno (/produtos) ou uma URL https://.");
        }
    }

    private static void Apply(Banner b, BannerForm f)
    {
        b.Title = f.Title.Trim();
        b.Subtitle = f.Subtitle.NullIfEmpty();
        b.LinkUrl = f.LinkUrl.NullIfEmpty();
        b.ButtonText = f.ButtonText.NullIfEmpty();
        b.SortOrder = f.SortOrder;
        b.IsActive = f.IsActive;
    }
}
