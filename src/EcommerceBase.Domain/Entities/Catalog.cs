using EcommerceBase.Domain.Enums;
using EcommerceBase.Domain.Rules;

namespace EcommerceBase.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Brand
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }

    public decimal Price { get; set; }
    public decimal? PromotionalPrice { get; set; }

    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public int? BrandId { get; set; }
    public Brand? Brand { get; set; }

    /// <summary>Estoque disponível para venda (já descontadas as reservas de pedidos em aberto).</summary>
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 5;

    /// <summary>Peso em gramas (inteiro, evita ambiguidade de separador decimal).</summary>
    public int WeightGrams { get; set; }
    public decimal HeightCm { get; set; }
    public decimal WidthCm { get; set; }
    public decimal LengthCm { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }

    // Dados fiscais do produto (necessários para emissão de NF-e). Sem valores padrão fictícios.
    public string? Ncm { get; set; }
    public string? Cest { get; set; }
    public string CommercialUnit { get; set; } = "UN";
    /// <summary>Origem da mercadoria (0 a 8), conforme tabela fiscal.</summary>
    public int OriginCode { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

    public decimal EffectivePrice => PricingRules.EffectivePrice(Price, PromotionalPrice);
    public bool IsOnSale => PricingRules.IsOnSale(Price, PromotionalPrice);
    public bool IsPurchasable => IsActive && StockQuantity > 0;
}

public class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    /// <summary>Chave no armazenamento de arquivos (nunca um caminho informado pelo usuário).</summary>
    public string StorageKey { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
}

/// <summary>
/// Estrutura preparada para variantes (tamanho, cor...). Na v1 o carrinho/checkout opera sobre o produto;
/// as variantes são a base de evolução e ainda não participam do fluxo de compra.
/// </summary>
public class ProductVariant
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal? PriceOverride { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
}

public class StockMovement
{
    public long Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public StockMovementType Type { get; set; }
    /// <summary>Variação com sinal (positivo entra, negativo sai).</summary>
    public int QuantityDelta { get; set; }
    public int BalanceAfter { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int? OrderId { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
