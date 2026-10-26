using System.Security.Cryptography;
using System.Text;

namespace PortalGamerX.Domain.Rules;

/// <summary>Regras puras para códigos de ativação: normalização, validação, hash de duplicidade e máscara.</summary>
public static class KeyRules
{
    public const int MinLength = 4;
    public const int MaxLength = 120;
    public const int MaxPerImport = 2000;

    /// <summary>Remove espaços nas pontas. O conteúdo (maiúsculas/minúsculas, hífens) é preservado.</summary>
    public static string Normalize(string? code) => (code ?? string.Empty).Trim();

    /// <summary>Código válido: 4 a 120 caracteres imprimíveis, sem espaços internos nem caracteres de controle.</summary>
    public static bool IsValid(string? code)
    {
        var c = Normalize(code);
        if (c.Length < MinLength || c.Length > MaxLength) return false;
        foreach (var ch in c)
            if (char.IsWhiteSpace(ch) || char.IsControl(ch)) return false;
        return true;
    }

    /// <summary>
    /// Hash para detectar duplicidade (SHA-256 em hexadecimal, 64 caracteres). Ignora maiúsculas/minúsculas
    /// e hífens, para que "ABCD-1234" e "abcd1234" sejam tratados como a mesma key.
    /// </summary>
    public static string Hash(string code)
    {
        var canonical = Normalize(code).Replace("-", string.Empty).ToUpperInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("portalgamerx:key:" + canonical)));
    }

    /// <summary>Últimos 4 caracteres, usados para identificar a key no painel sem expor o código.</summary>
    public static string Hint(string code)
    {
        var c = Normalize(code);
        return c.Length <= 4 ? c : c[^4..];
    }

    public static string Mask(string? hint) => string.IsNullOrEmpty(hint) ? "••••" : "••••-••••-" + hint;

    /// <summary>
    /// Separa um texto colado (uma key por linha; vírgula e ponto e vírgula também separam) em códigos únicos.
    /// Retorna os válidos, sem repetições, e as contagens de inválidos e repetidos no próprio texto.
    /// </summary>
    public static (List<string> Codes, int Invalid, int RepeatedInText) Parse(string? text)
    {
        var codes = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var invalid = 0;
        var repeated = 0;
        var parts = (text ?? string.Empty).Split(new[] { '\r', '\n', ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var raw in parts)
        {
            var c = Normalize(raw);
            if (c.Length == 0) continue;
            if (!IsValid(c)) { invalid++; continue; }
            if (!seen.Add(Hash(c))) { repeated++; continue; }
            codes.Add(c);
        }
        return (codes, invalid, repeated);
    }
}
