using PortalGamerX.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PortalGamerX.Infrastructure;

public class NavCategoriesViewComponent : ViewComponent
{
    private readonly AppDbContext _db;
    public NavCategoriesViewComponent(AppDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        try
        {
            var cats = await _db.Categories.AsNoTracking().Where(c => c.IsActive && c.ParentId == null)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name).Take(15).ToListAsync();
            return View(cats);
        }
        catch
        {
            return View(new List<PortalGamerX.Domain.Entities.Category>());
        }
    }
}
