using System.Globalization;
using System.Text;

namespace EcommerceBase.Web.Domain.Rules;

/// <summary>Validação de formato e dígitos verificadores. NÃO comprova a regularidade do CPF/CNPJ na Receita Federal.</summary>
public static class Documents
{
    public static string OnlyDigits(string? value) =>
        string.IsNullOrEmpty(value) ? string.Empty : new string(value.Where(char.IsDigit).ToArray());

    public static bool IsValidCpf(string? input)
    {
        var d = OnlyDigits(input);
        if (d.Length != 11 || d.Distinct().Count() == 1) return false;
        return CheckDigit(d, 9, 10) == d[9] - '0' && CheckDigit(d, 10, 11) == d[10] - '0';
    }

    private static int CheckDigit(string d, int length, int startWeight)
    {
        var sum = 0;
        for (var i = 0; i < length; i++) sum += (d[i] - '0') * (startWeight - i);
        var r = sum * 10 % 11;
        return r == 10 ? 0 : r;
    }

    public static bool IsValidCnpj(string? input)
    {
        var d = OnlyDigits(input);
        if (d.Length != 14 || d.Distinct().Count() == 1) return false;
        int[] w1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] w2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        return CnpjDigit(d, w1) == d[12] - '0' && CnpjDigit(d, w2) == d[13] - '0';
    }

    private static int CnpjDigit(string d, int[] weights)
    {
        var sum = 0;
        for (var i = 0; i < weights.Length; i++) sum += (d[i] - '0') * weights[i];
        var r = sum % 11;
        return r < 2 ? 0 : 11 - r;
    }

    public static bool IsValidCpfOrCnpj(string? input)
    {
        var d = OnlyDigits(input);
        return d.Length == 11 ? IsValidCpf(d) : d.Length == 14 && IsValidCnpj(d);
    }

    public static bool IsValidPostalCode(string? input) => OnlyDigits(input).Length == 8;

    public static string FormatPostalCode(string? input)
    {
        var d = OnlyDigits(input);
        return d.Length == 8 ? d.Insert(5, "-") : d;
    }
}

public static class SlugHelper
{
    public static string Generate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "item";
        var normalized = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        var lastDash = false;
        foreach (var c in normalized)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat == UnicodeCategory.NonSpacingMark) continue;
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
            {
                sb.Append(c);
                lastDash = false;
            }
            else if (!lastDash && sb.Length > 0)
            {
                sb.Append('-');
                lastDash = true;
            }
        }
        var slug = sb.ToString().Trim('-');
        if (slug.Length > 100) slug = slug.Substring(0, 100).Trim('-');
        return slug.Length == 0 ? "item" : slug;
    }
}

public static class OrderNumberGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Generate(DateTime utcNow)
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(6);
        var sb = new StringBuilder("EB-");
        sb.Append(utcNow.ToString("yyMMdd"));
        sb.Append('-');
        foreach (var b in bytes) sb.Append(Alphabet[b % Alphabet.Length]);
        return sb.ToString();
    }
}
