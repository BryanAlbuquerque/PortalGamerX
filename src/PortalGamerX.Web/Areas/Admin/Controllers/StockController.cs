using PortalGamerX.Data;
using PortalGamerX.Domain;
using PortalGamerX.Domain.Rules;
using PortalGamerX.Infrastructure;
using PortalGamerX.Services;
using PortalGamerX.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PortalGamerX.Areas.Admin.Controllers;

/// <summary>Estoque de keys: importação, consulta (mascarada), revelação auditada e invalidação.</summary>
public class StockController : AdminBaseController
{
    private readonly AppDbContext _db;
    private readonly IStockService _stock;
    private readonly IKeyVault _vault;
    private readonly IStoreSettingsService _store;
    private readonly IAuditService _audit;

    public StockController(AppDbContext db, IStockService stock, IKeyVault vault, IStoreSettingsService store, IAuditService audit)
    {
        _db = db;
        _stock = stock;
        _vault = vault;
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

    // ---------- Importar keys ----------
    public async Task<IActionResult> Import(int id)
    {
        var p = await _db.Jogos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        return View(new KeyImportForm { JogoId = id, JogoName = p.Name, CurrentStock = p.StockQuantity });
    }

    [HttpPost]
    public async Task<IActionResult> Import(int id, KeyImportForm f)
    {
        var p = await _db.Jogos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        f.JogoId = id;
        f.JogoName = p.Name;
        f.CurrentStock = p.StockQuantity;
        if (!ModelState.IsValid) return View(f);

        var r = await _stock.ImportKeysAsync(id, f.Keys, f.Note, CurrentUserId, CurrentUserName);
        f.Keys = string.Empty; // nunca devolve as keys coladas na página
        if (!r.Success)
        {
            ModelState.AddModelError(string.Empty, r.Error ?? "Falha na importação.");
            return View(f);
        }
        await _audit.LogAsync("keys.import", "Jogo", id.ToString(), $"{r.Imported} importada(s); {r.Duplicates} já existente(s); {r.Invalid} inválida(s); saldo {r.NewBalance}");
        TempData["Success"] = $"{r.Imported} key(s) importada(s). Já cadastradas: {r.Duplicates}. Linhas inválidas: {r.Invalid}. Repetidas no texto: {r.RepeatedInText}. Novo saldo: {r.NewBalance}.";
        return RedirectToAction(nameof(Keys), new { id });
    }

    // ---------- Lista de keys (mascaradas) ----------
    public async Task<IActionResult> Keys(int id, GameKeyStatus? status, int page = 1)
    {
        page = Math.Max(page, 1);
        var jogo = await _db.Jogos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (jogo == null) return NotFound();

        var all = _db.GameKeys.AsNoTracking().Where(k => k.JogoId == id);
        var counts = await all.GroupBy(k => k.Status).Select(g => new { g.Key, N = g.Count() }).ToListAsync();
        var q = status.HasValue ? all.Where(k => k.Status == status.Value) : all;
        var total = await q.CountAsync();
        // Nunca carrega o texto cifrado para a lista: só dados de identificação mascarados.
        var items = await q.OrderByDescending(k => k.Id).Skip((page - 1) * PageSize).Take(PageSize)
            .Select(k => new Domain.Entities.GameKey
            {
                Id = k.Id, JogoId = k.JogoId, CodeHint = k.CodeHint, Status = k.Status, OrderId = k.OrderId, Note = k.Note,
                CreatedByName = k.CreatedByName, CreatedAtUtc = k.CreatedAtUtc, SoldAtUtc = k.SoldAtUtc
            }).ToListAsync();

        return View(new KeyListVm
        {
            Jogo = jogo, Items = items, Status = status, Page = page, TotalPages = TotalPagesFor(total),
            Counts = counts.ToDictionary(c => c.Key, c => c.N)
        });
    }

    /// <summary>Revela uma key (ação auditada). POST para não ficar em histórico/links.</summary>
    [HttpPost]
    public async Task<IActionResult> Reveal(long keyId)
    {
        var k = await _db.GameKeys.AsNoTracking().Include(x => x.Jogo).FirstOrDefaultAsync(x => x.Id == keyId);
        if (k == null) return NotFound();
        var code = _vault.Unprotect(k.CodeEncrypted);
        await _audit.LogAsync("keys.reveal", "GameKey", keyId.ToString(), $"jogo {k.JogoId}; pedido {k.OrderId}");
        Response.Headers.CacheControl = "no-store";
        return View(new KeyRevealVm { Jogo = k.Jogo!, KeyId = k.Id, Code = code ?? "[ilegível: o anel de chaves do Data Protection mudou]", OrderId = k.OrderId });
    }

    [HttpPost]
    public async Task<IActionResult> VoidKey(long keyId, string? reason, int jogoId)
    {
        var r = await _stock.VoidKeyAsync(keyId, reason ?? string.Empty, CurrentUserId, CurrentUserName);
        if (r.Success)
        {
            await _audit.LogAsync("keys.void", "GameKey", keyId.ToString(), reason);
            TempData["Success"] = "Key invalidada.";
        }
        else TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Keys), new { id = jogoId });
    }

    /// <summary>Corrige divergência entre o saldo do jogo e as keys realmente disponíveis.</summary>
    [HttpPost]
    public async Task<IActionResult> Recalculate(int id)
    {
        var n = await _stock.RecalculateAsync(id);
        await _audit.LogAsync("keys.recalculate", "Jogo", id.ToString(), $"saldo = {n}");
        TempData["Success"] = $"Saldo recalculado: {n} key(s) disponível(is).";
        return RedirectToAction(nameof(Keys), new { id });
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
