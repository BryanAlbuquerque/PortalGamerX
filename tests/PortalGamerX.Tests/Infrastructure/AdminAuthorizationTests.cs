using System.Reflection;
using PortalGamerX.Areas.Admin.Controllers;
using PortalGamerX.Domain;
using PortalGamerX.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace PortalGamerX.Tests.Infrastructure;

/// <summary>Garante por reflexão que nenhum controller do painel escapa da exigência de papel Admin.</summary>
public class AdminAuthorizationTests
{
    private static IEnumerable<Type> AdminControllers() =>
        typeof(AdminBaseController).Assembly.GetTypes()
            .Where(t => typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract)
            .Where(t => t.GetCustomAttributes<AreaAttribute>(true).Any(a => a.RouteValue == "Admin")
                        || t.Namespace == "PortalGamerX.Areas.Admin.Controllers");

    [Fact]
    public void Existem_controllers_administrativos() => Assert.NotEmpty(AdminControllers());

    [Fact]
    public void Todos_herdam_da_base_protegida()
    {
        foreach (var t in AdminControllers())
            Assert.True(typeof(AdminBaseController).IsAssignableFrom(t), $"{t.Name} não herda de AdminBaseController.");
    }

    [Fact]
    public void Base_exige_politica_AdminOnly_e_area_Admin()
    {
        var authorize = typeof(AdminBaseController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(Policies.AdminOnly, authorize!.Policy);
        Assert.Equal("Admin", typeof(AdminBaseController).GetCustomAttribute<AreaAttribute>()!.RouteValue);
    }

    [Fact]
    public void Nenhuma_action_admin_e_anonima()
    {
        foreach (var t in AdminControllers())
        {
            Assert.Null(t.GetCustomAttribute<AllowAnonymousAttribute>(true));
            foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                Assert.Null(m.GetCustomAttribute<AllowAnonymousAttribute>());
        }
    }

    [Fact]
    public void Convencao_adiciona_filtro_de_autorizacao_a_qualquer_controller_da_area()
    {
        var model = new ControllerModel(typeof(AdminBaseController).GetTypeInfo(), Array.Empty<object>());
        model.RouteValues["area"] = "Admin";
        new AdminAreaAuthorizationConvention().Apply(model);
        Assert.Contains(model.Filters, f => f is AuthorizeFilter);
    }

    [Fact]
    public void Convencao_nao_afeta_controllers_publicos()
    {
        var model = new ControllerModel(typeof(AdminBaseController).GetTypeInfo(), Array.Empty<object>());
        new AdminAreaAuthorizationConvention().Apply(model);
        Assert.DoesNotContain(model.Filters, f => f is AuthorizeFilter);
    }

    [Theory]
    [InlineData(typeof(PortalGamerX.Controllers.CheckoutController))]
    [InlineData(typeof(PortalGamerX.Controllers.ManageController))]
    public void Controllers_da_area_logada_exigem_autenticacao(Type t) =>
        Assert.NotNull(t.GetCustomAttribute<AuthorizeAttribute>());
}
