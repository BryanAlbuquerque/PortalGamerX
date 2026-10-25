using PortalGamerX.Data;
using PortalGamerX.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PortalGamerX.Areas.Admin.Controllers;

public class AuditController : AdminBaseController
{
    private readonly AppDbContext _db;
    public AuditController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(int page = 1)
    {
        page = Math.Max(page, 1);
        var total = await _db.AuditLogs.CountAsync();
        var items = await _db.AuditLogs.AsNoTracking().OrderByDescending(a => a.CreatedAtUtc).Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
        return View(new AuditListVm { Items = items, Page = page, TotalPages = TotalPagesFor(total) });
    }
}
