using EcommerceBase.Web.Domain.Rules;
using EcommerceBase.Web.Integrations;
using EcommerceBase.Web.Integrations.Shipping;

namespace EcommerceBase.Web.Services;

public interface IShippingService
{
    Task<ShippingQuoteResult> QuoteAsync(string destinationPostalCode, IReadOnlyList<ShippingPackageItem> items, decimal subtotal);
    Task<ProviderResult<string>> TestConnectionAsync();
    Task<string> GetProviderNameAsync();
}

/// <summary>Escolhe o provedor configurado, aplica frete grátis, prazo de manuseio e valida a resposta.</summary>
public class ShippingService : IShippingService
{
    private readonly IEnumerable<IShippingProvider> _providers;
    private readonly IIntegrationSettings _settings;

    public ShippingService(IEnumerable<IShippingProvider> providers, IIntegrationSettings settings)
    {
        _providers = providers;
        _settings = settings;
    }

    public async Task<string> GetProviderNameAsync() => await _settings.GetOrDefaultAsync(SettingKeys.ShippingProvider, "None");

    private async Task<IShippingProvider?> ResolveAsync()
    {
        var name = await GetProviderNameAsync();
        return _providers.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<ShippingQuoteResult> QuoteAsync(string destinationPostalCode, IReadOnlyList<ShippingPackageItem> items, decimal subtotal)
    {
        if (!Documents.IsValidPostalCode(destinationPostalCode)) return ShippingQuoteResult.Fail("CEP inválido.");
        var provider = await ResolveAsync();
        if (provider == null) return ShippingQuoteResult.Fail("Cálculo de frete indisponível: nenhum provedor configurado.");

        var origin = await _settings.GetAsync(SettingKeys.ShippingOriginZip);
        if (!Documents.IsValidPostalCode(origin)) return ShippingQuoteResult.Fail("Cálculo de frete indisponível: CEP de origem não configurado.");
        if (!await provider.IsConfiguredAsync()) return ShippingQuoteResult.Fail("Cálculo de frete indisponível: credenciais do provedor não configuradas.");

        var result = await provider.QuoteAsync(new ShippingQuoteRequest(origin!, destinationPostalCode, items));
        if (!result.Success) return result;

        var handling = int.TryParse(await _settings.GetAsync(SettingKeys.ShippingHandlingDays), out var h) ? Math.Max(h, 0) : 0;
        var freeMin = FlexParse(await _settings.GetAsync(SettingKeys.ShippingFreeMinimum));

        var options = result.Options
            .Select(o => o with { DeliveryDays = o.DeliveryDays + handling })
            .OrderBy(o => o.Price).ThenBy(o => o.DeliveryDays)
            .ToList();

        if (freeMin.HasValue && freeMin.Value > 0 && subtotal >= freeMin.Value && options.Count > 0)
        {
            // Frete grátis na modalidade mais barata; demais modalidades continuam com preço cheio.
            options[0] = options[0] with { Price = 0m, Name = options[0].Name + " - FRETE GRÁTIS" };
        }
        return new ShippingQuoteResult(true, options, null, result.IsSimulated);
    }

    private static decimal? FlexParse(string? raw) =>
        Infrastructure.FlexibleDecimalModelBinder.TryParse(raw, out var v) ? v : null;

    public async Task<ProviderResult<string>> TestConnectionAsync()
    {
        var provider = await ResolveAsync();
        return provider == null
            ? ProviderResult<string>.Fail("Nenhum provedor de frete selecionado.")
            : await provider.TestConnectionAsync();
    }
}
