using EcommerceBase.Web.Data;
using EcommerceBase.Web.Domain;
using EcommerceBase.Web.Domain.Entities;
using EcommerceBase.Web.Domain.Rules;
using EcommerceBase.Web.Infrastructure;
using EcommerceBase.Web.Services;
using EcommerceBase.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceBase.Web.Areas.Admin.Controllers;

public class CustomersController : AdminBaseController
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IAuditService _audit;

    public CustomersController(AppDbContext db, UserManager<ApplicationUser> users, IAuditService audit)
    {
        _db = db;
        _users = users;
        _audit = audit;
    }

    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        page = Math.Max(page, 1);
        var customerIds = from ur in _db.UserRoles
                          join r in _db.Roles on ur.RoleId equals r.Id
                          where r.Name == Roles.Customer
                          select ur.UserId;
        var query = _db.Users.AsNoTracking().Where(u => customerIds.Contains(u.Id));
        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = "%" + q.Trim().EscapeLike() + "%";
            query = query.Where(u => EF.Functions.ILike(u.FullName, pattern, "\\") || EF.Functions.ILike(u.Email!, pattern, "\\"));
        }
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(u => u.CreatedAtUtc).Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
        return View(new CustomerListVm { Items = items, Q = q, Page = page, TotalPages = TotalPagesFor(total) });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        return View(new CustomerDetailsVm
        {
            User = user,
            Orders = await _db.Orders.AsNoTracking().Where(o => o.UserId == id).OrderByDescending(o => o.CreatedAtUtc).Take(50).ToListAsync(),
            Addresses = await _db.Addresses.AsNoTracking().Where(a => a.UserId == id).ToListAsync(),
            IsAdmin = await _users.IsInRoleAsync(user, Roles.Admin)
        });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        return View(new CustomerEditForm { Id = id, Email = user.Email ?? "", FullName = user.FullName, Phone = user.PhoneNumber, Document = user.Document });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, CustomerEditForm f)
    {
        var user = await _users.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();
        f.Id = id;
        f.Email = user.Email ?? "";
        var doc = Documents.OnlyDigits(f.Document);
        if (!string.IsNullOrEmpty(doc) && !Documents.IsValidCpfOrCnpj(doc)) ModelState.AddModelError(nameof(f.Document), "CPF/CNPJ inválido.");
        if (!ModelState.IsValid) return View(f);

        // Somente dados cadastrais. E-mail, papéis e senha nunca são alterados por aqui.
        user.FullName = f.FullName.Trim();
        user.PhoneNumber = f.Phone.NullIfEmpty();
        user.Document = string.IsNullOrEmpty(doc) ? null : doc;
        var r = await _users.UpdateAsync(user);
        if (!r.Succeeded)
        {
            foreach (var e in r.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(f);
        }
        await _audit.LogAsync("customer.update", "User", id.ToString(), "dados cadastrais");
        TempData["Success"] = "Cliente atualizado.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Bloqueia/desbloqueia o acesso. Administradores e a própria conta não podem ser desativados aqui.</summary>
    [HttpPost]
    public async Task<IActionResult> SetActive(Guid id, bool active)
    {
        var user = await _users.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();
        if (user.Id == CurrentUserId) { TempData["Error"] = "Você não pode alterar a sua própria conta aqui."; return RedirectToAction(nameof(Details), new { id }); }
        if (await _users.IsInRoleAsync(user, Roles.Admin)) { TempData["Error"] = "Contas de administrador não podem ser desativadas por esta tela."; return RedirectToAction(nameof(Details), new { id }); }

        user.IsActive = active;
        await _users.SetLockoutEnabledAsync(user, true);
        await _users.SetLockoutEndDateAsync(user, active ? null : DateTimeOffset.MaxValue);
        await _users.UpdateAsync(user);
        if (!active) await _users.UpdateSecurityStampAsync(user); // invalida sessões abertas
        await _audit.LogAsync(active ? "customer.activate" : "customer.deactivate", "User", id.ToString());
        TempData["Success"] = active ? "Acesso reativado." : "Acesso bloqueado.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
