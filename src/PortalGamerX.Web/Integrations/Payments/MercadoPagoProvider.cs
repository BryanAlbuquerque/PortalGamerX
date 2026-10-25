using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using PortalGamerX.Domain;
using PortalGamerX.Services;

namespace PortalGamerX.Integrations.Payments;

/// <summary>
/// Adaptador do Mercado Pago (Checkout Pro) usando a API REST oficial:
///  - POST /checkout/preferences        (cria a preferência; o cliente paga no ambiente do Mercado Pago)
///  - GET  /v1/payments/{id}            (consulta autenticada do pagamento)
///  - POST /v1/payments/{id}/refunds    (reembolso)
///  - GET  /users/me                    (teste da credencial)
/// O valor cobrado é o total do pedido calculado no servidor (item único), garantindo consistência com descontos e frete.
/// Nenhum dado de cartão trafega ou é armazenado por esta aplicação.
/// </summary>
public class MercadoPagoProvider : IPaymentProvider
{
    public const string HttpClientName = "mercadopago";
    public const string ProviderName = "MercadoPago";
    private readonly IHttpClientFactory _factory;
    private readonly IIntegrationSettings _settings;
    private readonly ILogger<MercadoPagoProvider> _logger;

    public MercadoPagoProvider(IHttpClientFactory factory, IIntegrationSettings settings, ILogger<MercadoPagoProvider> logger)
    {
        _factory = factory;
        _settings = settings;
        _logger = logger;
    }

    public string Name => ProviderName;

    public async Task<bool> IsConfiguredAsync() => await _settings.HasValueAsync(SettingKeys.MpAccessToken);

    public static PaymentStatus MapStatus(string? raw) => (raw ?? string.Empty).ToLowerInvariant() switch
    {
        "approved" => PaymentStatus.Approved,
        "pending" => PaymentStatus.Pending,
        "authorized" => PaymentStatus.InProcess,
        "in_process" => PaymentStatus.InProcess,
        "in_mediation" => PaymentStatus.InProcess,
        "rejected" => PaymentStatus.Rejected,
        "cancelled" => PaymentStatus.Cancelled,
        "refunded" => PaymentStatus.Refunded,
        "charged_back" => PaymentStatus.ChargedBack,
        _ => PaymentStatus.Pending
    };

    private async Task<HttpRequestMessage?> BuildAsync(HttpMethod method, string path, string? json, string? idempotencyKey = null)
    {
        var token = await _settings.GetAsync(SettingKeys.MpAccessToken);
        if (string.IsNullOrWhiteSpace(token)) return null;
        var req = new HttpRequestMessage(method, path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrEmpty(idempotencyKey)) req.Headers.TryAddWithoutValidation("X-Idempotency-Key", idempotencyKey);
        if (json != null) req.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return req;
    }

