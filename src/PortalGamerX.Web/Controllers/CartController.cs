using PortalGamerX.Infrastructure;
using PortalGamerX.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace PortalGamerX.Controllers;

[Route("carrinho")]
public class CartController : Controller
{
    private readonly ICartService _cart;
    private readonly IShippingService _shipping;

    public CartController(ICartService cart, IShippingService shipping)
    {
        _cart = cart;
        _shipping = shipping;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await _cart.GetCartAsync());

    [HttpPost("adicionar")]
    public async Task<IActionResult> Add(int jogoId, int quantity = 1, string? returnUrl = null)
    {
        var r = await _cart.AddAsync(jogoId, quantity);
        if (!r.Success)
        {
            TempData["Error"] = r.Error;
            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/carrinho");
        }
        TempData["Success"] = "Jogo adicionado ao carrinho.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("atualizar")]
    public async Task<IActionResult> Update(int jogoId, int quantity)
    {
        var r = await _cart.SetQuantityAsync(jogoId, quantity);
        if (!r.Success) TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("remover")]
    public async Task<IActionResult> Remove(int jogoId)
    {
        await _cart.RemoveAsync(jogoId);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("cupom")]
    public async Task<IActionResult> ApplyCoupon(string code)
    {
        var r = await _cart.ApplyCouponAsync(code ?? string.Empty);
        if (r.Success) TempData["Success"] = "Cupom aplicado."; else TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("cupom/remover")]
    public async Task<IActionResult> RemoveCoupon()
    {
        await _cart.RemoveCouponAsync();
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Estimativa de frete por CEP (informativa). O valor final é recalculado no servidor ao finalizar o pedido.</summary>
    [HttpPost("frete")]
    [EnableRateLimiting("shipping")]
    public async Task<IActionResult> EstimateShipping(string postalCode)
    {
        var (items, subtotal) = await _cart.GetShippingPackageAsync();
        if (items.Count == 0) return Json(new { success = false, error = "Carrinho vazio." });
        var q = await _shipping.QuoteAsync(postalCode ?? string.Empty, items, subtotal);
        return Json(ShippingJson.From(q));
    }
}
