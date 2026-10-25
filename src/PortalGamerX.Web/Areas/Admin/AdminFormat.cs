using System.Globalization;
using PortalGamerX.Domain.Entities;
using PortalGamerX.Services;

namespace PortalGamerX.Areas.Admin;

/// <summary>Formatação de datas do painel no fuso horário configurado da loja.</summary>
public static class AdminFormat
{
    public static string Fmt(this IStoreSettingsService svc, DateTime? utc, StoreSettings settings, bool withTime = true)
    {
        if (!utc.HasValue) return "-";
        var local = svc.ToLocal(utc.Value, settings);
        return local.ToString(withTime ? "dd/MM/yyyy HH:mm" : "dd/MM/yyyy", CultureInfo.InvariantCulture);
    }

    /// <summary>Valor para input datetime-local (yyyy-MM-ddTHH:mm).</summary>
    public static string ToInput(this DateTime? local) => local.HasValue ? local.Value.ToString("yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture) : string.Empty;

    /// <summary>Decimal para campo de texto no formato brasileiro (vírgula).</summary>
    public static string Dec(this decimal? v) => v.HasValue ? v.Value.ToString("0.##", new CultureInfo("pt-BR")) : string.Empty;
    public static string Dec(this decimal v) => v.ToString("0.##", new CultureInfo("pt-BR"));
}
