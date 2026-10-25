using PortalGamerX.Data;
using PortalGamerX.Domain;
using PortalGamerX.Infrastructure;
using PortalGamerX.Services;
using PortalGamerX.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PortalGamerX.Areas.Admin.Controllers;

public class StockController : AdminBaseController
{
    private readonly AppDbContext _db;
    private readonly IStockService _stock;
    private readonly IStoreSettingsService _store;
    private readonly IAuditService _audit;

    public StockController(AppDbContext db, IStockService stock, IStoreSettingsService store, IAuditService audit)
    {
        _db = db;
        _stock = stock;
        _store = store;
        _audit = audit;
    }

    public async Task<IActionResult> Index(string? q, bool lowOnly = false, int page = 1)
    {
        page = Math.Max(page, 1);
        var settings = await _store.GetAsync();
        var query = _db.Jogos.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = "%" + q.Trim().EscapeLike() + "%";
            query = query.Where(p => EF.Functions.ILike(p.Name, pattern, "\\") || EF.Functions.ILike(p.Sku, pattern, "\\"));
        }
        if (lowOnly) query = query.Where(p => p.StockQuantity <= settings.LowStockThreshold);
        var total = await query.CountAsync();
        var items = await query.OrderBy(p => p.StockQuantity).ThenBy(p => p.Name).Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
        return View(new StockListVm { Items = items, Q = q, LowOnly = lowOnly, Threshold = settings.LowStockThreshold, Page = page, TotalPages = TotalPagesFor(total) });
    }

    public async Task<IActionResult> Move(int id)
    {
        var p = await _db.Jogos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        return View(new StockMoveForm { JogoId = id, JogoName = p.Name, CurrentStock = p.StockQuantity });
    }

    [HttpPost]
    public async Task<IActionResult> Move(int id, StockMoveForm f)
    {
        var p = await _db.Jogos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        f.JogoId = id;
        f.JogoName = p.Name;
        f.CurrentStock = p.StockQuantity;
        if (f.Type is StockMovementType.OrderRelease or StockMovementType.OrderReservation)
            ModelState.AddModelError(nameof(f.Type), "Tipo inválido.");
        if (!ModelState.IsValid) return View(f);

        var r = await _stock.AdminMoveAsync(id, f.Type, f.Quantity, f.Reason, CurrentUserId, CurrentUserName);
        if (!r.Success) { ModelState.AddModelError(string.Empty, r.Error ?? "Falha na movimentação."); return View(f); }
        await _audit.LogAsync("stock.move", "Jogo", id.ToString(), $"{f.Type} {f.Quantity} -> saldo {r.NewBalance}: {f.Reason}");
        TempData["Success"] = $"Movimentação registrada. Novo saldo: {r.NewBalance}.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> History(int id)
    {
        var p = await _db.Jogos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        var moves = await _db.StockMovements.AsNoTracking().Where(m => m.JogoId == id)
            .OrderByDescending(m => m.CreatedAtUtc).Take(200).ToListAsync();
        return View(new StockHistoryVm { Jogo = p, Movements = moves });
    }
}
