namespace EcommerceBase.Web.Infrastructure;

public interface IPublicUrl
{
    /// <summary>URL absoluta a partir de um caminho (ex.: "/conta/entrar"). Usa App:PublicBaseUrl quando definido.</summary>
    string Absolute(string path);
}

public class PublicUrlService : IPublicUrl
{
    private readonly IConfiguration _cfg;
    private readonly IHttpContextAccessor _http;

    public PublicUrlService(IConfiguration cfg, IHttpContextAccessor http)
    {
        _cfg = cfg;
        _http = http;
    }

    public string Absolute(string path)
    {
        var baseUrl = _cfg["App:PublicBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            var req = _http.HttpContext?.Request;
            baseUrl = req != null ? $"{req.Scheme}://{req.Host}" : "http://localhost";
        }
        return baseUrl.TrimEnd('/') + (path.StartsWith('/') ? path : "/" + path);
    }
}
