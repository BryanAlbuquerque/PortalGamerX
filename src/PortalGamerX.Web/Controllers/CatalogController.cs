using PortalGamerX.Services;
using PortalGamerX.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace PortalGamerX.Controllers;

public class CatalogController : Controller
{
    private readonly ICatalogService _catalog;
    public CatalogController(ICatalogService catalog) => _catalog = catalog;

    [HttpGet("jogos")]
    public async Task<IActionResult> Index(JogoQuery query) => View(await _catalog.SearchAsync(query));

    [HttpGet("categoria/{slug}")]
    public async Task<IActionResult> Category(string slug, JogoQuery query)
    {
        query.Category = slug;
        var vm = await _catalog.SearchAsync(query);
        if (vm.CurrentCategory == null) return NotFound();
        return View("Index", vm);
    }

    [HttpGet("jogo/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var vm = await _catalog.GetDetailsAsync(slug);
        return vm == null ? NotFound() : View(vm);
    }
}
