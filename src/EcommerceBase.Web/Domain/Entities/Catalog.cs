using System.ComponentModel.DataAnnotations;

namespace EcommerceBase.Web.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    [MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(120)] public string Slug { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public int? ParentId { get; set; }
    public Category? Parent { get; set; }
    public List<Category> Children { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public List<Jogos> Jogos { get; set; } = new();
}

public class Brand
{
    public int Id { get; set; }
    [MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(120)] public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class Jogos
{
    public int Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(220)] public string Slug { get; set; } = string.Empty;
    [MaxLength(500)] public string? ShortDescription { get; set; }
    public string? Description { get; set; }

    public decimal Price { get; set; }
    public decimal? PromotionalPrice { get; set; }

    [MaxLength(60)] public string Sku { get; set; } = string.Empty;
    [MaxLength(60)] public string? Barcode { get; set; }
    /// <summary>NCM (8 dígitos) - dado tributário usado pela integração fiscal.</summary>
    [MaxLength(8)] public string? Ncm { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public int? BrandId { get; set; }
    public Brand? Brand { get; set; }

    public int StockQuantity { get; set; }

    /// <summary>Peso em kg.</summary>
    public decimal WeightKg { get; set; }
    public decimal HeightCm { get; set; }
    public decimal WidthCm { get; set; }
    public decimal LengthCm { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    /// <summary>Chave manual de disponibilidade (independe do estoque).</summary>
    public bool IsAvailable { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<ProductImage> Images { get; set; } = new();

    /// <summary>Preço efetivo: promocional se válido (&gt;0 e menor que o preço), senão o preço normal.</summary>
    public decimal EffectivePrice =>
        PromotionalPrice.HasValue && PromotionalPrice.Value > 0 && PromotionalPrice.Value < Price
            ? PromotionalPrice.Value
            : Price;

    public bool IsOnSale => EffectivePrice < Price;

    public bool IsPurchasable => IsActive && IsAvailable && StockQuantity > 0;
}

public class ProductImage
{
    public int Id { get; set; }
    public int JogosId { get; set; }
    public Jogos? jogos { get; set; }
    [MaxLength(300)] public string Path { get; set; } = string.Empty;
    [MaxLength(200)] public string? AltText { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
}

public class Banner
{
    public int Id { get; set; }
    [MaxLength(150)] public string Title { get; set; } = string.Empty;
    [MaxLength(250)] public string? Subtitle { get; set; }
    [MaxLength(300)] public string ImagePath { get; set; } = string.Empty;
    [MaxLength(300)] public string? LinkUrl { get; set; }
    [MaxLength(50)] public string? ButtonText { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
}

public class StockMovement
{
    public long Id { get; set; }
    public int JogosId { get; set; }
    public Jogos? Jogos { get; set; }
    public StockMovementType Type { get; set; }
    /// <summary>Variação com sinal (+ entrada, - saída).</summary>
    public int Quantity { get; set; }
    public int BalanceAfter { get; set; }
    [MaxLength(300)] public string? Reason { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? UserId { get; set; }
    [MaxLength(150)] public string? UserName { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
