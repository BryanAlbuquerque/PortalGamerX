using System.ComponentModel.DataAnnotations;
using EcommerceBase.Application.Validation;
using EcommerceBase.Domain.Enums;

namespace EcommerceBase.Application.Inputs;

// Modelos de entrada dos formulários. Contêm apenas campos que o usuário pode editar:
// nada de papel, status de pagamento, totais ou identificadores de dono (evita over-posting).

public class ProductInput
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome."), StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(220)]
    public string? Slug { get; set; }

    [StringLength(500)]
    public string? ShortDescription { get; set; }

    [StringLength(10000)]
    public string? Description { get; set; }

    [Range(0.01, 99999999, ErrorMessage = "Informe um preço maior que zero.")]
    public decimal Price { get; set; }

    [Range(0.01, 99999999, ErrorMessage = "Preço promocional inválido.")]
    public decimal? PromotionalPrice { get; set; }

    [Required(ErrorMessage = "Informe o SKU."), StringLength(60)]
    public string Sku { get; set; } = string.Empty;

    [StringLength(60)]
    public string? Barcode { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecione a categoria.")]
    public int CategoryId { get; set; }

    public int? BrandId { get; set; }

    /// <summary>Usado apenas na criação. Depois, o estoque só muda pela tela de Estoque (com motivo e histórico).</summary>
    [Range(0, 10000000)]
    public int InitialStock { get; set; }

    [Range(0, 100000)]
    public int LowStockThreshold { get; set; } = 5;

    [Range(0, 1000000, ErrorMessage = "Peso em gramas, entre 0 e 1.000.000.")]
    public int WeightGrams { get; set; }

    [Range(0, 1000)]
    public decimal HeightCm { get; set; }

    [Range(0, 1000)]
    public decimal WidthCm { get; set; }

    [Range(0, 1000)]
    public decimal LengthCm { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }

    [StringLength(8, MinimumLength = 8, ErrorMessage = "O NCM possui 8 dígitos.")]
    [RegularExpression("^[0-9]{8}$", ErrorMessage = "O NCM possui 8 dígitos numéricos.")]
    public string? Ncm { get; set; }

    [RegularExpression("^[0-9]{7}$", ErrorMessage = "O CEST possui 7 dígitos numéricos.")]
    public string? Cest { get; set; }

    [Required, StringLength(6)]
    public string CommercialUnit { get; set; } = "UN";

    [Range(0, 8)]
    public int OriginCode { get; set; }
}

public class CategoryInput
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome."), StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(140)]
    public string? Slug { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class BrandInput
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome."), StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(140)]
    public string? Slug { get; set; }

    public bool IsActive { get; set; } = true;
}

public class CouponInput : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o código."), StringLength(40, MinimumLength = 3)]
    [RegularExpression("^[A-Za-z0-9_-]+$", ErrorMessage = "Use apenas letras, números, hífen e sublinhado.")]
    public string Code { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    public DiscountType DiscountType { get; set; }

    [Range(0.01, 99999999, ErrorMessage = "Informe um valor maior que zero.")]
    public decimal Value { get; set; }

    /// <summary>Data/hora no fuso da loja.</summary>
    public DateTime? StartsAtLocal { get; set; }
    public DateTime? EndsAtLocal { get; set; }

    [Range(0.01, 99999999)]
    public decimal? MinimumOrderAmount { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxUses { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxUsesPerCustomer { get; set; }

    public bool IsActive { get; set; } = true;
    public int? CategoryId { get; set; }
    public int? ProductId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DiscountType == DiscountType.Percentage && (Value <= 0 || Value > 100))
            yield return new ValidationResult("O percentual deve ficar entre 0,01 e 100.", new[] { nameof(Value) });
        if (StartsAtLocal.HasValue && EndsAtLocal.HasValue && EndsAtLocal < StartsAtLocal)
            yield return new ValidationResult("A data final deve ser posterior à inicial.", new[] { nameof(EndsAtLocal) });
    }
}

public class BannerInput
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o título."), StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Subtitle { get; set; }

    [SafeUrl, StringLength(500)]
    public string? LinkUrl { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? StartsAtLocal { get; set; }
    public DateTime? EndsAtLocal { get; set; }
}

public class ContentPageInput
{
    public int Id { get; set; }

    [StringLength(100)]
    public string? Slug { get; set; }

