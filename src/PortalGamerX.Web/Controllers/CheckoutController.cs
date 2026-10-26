using PortalGamerX.Infrastructure;
using PortalGamerX.Services;
using PortalGamerX.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PortalGamerX.Controllers;

[Authorize]
[Route("checkout")]
public class CheckoutController : Controller
{
    private readonly ICartService _cart;
    private readonly IAddressService _addresses;
    private readonly ICheckoutService _checkout;
    private readonly IPaymentService _payments;

    public CheckoutController(ICartService cart, IAddressService addresses, ICheckoutService checkout, IPaymentService payments)
    {
        _cart = cart;
        _addresses = addresses;
        _checkout = checkout;
        _payments = payments;
    }

    private Guid UserId => User.GetUserId()!.Value;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var cart = await _cart.GetCartAsync();
        if (cart.IsEmpty) { TempData["Error"] = "Seu carrinho está vazio."; return RedirectToAction("Index", "Cart"); }
        if (cart.HasIssues) { TempData["Error"] = "Revise os itens do carrinho antes de continuar."; return RedirectToAction("Index", "Cart"); }

        var addresses = await _addresses.ListAsync(UserId);
        return View(new CheckoutVm
        {
            Cart = cart,
            Addresses = addresses,
            PaymentEnabled = await _payments.IsEnabledAsync(),
            SelectedAddressId = addresses.FirstOrDefault(a => a.IsDefault)?.Id ?? addresses.FirstOrDefault()?.Id
        });
    }

    [HttpPost("")]
    public async Task<IActionResult> Place(CheckoutInput input)
    {
        if (!ModelState.IsValid || input.IdempotencyKey == Guid.Empty)
        {
            TempData["Error"] = "Selecione o endereço de cobrança.";
            return RedirectToAction(nameof(Index));
        }
        if (!await _payments.IsEnabledAsync())
        {
            TempData["Error"] = "O pagamento online não está disponível no momento.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _checkout.PlaceOrderAsync(new PlaceOrderRequest(UserId, input.IdempotencyKey, input.AddressId, input.Notes));
        if (!result.Success || result.OrderId == null)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        var pay = await _payments.StartCheckoutAsync(result.OrderId.Value, UserId);
        if (pay.Success && !string.IsNullOrEmpty(pay.Value)) return Redirect(pay.Value);

        TempData["Error"] = "Seu pedido foi registrado, mas não foi possível iniciar o pagamento: " + pay.Error + " Você pode tentar novamente abaixo.";
        return RedirectToAction("OrderDetails", "Manage", new { id = result.OrderId.Value });
    }
}
