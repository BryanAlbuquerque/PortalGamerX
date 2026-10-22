using System.Security.Cryptography;
using System.Text;

namespace EcommerceBase.Web.Integrations.Payments;

/// <summary>
/// Validação da assinatura de webhooks do Mercado Pago (cabeçalho x-signature).
/// Formato: "ts=1704908010,v1=hash". Manifesto assinado: "id:[data.id];request-id:[x-request-id];ts:[ts];"
/// com HMAC-SHA256 usando a "chave secreta" do webhook. Campos ausentes são omitidos do manifesto.
/// Referência: documentação oficial de Webhooks do Mercado Pago.
/// </summary>
public static class MercadoPagoSignature
{
    public static bool TryParseHeader(string? header, out string ts, out string v1)
    {
        ts = string.Empty;
        v1 = string.Empty;
        if (string.IsNullOrWhiteSpace(header)) return false;
        foreach (var part in header.Split(','))
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;
            var k = kv[0].Trim();
            var v = kv[1].Trim();
            if (k == "ts") ts = v;
            else if (k == "v1") v1 = v;
        }
        return ts.Length > 0 && v1.Length > 0;
    }

    public static string BuildManifest(string? dataId, string? requestId, string ts)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(dataId))
        {
            // Se o id for alfanumérico, o Mercado Pago o assina em minúsculas.
            var normalized = dataId.All(char.IsLetterOrDigit) ? dataId.ToLowerInvariant() : dataId;
            sb.Append("id:").Append(normalized).Append(';');
        }
        if (!string.IsNullOrEmpty(requestId)) sb.Append("request-id:").Append(requestId).Append(';');
        sb.Append("ts:").Append(ts).Append(';');
        return sb.ToString();
    }

    public static string ComputeHex(string secret, string manifest)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest))).ToLowerInvariant();
    }

    public static bool IsValid(string secret, string? signatureHeader, string? requestId, string? dataId)
    {
        if (string.IsNullOrEmpty(secret)) return false;
        if (!TryParseHeader(signatureHeader, out var ts, out var v1)) return false;
        var expected = ComputeHex(secret, BuildManifest(dataId, requestId, ts));
        var a = Encoding.UTF8.GetBytes(expected);
        var b = Encoding.UTF8.GetBytes(v1.ToLowerInvariant());
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }
}
