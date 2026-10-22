using System.Globalization;

namespace EcommerceBase.Web.Domain.Rules;

/// <summary>Regras de arredondamento monetário: 2 casas, "away from zero". Nunca usar float/double.</summary>
public static class Money
{
    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>Formata como "R$ 1.234,56" sem depender de ICU/cultura instalada.</summary>
    public static string ToBrl(this decimal value)
    {
        var s = Round(value).ToString("N2", CultureInfo.InvariantCulture); // 1,234.56
        var chars = s.Select(c => c == ',' ? '.' : c == '.' ? ',' : c).ToArray();
        return "R$ " + new string(chars);
    }

    public static string ToBrl(this decimal? value) => value.HasValue ? value.Value.ToBrl() : "-";
}
