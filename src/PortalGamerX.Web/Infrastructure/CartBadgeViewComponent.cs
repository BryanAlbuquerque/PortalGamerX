using PortalGamerX.Services;
using Microsoft.AspNetCore.Mvc;

namespace PortalGamerX.Infrastructure;

public class CartBadgeViewComponent : ViewComponent
{
    private readonly ICartService _cart;
    public CartBadgeViewComponent(ICartService cart) => _cart = cart;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        int count;
        try { count = await _cart.CountAsync(); } catch { count = 0; }
        return View(count);
    }
}
