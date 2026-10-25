using System.Security.Claims;
using System.Text;
using PortalGamerX.Domain;

namespace PortalGamerX.Infrastructure;

public static class ClaimsExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal? user)
    {
        var v = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(v, out var id) ? id : null;
    }

    public static bool IsAdmin(this ClaimsPrincipal? user) => user?.IsInRole(Roles.Admin) == true;
}

public static class StringExtensions
{
    public static string? NullIfEmpty(this string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    public static string Truncate(this string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Length <= max ? s : s.Substring(0, max);
    }

    /// <summary>Escapa curingas do LIKE/ILIKE (\ % _).</summary>
    public static string EscapeLike(this string s) =>
        s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    public static string ToBase64Url(this byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Base64UrlEncode(string text) => Encoding.UTF8.GetBytes(text).ToBase64Url();

    public static string Base64UrlDecode(string text)
    {
        var s = text.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Encoding.UTF8.GetString(Convert.FromBase64String(s));
    }
}
