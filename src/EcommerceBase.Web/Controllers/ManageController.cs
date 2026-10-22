using EcommerceBase.Web.Data;
using EcommerceBase.Web.Domain;
using EcommerceBase.Web.Domain.Entities;
using EcommerceBase.Web.Domain.Rules;
using EcommerceBase.Web.Infrastructure;
using EcommerceBase.Web.Services;
using EcommerceBase.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceBase.Web.Controllers;

/// <summary>Minha Conta. Todo acesso a registros é filtrado pelo usuário autenticado (nunca só pelo ID da URL).</summary>
[Authorize]
[Route("conta")]
public class ManageController : Controller
{
    private const int PageSize = 10;
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly IAddressService _addresses;
    private readonly IOrderService _orders;
    private readonly IPaymentService _payments;
    private readonly AppDbContext _db;

    public ManageController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, IAddressService addresses,
        IOrderService orders, IPaymentService payments, AppDbContext db)
    {
        _users = users;
        _signIn = signIn;
        _addresses = addresses;
        _orders = orders;
        _payments = payments;
        _db = db;
    }

    private Guid UserId => User.GetUserId()!.Value;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var user = await _users.GetUserAsync(User);
        if (user == null) return Challenge();
        ViewBag.RecentOrders = await _db.Orders.AsNoTracking().Where(o => o.UserId == UserId)
            .OrderByDescending(o => o.CreatedAtUtc).Take(3).ToListAsync();
        return View(user);
    }

    // ---------- Perfil ----------
    [HttpGet("perfil")]
    public async Task<IActionResult> Profile()
    {
        var user = await _users.GetUserAsync(User);
        if (user == null) return Challenge();
        return View(new ProfileVm { FullName = user.FullName, Email = user.Email ?? "", Phone = user.PhoneNumber, Document = user.Document });
    }

    [HttpPost("perfil")]
    public async Task<IActionResult> Profile(ProfileVm model)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null) return Challenge();
        model.Email = user.Email ?? "";
        var doc = Documents.OnlyDigits(model.Document);
        if (!string.IsNullOrEmpty(doc) && !Documents.IsValidCpfOrCnpj(doc))
            ModelState.AddModelError(nameof(model.Document), "CPF/CNPJ inválido.");
        if (!ModelState.IsValid) return View(model);

        // Somente estes campos são atribuídos: papel, e-mail, status e demais propriedades ficam intocados.
        user.FullName = model.FullName.Trim();
        user.PhoneNumber = model.Phone.NullIfEmpty();
        user.Document = string.IsNullOrEmpty(doc) ? null : doc;
        var result = await _users.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }
        TempData["Success"] = "Dados atualizados.";
        return RedirectToAction(nameof(Profile));
    }

    // ---------- Senha ----------
    [HttpGet("senha")]
    public IActionResult ChangePassword() => View(new ChangePasswordVm());

    [HttpPost("senha")]
    public async Task<IActionResult> ChangePassword(ChangePasswordVm model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _users.GetUserAsync(User);
        if (user == null) return Challenge();
        var result = await _users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Code == "PasswordMismatch" ? "A senha atual está incorreta." : e.Description);
            return View(model);
        }
        await _signIn.RefreshSignInAsync(user);
        TempData["Success"] = "Senha alterada com sucesso.";
        return RedirectToAction(nameof(ChangePassword));
    }

    // ---------- Endereços ----------
    [HttpGet("enderecos")]
    public async Task<IActionResult> Addresses() => View(await _addresses.ListAsync(UserId));

    [HttpGet("enderecos/novo")]
    public async Task<IActionResult> AddressCreate(string? returnUrl = null)
    {
        var user = await _users.GetUserAsync(User);
        return View("AddressForm", new AddressInput { RecipientName = user?.FullName ?? "", Phone = user?.PhoneNumber, ReturnUrl = returnUrl });
    }

    [HttpGet("enderecos/{id:int}/editar")]
    public async Task<IActionResult> AddressEdit(int id)
    {
        var a = await _addresses.GetAsync(UserId, id);
        if (a == null) return NotFound();
        return View("AddressForm", new AddressInput
        {
            Id = a.Id, Label = a.Label, RecipientName = a.RecipientName, Phone = a.Phone, PostalCode = Documents.FormatPostalCode(a.PostalCode),
            Street = a.Street, Number = a.Number, Complement = a.Complement, District = a.District, City = a.City, State = a.State, IsDefault = a.IsDefault
        });
    }

    [HttpPost("enderecos/salvar")]
    public async Task<IActionResult> AddressSave(AddressInput model)
    {
        if (!ModelState.IsValid) return View("AddressForm", model);
        var r = await _addresses.SaveAsync(UserId, model);
        if (!r.Success)
        {
            ModelState.AddModelError(string.Empty, r.Error ?? "Não foi possível salvar.");
            return View("AddressForm", model);
        }
        TempData["Success"] = "Endereço salvo.";
        return Url.IsLocalUrl(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl!) : RedirectToAction(nameof(Addresses));
    }

    [HttpPost("enderecos/{id:int}/principal")]
    public async Task<IActionResult> AddressDefault(int id)
    {
        var r = await _addresses.SetDefaultAsync(UserId, id);
        if (!r.Success) TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost("enderecos/{id:int}/excluir")]
    public async Task<IActionResult> AddressDelete(int id)
    {
        var r = await _addresses.DeleteAsync(UserId, id);
        if (!r.Success) TempData["Error"] = r.Error; else TempData["Success"] = "Endereço removido.";
        return RedirectToAction(nameof(Addresses));
    }

    // ---------- Pedidos ----------
    [HttpGet("pedidos")]
    public async Task<IActionResult> Orders(int page = 1)
    {
        page = Math.Max(page, 1);
        var q = _db.Orders.AsNoTracking().Where(o => o.UserId == UserId);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(o => o.CreatedAtUtc).Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
        return View(new OrderListVm { Orders = items, Page = page, TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize)) });
    }

    [HttpGet("pedidos/{id:guid}")]
    public async Task<IActionResult> OrderDetails(Guid id)
    {
        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.History.OrderBy(h => h.CreatedAtUtc))
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == UserId);
        if (order == null) return NotFound();
        ViewBag.PaymentEnabled = await _payments.IsEnabledAsync();
        return View(order);
    }

    [HttpPost("pedidos/{id:guid}/cancelar")]
    public async Task<IActionResult> CancelOrder(Guid id)
    {
        var r = await _orders.CancelByCustomerAsync(id, UserId);
        if (r.Success) TempData["Success"] = "Pedido cancelado."; else TempData["Error"] = r.Error;
        return RedirectToAction(nameof(OrderDetails), new { id });
    }

    [HttpPost("pedidos/{id:guid}/solicitar-cancelamento")]
    public async Task<IActionResult> RequestCancellation(Guid id)
    {
        var r = await _orders.RequestCancellationAsync(id, UserId);
        if (r.Success) TempData["Success"] = "Solicitação enviada. A loja analisará o cancelamento."; else TempData["Error"] = r.Error;
        return RedirectToAction(nameof(OrderDetails), new { id });
    }

    [HttpPost("pedidos/{id:guid}/pagar")]
    public async Task<IActionResult> Pay(Guid id)
    {
        var r = await _payments.StartCheckoutAsync(id, UserId);
        if (r.Success && !string.IsNullOrEmpty(r.Value)) return Redirect(r.Value);
        TempData["Error"] = r.Error;
        return RedirectToAction(nameof(OrderDetails), new { id });
    }
}
