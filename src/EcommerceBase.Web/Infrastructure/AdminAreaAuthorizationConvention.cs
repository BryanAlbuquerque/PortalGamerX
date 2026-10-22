using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using EcommerceBase.Web.Domain;

namespace EcommerceBase.Web.Infrastructure;

/// <summary>
/// Defesa em profundidade: qualquer controller da área "Admin" recebe a política AdminOnly,
/// mesmo que alguém esqueça o atributo [Authorize]. Visibilidade de botões nunca é proteção.
/// </summary>
public class AdminAreaAuthorizationConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        var isAdminArea = controller.RouteValues.TryGetValue("area", out var area)
                          && string.Equals(area, "Admin", StringComparison.OrdinalIgnoreCase);
        if (isAdminArea) controller.Filters.Add(new AuthorizeFilter(Policies.AdminOnly));
    }
}