    [Required(ErrorMessage = "Informe o título."), StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o conteúdo."), StringLength(50000)]
    public string Body { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public bool ShowInFooter { get; set; } = true;
    public int SortOrder { get; set; }
}

public class StockAdjustmentInput
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecione o produto.")]
    public int ProductId { get; set; }

    /// <summary>Entry (entrada), Exit (saída) ou Adjustment (define a quantidade final).</summary>
    public StockMovementType Type { get; set; } = StockMovementType.Entry;

    [Range(0, 10000000, ErrorMessage = "Quantidade inválida.")]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "Informe o motivo da movimentação."), StringLength(300, MinimumLength = 3)]
    public string Reason { get; set; } = string.Empty;
}

public class AddressInput
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe um nome para o endereço."), StringLength(40)]
    public string Label { get; set; } = "Casa";

    [Required(ErrorMessage = "Informe o destinatário."), StringLength(150)]
    public string RecipientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o CEP."), Cep]
    public string PostalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a rua."), StringLength(200)]
    public string Street { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o número."), StringLength(20)]
    public string Number { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Complement { get; set; }

    [Required(ErrorMessage = "Informe o bairro."), StringLength(100)]
    public string District { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a cidade."), StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a UF."), StateCode]
    public string State { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}

public class ProfileInput
{
    [Required(ErrorMessage = "Informe seu nome."), StringLength(150, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(30)]
    [RegularExpression(@"^[0-9()+\-\s]*$", ErrorMessage = "Telefone inválido.")]
    public string? PhoneNumber { get; set; }

    [Cpf]
    public string? Document { get; set; }
}

public class CompanySettingsInput
{
    [StringLength(200)] public string? LegalName { get; set; }
    [StringLength(200)] public string? TradeName { get; set; }
    [Cnpj] public string? Cnpj { get; set; }
    [StringLength(30)] public string? StateRegistration { get; set; }
    [StringLength(30)] public string? MunicipalRegistration { get; set; }
    [EmailAddress, StringLength(200)] public string? ContactEmail { get; set; }
    [StringLength(30)] public string? Phone { get; set; }
    [Cep] public string? PostalCode { get; set; }
    [StringLength(200)] public string? Street { get; set; }
    [StringLength(20)] public string? Number { get; set; }
    [StringLength(100)] public string? Complement { get; set; }
    [StringLength(100)] public string? District { get; set; }
    [StringLength(100)] public string? City { get; set; }
    [StateCode] public string? State { get; set; }
    [StringLength(60)] public string Country { get; set; } = "Brasil";
    [SafeUrl, StringLength(300)] public string? Website { get; set; }
    [StringLength(1000)] public string? SupportInfo { get; set; }
    [Range(1, 3)] public int? TaxRegimeCode { get; set; }
}

public class BrandingSettingsInput
{
    [Required, HexColor] public string PrimaryColor { get; set; } = "#0d6efd";
    [Required, HexColor] public string SecondaryColor { get; set; } = "#212529";
    [Required, HexColor] public string AccentColor { get; set; } = "#ffc107";
    [StringLength(1000)] public string? FooterText { get; set; }
    [StringLength(150)] public string? AboutTitle { get; set; }
    [StringLength(3000)] public string? AboutText { get; set; }
    public bool RemoveLogo { get; set; }
    public bool RemoveFavicon { get; set; }
}

public class GeneralSettingsInput
{
    [Required(ErrorMessage = "Informe o nome da loja."), StringLength(120)]
    public string StoreName { get; set; } = string.Empty;

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "BRL";

    [Required, StringLength(80)]
    public string TimeZoneId { get; set; } = "America/Sao_Paulo";

    [Range(15, 20160, ErrorMessage = "Entre 15 minutos e 14 dias.")]
    public int OrderExpirationMinutes { get; set; } = 1440;

    [Range(0, 100000)]
    public int DefaultLowStockThreshold { get; set; } = 5;
}

public class IntegrationSaveRequest
{
    public string ProviderKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsSandbox { get; set; } = true;
    /// <summary>Valores não secretos. Chaves ausentes são removidas.</summary>
    public Dictionary<string, string> Values { get; set; } = new();
    /// <summary>Segredos informados agora. Valor vazio/ausente = manter o segredo já salvo.</summary>
    public Dictionary<string, string> Secrets { get; set; } = new();
    /// <summary>Segredos a apagar explicitamente.</summary>
    public List<string> ClearSecrets { get; set; } = new();
}
