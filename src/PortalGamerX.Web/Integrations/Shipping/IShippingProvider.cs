namespace PortalGamerX.Integrations.Shipping;

public record ShippingPackageItem(string Id, int Quantity, decimal WeightKg, decimal HeightCm, decimal WidthCm, decimal LengthCm, decimal UnitValue);

public record ShippingQuoteRequest(string OriginPostalCode, string DestinationPostalCode, IReadOnlyList<ShippingPackageItem> Items);

public record ShippingOption(string Code, string Name, string Carrier, decimal Price, int DeliveryDays, bool IsSimulated);

public record ShippingQuoteResult(bool Success, IReadOnlyList<ShippingOption> Options, string? Error, bool IsSimulated = false)
{
    public static ShippingQuoteResult Fail(string error) => new(false, Array.Empty<ShippingOption>(), error);
}

public interface IShippingProvider
{
    string Name { get; }
    Task<bool> IsConfiguredAsync();
    Task<ShippingQuoteResult> QuoteAsync(ShippingQuoteRequest request, CancellationToken ct = default);
    Task<ProviderResult<string>> TestConnectionAsync(CancellationToken ct = default);
}
