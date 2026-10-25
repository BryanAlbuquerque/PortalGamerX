using PortalGamerX.Domain;
using PortalGamerX.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PortalGamerX.Areas.Admin.Controllers;

/// <summary>
/// Base de TODOS os controllers administrativos. A autorização é exigida no servidor (política AdminOnly);
/// a convenção AdminAreaAuthorizationConvention a reforça para qualquer controller da área.
/// </summary>
[Area("Admin")]
[Authorize(Policy = Policies.AdminOnly)]
public abstract class AdminBaseController : Controller
{
    protected Guid? CurrentUserId => User.GetUserId();
    protected string CurrentUserName => User.Identity?.Name ?? "admin";
    protected const int PageSize = 20;

    protected static int TotalPagesFor(int total) => Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
}