    public async Task<ProviderResult<CheckoutSession>> CreateCheckoutAsync(PaymentRequest r, CancellationToken ct = default)
    {
        var body = new JsonObject
        {
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = r.OrderNumber,
                    ["title"] = r.Description,
                    ["quantity"] = 1,
                    ["currency_id"] = "BRL",
                    ["unit_price"] = (double)r.Total
                }
            },
            ["payer"] = new JsonObject { ["email"] = r.PayerEmail, ["name"] = r.PayerName },
            ["external_reference"] = r.OrderNumber,
            ["back_urls"] = new JsonObject
            {
                ["success"] = r.SuccessUrl,
                ["failure"] = r.FailureUrl,
                ["pending"] = r.PendingUrl
            }
        };
        // auto_return e notification_url exigem URL pública HTTPS.
        if (r.SuccessUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) body["auto_return"] = "approved";
        if (r.NotificationUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) body["notification_url"] = r.NotificationUrl;
        if (r.ExpiresAtUtc.HasValue)
        {
            body["expires"] = true;
            body["expiration_date_to"] = r.ExpiresAtUtc.Value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture) + "+00:00";
        }

        using var message = await BuildAsync(HttpMethod.Post, "checkout/preferences", body.ToJsonString(), r.IdempotencyKey);
        if (message == null) return ProviderResult<CheckoutSession>.Fail("Mercado Pago não configurado (Access Token ausente).");

        try
        {
            var client = _factory.CreateClient(HttpClientName);
            using var resp = await client.SendAsync(message, ct);
            var text = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Mercado Pago recusou a criação da preferência: HTTP {Status}", (int)resp.StatusCode);
                return ProviderResult<CheckoutSession>.Fail($"O Mercado Pago recusou a solicitação (HTTP {(int)resp.StatusCode}). Verifique as credenciais no painel.");
            }
            var json = JsonNode.Parse(text);
            var id = json?["id"]?.ToString();
            var env = await _settings.GetOrDefaultAsync(SettingKeys.MpEnvironment, "sandbox");
            var sandbox = env != "production";
            var url = sandbox ? json?["sandbox_init_point"]?.ToString() : json?["init_point"]?.ToString();
            url ??= json?["init_point"]?.ToString();
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(url))
                return ProviderResult<CheckoutSession>.Fail("Resposta inesperada do Mercado Pago.");
            return ProviderResult<CheckoutSession>.Ok(new CheckoutSession(id, url, sandbox));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning("Falha de comunicação com o Mercado Pago: {Type}", ex.GetType().Name);
            return ProviderResult<CheckoutSession>.Fail("Não foi possível contatar o Mercado Pago. Tente novamente.");
        }
    }

    public async Task<ProviderResult<ProviderPayment>> GetPaymentAsync(string externalPaymentId, CancellationToken ct = default)
    {
        if (externalPaymentId.Any(c => !char.IsDigit(c))) return ProviderResult<ProviderPayment>.Fail("Identificador de pagamento inválido.");
        using var message = await BuildAsync(HttpMethod.Get, $"v1/payments/{externalPaymentId}", null);
        if (message == null) return ProviderResult<ProviderPayment>.Fail("Mercado Pago não configurado.");
        try
        {
            var client = _factory.CreateClient(HttpClientName);
            using var resp = await client.SendAsync(message, ct);
            var text = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                return ProviderResult<ProviderPayment>.Fail($"Consulta recusada pelo Mercado Pago (HTTP {(int)resp.StatusCode}).");

            var j = JsonNode.Parse(text);
            if (j == null) return ProviderResult<ProviderPayment>.Fail("Resposta inválida do Mercado Pago.");
            var raw = j["status"]?.ToString() ?? string.Empty;
            return ProviderResult<ProviderPayment>.Ok(new ProviderPayment(
                j["id"]?.ToString() ?? externalPaymentId,
                MapStatus(raw),
                raw,
                j["status_detail"]?.ToString(),
                j["external_reference"]?.ToString(),
                ParseDecimal(j["transaction_amount"]),
                j["payment_method_id"]?.ToString(),
                j["date_last_updated"]?.ToString(),
                ParseDecimal(j["transaction_amount_refunded"])));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ProviderResult<ProviderPayment>.Fail("Não foi possível contatar o Mercado Pago.");
        }
    }

    public async Task<ProviderResult<string>> RefundAsync(string externalPaymentId, decimal? amount, string idempotencyKey, CancellationToken ct = default)
    {
        if (externalPaymentId.Any(c => !char.IsDigit(c))) return ProviderResult<string>.Fail("Identificador de pagamento inválido.");
        var body = new JsonObject();
        if (amount.HasValue) body["amount"] = (double)amount.Value;
        using var message = await BuildAsync(HttpMethod.Post, $"v1/payments/{externalPaymentId}/refunds", body.ToJsonString(), idempotencyKey);
        if (message == null) return ProviderResult<string>.Fail("Mercado Pago não configurado.");
        try
        {
            var client = _factory.CreateClient(HttpClientName);
            using var resp = await client.SendAsync(message, ct);
            return resp.IsSuccessStatusCode
                ? ProviderResult<string>.Ok("Reembolso solicitado ao Mercado Pago.")
                : ProviderResult<string>.Fail($"O Mercado Pago recusou o reembolso (HTTP {(int)resp.StatusCode}).");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ProviderResult<string>.Fail("Não foi possível contatar o Mercado Pago.");
        }
    }

    public async Task<ProviderResult<string>> TestConnectionAsync(CancellationToken ct = default)
    {
        using var message = await BuildAsync(HttpMethod.Get, "users/me", null);
        if (message == null) return ProviderResult<string>.Fail("Access Token não informado.");
        try
        {
            var client = _factory.CreateClient(HttpClientName);
            using var resp = await client.SendAsync(message, ct);
            return resp.IsSuccessStatusCode
                ? ProviderResult<string>.Ok("Credencial aceita pelo Mercado Pago.")
                : ProviderResult<string>.Fail($"O Mercado Pago recusou a credencial (HTTP {(int)resp.StatusCode}).");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ProviderResult<string>.Fail("Não foi possível contatar o Mercado Pago.");
        }
    }

    public async Task<bool> ValidateWebhookAsync(string? signatureHeader, string? requestId, string? dataId)
    {
        var secret = await _settings.GetAsync(SettingKeys.MpWebhookSecret);
        return !string.IsNullOrEmpty(secret) && MercadoPagoSignature.IsValid(secret, signatureHeader, requestId, dataId);
    }

    private static decimal ParseDecimal(JsonNode? n) =>
        n != null && decimal.TryParse(n.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : 0m;
}
