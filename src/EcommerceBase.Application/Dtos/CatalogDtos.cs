namespace EcommerceBase.Application.Dtos;

public class ProductListQuery
{
    public string? Q { get; set; }
    public string? Category { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    /// <summary>newest | price_asc | price_desc | name_asc | name_desc</summary>
    public string? Sort { get; set; }
    public bool OnSale { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public class ProductCardDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public decimal Price { get; set; }
    public decimal? PromotionalPrice { get; set; }
    public decimal EffectivePrice { get; set; }
    public bool IsOnSale { get; set; }
    public string? ImageKey { get; set; }
    public bool InStock { get; set; }
    public string? CategoryName { get; set; }
}

public class ProductImageDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Alt { get; set; }
    public bool IsPrimary { get; set; }
}

public class ProductDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal Price { get; set; }
    public decimal? PromotionalPrice { get; set; }
    public decimal EffectivePrice { get; set; }
    public bool IsOnSale { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public string? BrandName { get; set; }
    public int StockQuantity { get; set; }
    public bool IsPurchasable { get; set; }
    public bool IsLowStock { get; set; }
    public int WeightGrams { get; set; }
    public List<ProductImageDto> Images { get; set; } = new();
    public List<ProductCardDto> Related { get; set; } = new();
}

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ProductCount { get; set; }
}

public class BannerDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string ImageKey { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
}

public class HomeDto
{
    public List<BannerDto> Banners { get; set; } = new();
    public List<ProductCardDto> Featured { get; set; } = new();
    public List<ProductCardDto> NewArrivals { get; set; } = new();
    public List<ProductCardDto> Offers { get; set; } = new();
    public List<ProductCardDto> BestSellers { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public bool StoreIsEmpty { get; set; }
}

public class ContentPageDto
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool ShowInFooter { get; set; }
}
