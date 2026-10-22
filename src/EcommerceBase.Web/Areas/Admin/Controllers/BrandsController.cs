using EcommerceBase.Web.Data;
using EcommerceBase.Web.Domain.Entities;
using EcommerceBase.Web.Domain.Rules;
using EcommerceBase.Web.Services;
using EcommerceBase.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceBase.Web.Areas.Admin.Controllers;

public class BrandsController : AdminBaseController
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public BrandsController(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IActionResult> Index() => View(await _db.Brands.AsNoTracking().OrderBy(b => b.Name).ToListAsync());

    public IActionResult Create() => View("Form", new BrandForm());

    [HttpPost]
    public async Task<IActionResult> Create(BrandForm f)
    {
        if (!ModelState.IsValid) return View("Form", f);
        var baseSlug = SlugHelper.Generate(f.Name);
        var slug = baseSlug;
        var n = 2;
        while (await _db.Brands.AnyAsync(b => b.Slug == slug)) slug = $"{baseSlug}-{n++}";
        var b = new Brand { Name = f.Name.Trim(), Slug = slug, IsActive = f.IsActive };
        _db.Brands.Add(b);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("brand.create", "Brand", b.Id.ToString(), b.Name);
        TempData["Success"] = "Marca criada.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var b = await _db.Brands.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return b == null ? NotFound() : View("Form", new BrandForm { Id = b.Id, Name = b.Name, IsActive = b.IsActive });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, BrandForm f)
    {
        var b = await _db.Brands.FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();
        f.Id = id;
        if (!ModelState.IsValid) return View("Form", f);
        b.Name = f.Name.Trim();
        b.IsActive = f.IsActive;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("brand.update", "Brand", id.ToString(), b.Name);
        TempData["Success"] = "Marca atualizada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var b = await _db.Brands.FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();
        _db.Brands.Remove(b); // produtos mantêm o cadastro (FK SetNull)
        await _db.SaveChangesAsync();
        await _audit.LogAsync("brand.delete", "Brand", id.ToString(), b.Name);
        TempData["Success"] = "Marca excluída.";
        return RedirectToAction(nameof(Index));
    }
}
