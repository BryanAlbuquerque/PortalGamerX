using PortalGamerX.Data;
using PortalGamerX.Domain;
using PortalGamerX.Domain.Entities;
using PortalGamerX.Domain.Rules;
using PortalGamerX.Infrastructure;
using PortalGamerX.Services;
using PortalGamerX.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PortalGamerX.Areas.Admin.Controllers;

public class OrdersController : AdminBaseController
{
    private readonly AppDbContext _db;
    private readonly IOrderService _orders;
    private readonly IPaymentService _payments;
    private readonly IFiscalService _fiscal;
    private readonly IAuditService _audit;
    private readonly IStockService _stock;

    public OrdersController(AppDbContext db, IOrderService orders, IPaymentService payments, IFiscalService fiscal, IAuditService audit, IStockService stock)
    {
        _stock = stock;
        _db = db;
        _orders = orders;
        _payments = payments;
        _fiscal = fiscal;
        _audit = audit;
    }

    public async Task<IActionResult> Index(string? q, OrderStatus? status, int page = 1)
    {
        page = Math.Max(page, 1);
        var query = _db.Orders.AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(o => o.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = "%" + q.Trim().EscapeLike() + "%";
            query = query.Where(o => EF.Functions.ILike(o.Number, pattern, "\\") || EF.Functions.ILike(o.CustomerEmail, pattern, "\\") || EF.Functions.ILike(o.CustomerName, pattern, "\\"));
        }
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(o => o.CreatedAtUtc).Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
        return View(new AdminOrderListVm { Items = items, Q = q, Status = status, Page = page, Total = total, TotalPages = TotalPagesFor(total) });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.History.OrderBy(h => h.CreatedAtUtc))
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();

        var fiscalDocs = await _db.FiscalDocuments.AsNoTracking().Include(f => f.Events).Where(f => f.OrderId == id).OrderByDescending(f => f.CreatedAtUtc).ToListAsync();
        return View(new AdminOrderVm
        {
            Order = order,
            FiscalDocuments = fiscalDocs,
            FiscalConfigured = await _fiscal.IsConfiguredAsync(),
            Keys = await _stock.GetDeliveredKeysAsync(id, includeCode: false),
            AllowedTargets = OrderStateMachine.AdminTargets(order.Status).ToList(),
            CanRefund = order.Payments.Any(p => p.Status == PaymentStatus.Approved && !string.IsNullOrEmpty(p.ExternalPaymentId))
        });
    }

    [HttpPost]
    public async Task<IActionResult> ChangeStatus(ChangeStatusInput input)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "Dados inválidos."; return RedirectToAction(nameof(Details), new { id = input.Id }); }
        var r = await _orders.ChangeStatusAsync(input.Id, input.Target, input.Note, new Actor(CurrentUserId, CurrentUserName));
        if (r.Success)
        {
            await _audit.LogAsync("order.status", "Order", input.Id.ToString(), $"-> {input.Target}");
            TempData["Success"] = "Status atualizado.";
        }
        else TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Details), new { id = input.Id });
    }

    /// <summary>Entrega as keys de um pedido pago que ficou sem entrega (idempotente).</summary>
    [HttpPost]
    public async Task<IActionResult> DeliverKeys(Guid id)
    {
        var r = await _orders.DeliverKeysAsync(id, new Actor(CurrentUserId, CurrentUserName));
        await _audit.LogAsync("order.deliverkeys", "Order", id.ToString(), r.Success ? "ok" : "falhou");
        if (r.Success) TempData["Success"] = "Keys entregues ao cliente."; else TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> SaveNotes(Guid id, string? notes)
    {
        var r = await _orders.SaveAdminNotesAsync(id, notes);
        if (r.Success) { await _audit.LogAsync("order.notes", "Order", id.ToString()); TempData["Success"] = "Observação salva."; }
        else TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Refund(Guid id)
    {
        var r = await _payments.RefundAsync(id);
        await _audit.LogAsync("order.refund", "Order", id.ToString(), r.Success ? "solicitado" : "falhou");
        if (r.Success) TempData["Success"] = "Reembolso solicitado ao provedor. O status será atualizado após a confirmação."; else TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> RequestFiscal(Guid id)
    {
        var r = await _fiscal.RequestIssueAsync(id);
        await _audit.LogAsync("order.fiscal.request", "Order", id.ToString(), r.Success ? "ok" : "falhou");
        if (r.Success) TempData["Success"] = "Emissão solicitada ao provedor fiscal."; else TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> RefreshFiscal(Guid id, int documentId)
    {
        var r = await _fiscal.RefreshAsync(documentId);
        if (r.Success) TempData["Success"] = "Situação fiscal atualizada."; else TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Details), new { id });
    }
}
