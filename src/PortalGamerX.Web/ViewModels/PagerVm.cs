namespace PortalGamerX.ViewModels;

public class PagerVm
{
    public int Page { get; set; }
    public int TotalPages { get; set; }
    /// <summary>URL base já terminada em "?" ou "&amp;" (ex.: "/jogos?q=abc&amp;").</summary>
    public string BaseUrl { get; set; } = "?";

    public static PagerVm Create(int page, int totalPages, string baseUrl) =>
        new() { Page = page, TotalPages = totalPages, BaseUrl = baseUrl };
}
