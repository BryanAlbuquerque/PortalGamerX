using EcommerceBase.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceBase.Web.Areas.Admin.Controllers;

public class DashboardController : AdminBaseController
{
    private readonly IDashboardService _dashboard;
    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    public async Task<IActionResult> Index() => View(await _dashboard.GetAsync());
}
