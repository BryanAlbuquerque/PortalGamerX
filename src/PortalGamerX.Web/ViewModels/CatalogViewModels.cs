using PortalGamerX.Domain;
using PortalGamerX.Domain.Entities;

namespace PortalGamerX.ViewModels;

public class JogoCardVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public decimal Price { get; set; }
    public decimal EffectivePrice { get; set; }
    public bool IsOnSale => EffectivePrice < Price;
    public bool IsPurchasable { get; set; }
    public string? CategoryName { get; set; }
    public GamePlatform Platform { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int Total { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}

public class JogoQuery
{
    public string? Q { get; set; }
    public string? Category { get; set; }
    public GamePlatform? Platform { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public class JogoListVm
{
    public JogoQuery Query { get; set; } = new();
    public PagedResult<JogoCardVm> Result { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public Category? CurrentCategory { get; set; }
}

public class JogoDetailsVm
{
    public Jogo Jogo { get; set; } = null!;
    public List<JogoCardVm> Related { get; set; } = new();
}

public class HomeVm
{
    public List<Banner> Banners { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<JogoCardVm> Featured { get; set; } = new();
    public List<JogoCardVm> Newest { get; set; } = new();
    public List<JogoCardVm> Offers { get; set; } = new();
    public List<JogoCardVm> BestSellers { get; set; } = new();
    public bool IsEmpty => Featured.Count == 0 && Newest.Count == 0 && Offers.Count == 0 && BestSellers.Count == 0;
}
