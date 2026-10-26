using System.ComponentModel.DataAnnotations;

namespace PortalGamerX.Domain.Entities;

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
    public List<Jogo> Jogos { get; set; } = new();
}

public class Brand
{
    public int Id { get; set; }
    [MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(120)] public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class Jogo
{
    public int Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(220)] public string Slug { get; set; } = string.Empty;
    [MaxLength(500)] public string? ShortDescription { get; set; }
    public string? Description { get; set; }

    public decimal Price { get; set; }
    public decimal? PromotionalPrice { get; set; }

    [MaxLength(60)] public string Sku { get; set; } = string.Empty;

    // ---- Dados do jogo / da key ----
    public GamePlatform Platform { get; set; } = GamePlatform.Outro;
    /// <summary>Loja onde o cliente resgata a key (Steam, Epic, PlayStation Store...).</summary>
    public ActivationStore ActivationStore { get; set; } = ActivationStore.Steam;
    public KeyRegion Region { get; set; } = KeyRegion.Global;
    public AgeRating AgeRating { get; set; } = AgeRating.Livre;
    public DateOnly? ReleaseDate { get; set; }
    /// <summary>Passo a passo de ativação exibido ao cliente junto com a key (texto simples).</summary>
    [MaxLength(2000)] public string? ActivationInstructions { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public int? BrandId { get; set; }
    public Brand? Brand { get; set; }

    /// <summary>
    /// Quantidade de keys DISPONÍVEIS. É um contador mantido junto com o inventário (<see cref="GameKey"/>):
    /// só muda ao importar, reservar, liberar ou invalidar keys.
    /// </summary>
    public int StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    /// <summary>Chave manual de disponibilidade (independe do estoque).</summary>
    public bool IsAvailable { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<JogoImage> Images { get; set; } = new();

    /// <summary>Preço efetivo: promocional se válido (&gt;0 e menor que o preço), senão o preço normal.</summary>
    public decimal EffectivePrice =>
        PromotionalPrice.HasValue && PromotionalPrice.Value > 0 && PromotionalPrice.Value < Price
            ? PromotionalPrice.Value
            : Price;

    public bool IsOnSale => EffectivePrice < Price;

    public bool IsPurchasable => IsActive && IsAvailable && StockQuantity > 0;
}

public class JogoImage
{
    public int Id { get; set; }
    public int JogoId { get; set; }
    public Jogo? Jogo { get; set; }
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
    public int JogoId { get; set; }
    public Jogo? Jogo { get; set; }
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

/// <summary>
/// Key de ativação (inventário). O código é CIFRADO (Data Protection); CodeHash (SHA-256) impede cadastro duplicado;
/// CodeHint guarda só os 4 últimos caracteres para identificação no painel.
/// </summary>
public class GameKey
{
    public long Id { get; set; }
    public int JogoId { get; set; }
    public Jogo? Jogo { get; set; }

    public string CodeEncrypted { get; set; } = string.Empty;
    [MaxLength(64)] public string CodeHash { get; set; } = string.Empty;
    [MaxLength(8)] public string CodeHint { get; set; } = string.Empty;

    public GameKeyStatus Status { get; set; } = GameKeyStatus.Available;
    /// <summary>Pedido ao qual a key está reservada/vendida.</summary>
    public Guid? OrderId { get; set; }

    [MaxLength(300)] public string? Note { get; set; }
    [MaxLength(150)] public string? CreatedByName { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReservedAtUtc { get; set; }
    public DateTime? SoldAtUtc { get; set; }
}
