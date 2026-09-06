using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalGamerX.ViewModels;

namespace PortalGamerX.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class JogosController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(JogosAdminViewModel jogos)
    {
        if (!ModelState.IsValid)
        {
            return View();
        }

        // Salvar jogo

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        return View();
    }

    public IActionResult Delete(int id)
    {
        return View();
    }
}