using PortalGamerX.Services;
using Microsoft.AspNetCore.Mvc;

namespace PortalGamerX.Areas.Admin.Controllers;

public class DashboardController : AdminBaseController
{
    private readonly IDashboardService _dashboard;
    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    public async Task<IActionResult> Index() => View(await _dashboard.GetAsync());
}
