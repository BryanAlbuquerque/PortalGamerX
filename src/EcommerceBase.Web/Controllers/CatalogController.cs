using EcommerceBase.Web.Services;
using EcommerceBase.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceBase.Web.Controllers;

public class CatalogController : Controller
{
    private readonly ICatalogService _catalog;
    public CatalogController(ICatalogService catalog) => _catalog = catalog;

    [HttpGet("produtos")]
    public async Task<IActionResult> Index(ProductQuery query) => View(await _catalog.SearchAsync(query));

    [HttpGet("categoria/{slug}")]
    public async Task<IActionResult> Category(string slug, ProductQuery query)
    {
        query.Category = slug;
        var vm = await _catalog.SearchAsync(query);
        if (vm.CurrentCategory == null) return NotFound();
        return View("Index", vm);
    }

    [HttpGet("produto/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var vm = await _catalog.GetDetailsAsync(slug);
        return vm == null ? NotFound() : View(vm);
    }
}
