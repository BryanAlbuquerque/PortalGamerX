using System.Text;
using System.Text.RegularExpressions;

namespace EcommerceBase.Domain.Rules;

/// <summary>
/// Validação de FORMATO e dígitos verificadores. Isto NÃO comprova a regularidade cadastral
/// de um CPF ou CNPJ na Receita Federal.
/// </summary>
public static class BrazilianDocuments
{
    private static readonly int[] CnpjWeights1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
    private static readonly int[] CnpjWeights2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

    public static readonly string[] StateCodes =
    {
        "AC","AL","AP","AM","BA","CE","DF","ES","GO","MA","MT","MS","MG","PA",
        "PB","PR","PE","PI","RJ","RN","RS","RO","RR","SC","SP","SE","TO"
    };

    public static string OnlyDigits(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
            if (c >= '0' && c <= '9') sb.Append(c);
        return sb.ToString();
    }

    public static string NormalizeCep(string? value) => OnlyDigits(value);

    public static bool IsValidCep(string? value) => NormalizeCep(value).Length == 8;

    public static bool IsValidState(string? value) =>
        !string.IsNullOrWhiteSpace(value) && StateCodes.Contains(value.Trim().ToUpperInvariant());

    public static bool IsValidCpf(string? value)
    {
        var d = OnlyDigits(value);
        if (d.Length != 11 || d.Distinct().Count() == 1) return false;
        return CpfDigit(d, 9) == d[9] - '0' && CpfDigit(d, 10) == d[10] - '0';
    }

    private static int CpfDigit(string digits, int length)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
            sum += (digits[i] - '0') * (length + 1 - i);
        var r = sum * 10 % 11;
        return r == 10 ? 0 : r;
    }

    /// <summary>Remove máscara e coloca em maiúsculas. Aceita CNPJ numérico e o alfanumérico (a partir de 2026).</summary>
    public static string NormalizeCnpj(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return Regex.Replace(value.ToUpperInvariant(), "[^0-9A-Z]", string.Empty);
    }

    public static bool IsValidCnpj(string? value)
    {
        var c = NormalizeCnpj(value);
        if (c.Length != 14) return false;
        if (c.Distinct().Count() == 1) return false;
        for (var i = 0; i < 12; i++)
            if (!char.IsAsciiLetterOrDigit(c[i]) || char.IsAsciiLetterLower(c[i])) return false;
        if (!char.IsAsciiDigit(c[12]) || !char.IsAsciiDigit(c[13])) return false;

        return CnpjDigit(c, CnpjWeights1) == c[12] - '0' && CnpjDigit(c, CnpjWeights2) == c[13] - '0';
    }

    private static int CnpjDigit(string cnpj, int[] weights)
    {
        var sum = 0;
        for (var i = 0; i < weights.Length; i++)
            sum += (cnpj[i] - '0') * weights[i]; // valor do caractere = código ASCII - 48 (regra do CNPJ alfanumérico)
        var r = sum % 11;
        return r < 2 ? 0 : 11 - r;
    }

    public static string FormatCpf(string? value)
    {
        var d = OnlyDigits(value);
        return d.Length == 11 ? $"{d[..3]}.{d[3..6]}.{d[6..9]}-{d[9..]}" : value ?? string.Empty;
    }

    public static string FormatCnpj(string? value)
    {
        var c = NormalizeCnpj(value);
        return c.Length == 14 ? $"{c[..2]}.{c[2..5]}.{c[5..8]}/{c[8..12]}-{c[12..]}" : value ?? string.Empty;
    }

    public static string FormatCep(string? value)
    {
        var d = NormalizeCep(value);
        return d.Length == 8 ? $"{d[..5]}-{d[5..]}" : value ?? string.Empty;
    }

    /// <summary>CPF (11 dígitos) ou CNPJ (14 caracteres).</summary>
    public static bool IsValidCpfOrCnpj(string? value)
    {
        var digits = OnlyDigits(value);
        if (digits.Length == 11 && NormalizeCnpj(value).Length == 11) return IsValidCpf(value);
        return IsValidCnpj(value);
    }
}
