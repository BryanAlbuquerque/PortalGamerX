using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using PortalGamerX.Domain.Rules;
using PortalGamerX.Services;

namespace PortalGamerX.Integrations.Shipping;

/// <summary>
/// Cotação via API do Melhor Envio (POST /api/v2/me/shipment/calculate).
/// Requer token de acesso (OAuth/Personal Access Token) e um e-mail de contato técnico (exigido no User-Agent).
/// Confirme campos e limites na documentação oficial: https://docs.melhorenvio.com.br
/// </summary>
public class MelhorEnvioProvider : IShippingProvider
{
    public const string HttpClientName = "melhorenvio";
    private readonly IHttpClientFactory _factory;
    private readonly IIntegrationSettings _settings;
    private readonly ILogger<MelhorEnvioProvider> _logger;

    public MelhorEnvioProvider(IHttpClientFactory factory, IIntegrationSettings settings, ILogger<MelhorEnvioProvider> logger)
    {
        _factory = factory;
        _settings = settings;
        _logger = logger;
    }

    public string Name => "MelhorEnvio";

    public async Task<bool> IsConfiguredAsync() => await _settings.HasValueAsync(SettingKeys.ShippingMeToken);

    private async Task<HttpRequestMessage?> BuildAsync(HttpMethod method, string path, string? json)
    {
        var token = await _settings.GetAsync(SettingKeys.ShippingMeToken);
        if (string.IsNullOrWhiteSpace(token)) return null;
        var env = await _settings.GetOrDefaultAsync(SettingKeys.ShippingMeEnvironment, "sandbox");
        var baseUrl = env == "production" ? "https://melhorenvio.com.br" : "https://sandbox.melhorenvio.com.br";
        var contact = await _settings.GetOrDefaultAsync(SettingKeys.ShippingMeContactEmail, "contato@example.com");

        var req = new HttpRequestMessage(method, baseUrl + path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        req.Headers.TryAddWithoutValidation("User-Agent", $"PortalGamerX ({contact})");
        if (json != null) req.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return req;
    }

    public async Task<ShippingQuoteResult> QuoteAsync(ShippingQuoteRequest request, CancellationToken ct = default)
    {
        var products = new JsonArray();
        foreach (var i in request.Items)
        {
            products.Add(new JsonObject
            {
                ["id"] = i.Id,
                // Limites mínimos aceitos pelas transportadoras (evita rejeição por medida abaixo do mínimo).
                ["width"] = (double)Math.Max(i.WidthCm, 11m),
                ["height"] = (double)Math.Max(i.HeightCm, 2m),
                ["length"] = (double)Math.Max(i.LengthCm, 16m),
                ["weight"] = (double)Math.Max(i.WeightKg, 0.1m),
                ["insurance_value"] = (double)Money.Round(i.UnitValue),
                ["quantity"] = i.Quantity
            });
        }

        var body = new JsonObject
        {
            ["from"] = new JsonObject { ["postal_code"] = Documents.OnlyDigits(request.OriginPostalCode) },
            ["to"] = new JsonObject { ["postal_code"] = Documents.OnlyDigits(request.DestinationPostalCode) },
            ["products"] = products
        };
        var services = await _settings.GetAsync(SettingKeys.ShippingMeServices);
        if (!string.IsNullOrWhiteSpace(services)) body["services"] = services;

        using var message = await BuildAsync(HttpMethod.Post, "/api/v2/me/shipment/calculate", body.ToJsonString());
        if (message == null) return ShippingQuoteResult.Fail("Frete não configurado (token do Melhor Envio ausente).");

        try
        {
            var client = _factory.CreateClient(HttpClientName);
            using var resp = await client.SendAsync(message, ct);
            var text = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Melhor Envio retornou {Status} na cotação.", (int)resp.StatusCode);
                return ShippingQuoteResult.Fail($"Serviço de frete indisponível no momento (HTTP {(int)resp.StatusCode}).");
            }

            var node = JsonNode.Parse(text);
            if (node is not JsonArray arr) return ShippingQuoteResult.Fail("Resposta inesperada do serviço de frete.");

            var options = new List<ShippingOption>();
            foreach (var item in arr)
            {
                if (item is not JsonObject o) continue;
                if (!string.IsNullOrWhiteSpace(o["error"]?.ToString())) continue; // serviço indisponível para este envio
                var price = ReadDecimal(o["custom_price"]) ?? ReadDecimal(o["price"]);
                if (price == null) continue;
                var days = (int?)ReadDecimal(o["custom_delivery_time"]) ?? (int?)ReadDecimal(o["delivery_time"]) ?? 0;
                var id = o["id"]?.ToString() ?? string.Empty;
                var name = o["name"]?.ToString() ?? "Serviço";
                var carrier = o["company"]?["name"]?.ToString() ?? string.Empty;
                options.Add(new ShippingOption($"me:{id}", name, carrier, Money.Round(price.Value), days, false));
            }

            return options.Count == 0
                ? ShippingQuoteResult.Fail("Nenhuma modalidade de entrega disponível para este CEP.")
                : new ShippingQuoteResult(true, options, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning("Falha de comunicação com o Melhor Envio: {Type}", ex.GetType().Name);
            return ShippingQuoteResult.Fail("Não foi possível consultar o frete agora. Tente novamente em instantes.");
        }
    }

    public async Task<ProviderResult<string>> TestConnectionAsync(CancellationToken ct = default)
    {
        using var message = await BuildAsync(HttpMethod.Get, "/api/v2/me", null);
        if (message == null) return ProviderResult<string>.Fail("Token do Melhor Envio não informado.");
        try
        {
            var client = _factory.CreateClient(HttpClientName);
            using var resp = await client.SendAsync(message, ct);
            return resp.IsSuccessStatusCode
                ? ProviderResult<string>.Ok("Token aceito pelo Melhor Envio.")
                : ProviderResult<string>.Fail($"O Melhor Envio recusou a credencial (HTTP {(int)resp.StatusCode}).");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ProviderResult<string>.Fail("Não foi possível contatar o Melhor Envio.");
        }
    }

    private static decimal? ReadDecimal(JsonNode? node)
    {
        if (node == null) return null;
        var s = node.ToString();
        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null;
    }
}
