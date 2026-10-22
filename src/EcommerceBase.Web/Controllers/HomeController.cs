using EcommerceBase.Web.Services;
using EcommerceBase.Web.ViewModels;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceBase.Web.Controllers;

public class HomeController : Controller
{
    private readonly ICatalogService _catalog;
    private readonly IStoreSettingsService _store;

    public HomeController(ICatalogService catalog, IStoreSettingsService store)
    {
        _catalog = catalog;
        _store = store;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await _catalog.GetHomeAsync());

    [HttpGet("sobre")]
    public async Task<IActionResult> About()
    {
        var s = await _store.GetAsync();
        return View("TextPage", new TextPageVm { Title = "Sobre nós", Text = s.AboutText });
    }

    [HttpGet("politicas/{slug}")]
    public async Task<IActionResult> Policy(string slug)
    {
        var s = await _store.GetAsync();
        var page = slug.ToLowerInvariant() switch
        {
            "termos" => new TextPageVm { Title = "Termos de uso", Text = s.TermsText },
            "privacidade" => new TextPageVm { Title = "Política de privacidade", Text = s.PrivacyText },
            "entrega" => new TextPageVm { Title = "Política de entrega", Text = s.ShippingPolicyText },
            "trocas" => new TextPageVm { Title = "Trocas e devoluções", Text = s.ReturnPolicyText },
            _ => null
        };
        return page == null ? NotFound() : View("TextPage", page);
    }

    // Aceita qualquer método: UseStatusCodePagesWithReExecute reexecuta com o método original (ex.: POST).
    [Route("erro")]
    [Route("erro/{code:int}")]
    [IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? code)
    {
        var status = code ?? 500;
        var vm = new ErrorVm { Code = status };
        switch (status)
        {
            case 404: vm.Title = "Página não encontrada"; vm.Message = "O endereço acessado não existe ou foi removido."; break;
            case 403: vm.Title = "Acesso negado"; vm.Message = "Você não tem permissão para acessar este recurso."; break;
            case 429: vm.Title = "Muitas requisições"; vm.Message = "Aguarde um instante e tente novamente."; break;
            default: vm.Title = "Ocorreu um erro"; vm.Message = "Não foi possível concluir a operação. Tente novamente em instantes."; break;
        }
        Response.StatusCode = status;
        return View(vm);
    }
}
