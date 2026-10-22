using EcommerceBase.Web.Domain.Rules;

namespace EcommerceBase.Web.Integrations.Shipping;

/// <summary>
/// MODO DE DESENVOLVIMENTO: tabela fixa por região do CEP. NÃO é cotação real.
/// Toda opção é marcada como simulada e isso é exibido ao cliente e gravado no pedido.
/// </summary>
public class DevelopmentShippingProvider : IShippingProvider
{
    public string Name => "Development";

    public Task<bool> IsConfiguredAsync() => Task.FromResult(true);

    public Task<ShippingQuoteResult> QuoteAsync(ShippingQuoteRequest request, CancellationToken ct = default)
    {
        var dest = Documents.OnlyDigits(request.DestinationPostalCode);
        var origin = Documents.OnlyDigits(request.OriginPostalCode);
        if (dest.Length != 8) return Task.FromResult(ShippingQuoteResult.Fail("CEP de destino inválido."));

        var sameRegion = origin.Length >= 1 && origin[0] == dest[0];
        var weight = request.Items.Sum(i => i.WeightKg * i.Quantity);
        var extra = Math.Ceiling(Math.Max(weight, 0.1m)) * 2m;

        var options = new List<ShippingOption>
        {
            new("dev:standard", "Padrão (SIMULADO)", "Simulação", Money.Round((sameRegion ? 12m : 24m) + extra), sameRegion ? 4 : 8, true),
            new("dev:express", "Expresso (SIMULADO)", "Simulação", Money.Round((sameRegion ? 22m : 42m) + extra * 1.5m), sameRegion ? 2 : 4, true)
        };
        return Task.FromResult(new ShippingQuoteResult(true, options, null, true));
    }

    public Task<ProviderResult<string>> TestConnectionAsync(CancellationToken ct = default) =>
        Task.FromResult(ProviderResult<string>.Ok("Modo de desenvolvimento ativo (valores simulados)."));
}
