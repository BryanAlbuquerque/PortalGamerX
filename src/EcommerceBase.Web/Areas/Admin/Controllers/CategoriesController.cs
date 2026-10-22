using EcommerceBase.Web.Data;
using EcommerceBase.Web.Domain.Entities;
using EcommerceBase.Web.Domain.Rules;
using EcommerceBase.Web.Infrastructure;
using EcommerceBase.Web.Services;
using EcommerceBase.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcommerceBase.Web.Areas.Admin.Controllers;

public class CategoriesController : AdminBaseController
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public CategoriesController(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _db.Categories.AsNoTracking().Include(c => c.Parent)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync();
        var counts = await _db.Products.GroupBy(p => p.CategoryId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        ViewBag.Counts = counts;
        return View(items);
    }

    private async Task FillParentsAsync(CategoryForm f)
    {
        f.Parents = await _db.Categories.AsNoTracking().Where(c => c.ParentId == null && c.Id != f.Id)
            .OrderBy(c => c.Name).Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToListAsync();
    }

    public async Task<IActionResult> Create()
    {
        var f = new CategoryForm();
        await FillParentsAsync(f);
        return View("Form", f);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CategoryForm f)
    {
        if (!ModelState.IsValid) { await FillParentsAsync(f); return View("Form", f); }
        var c = new Category { Slug = await UniqueSlugAsync(f.Name, null) };
        Apply(c, f);
        _db.Categories.Add(c);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("category.create", "Category", c.Id.ToString(), c.Name);
        TempData["Success"] = "Categoria criada.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var c = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();
        var f = new CategoryForm { Id = c.Id, Name = c.Name, Description = c.Description, ParentId = c.ParentId, SortOrder = c.SortOrder, IsActive = c.IsActive };
        await FillParentsAsync(f);
        return View("Form", f);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CategoryForm f)
    {
        var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();
        f.Id = id;
        if (f.ParentId == id) ModelState.AddModelError(nameof(f.ParentId), "A categoria não pode ser pai de si mesma.");
        if (f.ParentId.HasValue && await _db.Categories.AnyAsync(x => x.ParentId == id))
            ModelState.AddModelError(nameof(f.ParentId), "Esta categoria possui subcategorias e não pode ter pai (limite de 2 níveis).");
        if (!ModelState.IsValid) { await FillParentsAsync(f); return View("Form", f); }
        Apply(c, f);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("category.update", "Category", id.ToString(), c.Name);
        TempData["Success"] = "Categoria atualizada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();
        c.IsActive = !c.IsActive;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(c.IsActive ? "category.activate" : "category.deactivate", "Category", id.ToString(), c.Name);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();
        if (await _db.Products.AnyAsync(p => p.CategoryId == id) || await _db.Categories.AnyAsync(x => x.ParentId == id))
        {
            TempData["Error"] = "Não é possível excluir: há produtos ou subcategorias vinculados. Desative a categoria.";
            return RedirectToAction(nameof(Index));
        }
        _db.Categories.Remove(c);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("category.delete", "Category", id.ToString(), c.Name);
        TempData["Success"] = "Categoria excluída.";
        return RedirectToAction(nameof(Index));
    }

    private static void Apply(Category c, CategoryForm f)
    {
        c.Name = f.Name.Trim();
        c.Description = f.Description.NullIfEmpty();
        c.ParentId = f.ParentId;
        c.SortOrder = f.SortOrder;
        c.IsActive = f.IsActive;
    }

    private async Task<string> UniqueSlugAsync(string name, int? ignoreId)
    {
        var baseSlug = SlugHelper.Generate(name);
        var slug = baseSlug;
        var n = 2;
        while (await _db.Categories.AnyAsync(c => c.Slug == slug && c.Id != ignoreId)) slug = $"{baseSlug}-{n++}";
        return slug;
    }
}
