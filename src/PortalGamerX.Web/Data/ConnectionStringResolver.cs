using Npgsql;

namespace PortalGamerX.Data;

public static class ConnectionStringResolver
{
    /// <summary>DATABASE_URL (formato Render) tem prioridade; senão ConnectionStrings:Default.</summary>
    public static string Resolve(IConfiguration cfg)
    {
        var url = cfg["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(url)) return FromUrl(url, cfg["DATABASE_SSLMODE"]);

        var cs = cfg.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException(
                "Conexão PostgreSQL não configurada. Defina DATABASE_URL ou ConnectionStrings__Default.");
        return cs;
    }

    public static string FromUrl(string url, string? sslMode = null)
    {
        if (!url.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return url; // já está no formato chave=valor

        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null
        };

        var mode = SslMode.Prefer;
        if (!string.IsNullOrWhiteSpace(sslMode) && Enum.TryParse<SslMode>(sslMode, true, out var parsed)) mode = parsed;
        builder.SslMode = mode;
        if (mode == SslMode.Require) builder.TrustServerCertificate = true;
        return builder.ToString();
    }
}
