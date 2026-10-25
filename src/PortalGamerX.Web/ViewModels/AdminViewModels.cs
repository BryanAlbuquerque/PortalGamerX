using System.ComponentModel.DataAnnotations;
using PortalGamerX.Domain;
using PortalGamerX.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PortalGamerX.ViewModels;

// ---------- Jogos ----------
public class JogoForm : IValidatableObject
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Informe o nome."), StringLength(200)]
    [Display(Name = "Nome")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Descrição resumida")]
    public string? ShortDescription { get; set; }

    [Display(Name = "Descrição completa")]
    public string? Description { get; set; }

    [Range(0.01, 99999999, ErrorMessage = "Informe um preço maior que zero.")]
    [Display(Name = "Preço (R$)")]
    public decimal Price { get; set; }

    [Display(Name = "Preço promocional (R$)")]
    public decimal? PromotionalPrice { get; set; }

    [Required(ErrorMessage = "Informe o SKU."), StringLength(60)]
    [Display(Name = "SKU")]
    public string Sku { get; set; } = string.Empty;

    [StringLength(60)]
    [Display(Name = "Código de barras (GTIN/EAN)")]
    public string? Barcode { get; set; }

    [Display(Name = "Plataforma")]
    public GamePlatform Platform { get; set; } = GamePlatform.Outro;

    [Display(Name = "Tipo de mídia")]
    public GameMediaType MediaType { get; set; } = GameMediaType.Disc;

    [Display(Name = "Classificação indicativa")]
    public AgeRating AgeRating { get; set; } = AgeRating.Livre;

    [Display(Name = "Data de lançamento")]
    public DateOnly? ReleaseDate { get; set; }

    [StringLength(8, MinimumLength = 8, ErrorMessage = "O NCM possui 8 dígitos.")]
    [Display(Name = "NCM (fiscal)")]
    public string? Ncm { get; set; }

    [StringLength(7, MinimumLength = 7, ErrorMessage = "O CEST possui 7 dígitos.")]
    [Display(Name = "CEST (se houver ST)")]
    public string? Cest { get; set; }

    [Range(0, 8, ErrorMessage = "Origem inválida (0 a 8).")]
    [Display(Name = "Origem da mercadoria")]
    public int OriginCode { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecione a categoria.")]
    [Display(Name = "Categoria")]
    public int CategoryId { get; set; }

    [Display(Name = "Publicadora")]
    public int? BrandId { get; set; }

    [Range(0, 1000000, ErrorMessage = "Estoque inválido.")]
    [Display(Name = "Estoque inicial")]
    public int StockQuantity { get; set; }

    [Range(0, 100000)] [Display(Name = "Peso (kg)")] public decimal WeightKg { get; set; }
    [Range(0, 100000)] [Display(Name = "Altura (cm)")] public decimal HeightCm { get; set; }
    [Range(0, 100000)] [Display(Name = "Largura (cm)")] public decimal WidthCm { get; set; }
    [Range(0, 100000)] [Display(Name = "Comprimento (cm)")] public decimal LengthCm { get; set; }

    [Display(Name = "Ativo")] public bool IsActive { get; set; } = true;
    [Display(Name = "Destaque na home")] public bool IsFeatured { get; set; }
    [Display(Name = "Disponível para venda")] public bool IsAvailable { get; set; } = true;

    public List<SelectListItem> Categories { get; set; } = new();
    public List<SelectListItem> Brands { get; set; } = new();
    public List<JogoImage> Images { get; set; } = new();
    public int CurrentStock { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PromotionalPrice.HasValue && (PromotionalPrice.Value <= 0 || PromotionalPrice.Value >= Price))
            yield return new ValidationResult("O preço promocional deve ser maior que zero e menor que o preço normal.", new[] { nameof(PromotionalPrice) });
        if (!string.IsNullOrEmpty(Ncm) && !Ncm.All(char.IsDigit))
            yield return new ValidationResult("O NCM deve conter apenas dígitos.", new[] { nameof(Ncm) });
        if (!string.IsNullOrEmpty(Cest) && !Cest.All(char.IsDigit))
            yield return new ValidationResult("O CEST deve conter apenas dígitos.", new[] { nameof(Cest) });
        if (!Enum.IsDefined(Platform))
            yield return new ValidationResult("Plataforma inválida.", new[] { nameof(Platform) });
        if (!Enum.IsDefined(MediaType))
            yield return new ValidationResult("Tipo de mídia inválido.", new[] { nameof(MediaType) });
        if (!Enum.IsDefined(AgeRating))
            yield return new ValidationResult("Classificação indicativa inválida.", new[] { nameof(AgeRating) });
    }
}

public class AdminJogoListVm
{
    public List<Jogo> Items { get; set; } = new();
    public string? Q { get; set; }
    public string? Filter { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int Total { get; set; }
}

// ---------- Categorias / Publicadoras ----------
public class CategoryForm
{
    public int? Id { get; set; }
    [Required(ErrorMessage = "Informe o nome."), StringLength(100)] [Display(Name = "Nome")] public string Name { get; set; } = string.Empty;
    [StringLength(500)] [Display(Name = "Descrição")] public string? Description { get; set; }
    [Display(Name = "Categoria pai")] public int? ParentId { get; set; }
    [Display(Name = "Ordem")] public int SortOrder { get; set; }
    [Display(Name = "Ativa")] public bool IsActive { get; set; } = true;
    public List<SelectListItem> Parents { get; set; } = new();
}

public class BrandForm
{
    public int? Id { get; set; }
    [Required(ErrorMessage = "Informe o nome."), StringLength(100)] [Display(Name = "Nome")] public string Name { get; set; } = string.Empty;
    [Display(Name = "Ativa")] public bool IsActive { get; set; } = true;
}

// ---------- Pedidos ----------
public class AdminOrderListVm
{
    public List<Order> Items { get; set; } = new();
    public string? Q { get; set; }
    public OrderStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int Total { get; set; }
}

public class AdminOrderVm
{
    public Order Order { get; set; } = null!;
    public List<FiscalDocument> FiscalDocuments { get; set; } = new();
    public bool FiscalConfigured { get; set; }
    public List<OrderStatus> AllowedTargets { get; set; } = new();
    public bool CanRefund { get; set; }
}

public class ChangeStatusInput
{
    public Guid Id { get; set; }
    [Required] public OrderStatus Target { get; set; }
    [StringLength(60)] public string? TrackingCode { get; set; }
    [StringLength(500)] public string? Note { get; set; }
}

// ---------- Clientes ----------
public class CustomerListVm
{
    public List<ApplicationUser> Items { get; set; } = new();
    public string? Q { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}

public class CustomerDetailsVm
{
    public ApplicationUser User { get; set; } = null!;
    public List<Order> Orders { get; set; } = new();
    public List<Address> Addresses { get; set; } = new();
    public bool IsAdmin { get; set; }
}

/// <summary>Edição administrativa limitada: e-mail, papéis e senha NÃO são editáveis aqui.</summary>
public class CustomerEditForm
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Informe o nome."), StringLength(150)] [Display(Name = "Nome")] public string FullName { get; set; } = string.Empty;
    [StringLength(20)] [Display(Name = "Telefone")] public string? Phone { get; set; }
    [StringLength(20)] [Display(Name = "CPF/CNPJ")] public string? Document { get; set; }
}

// ---------- Estoque ----------
public class StockListVm
{
    public List<Jogo> Items { get; set; } = new();
    public string? Q { get; set; }
    public bool LowOnly { get; set; }
    public int Threshold { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}

public class StockMoveForm
{
    public int JogoId { get; set; }
    public string JogoName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }

    [Required(ErrorMessage = "Selecione o tipo.")]
    [Display(Name = "Tipo")]
    public StockMovementType Type { get; set; } = StockMovementType.Entry;

    [Range(0, 1000000, ErrorMessage = "Quantidade inválida.")]
    [Display(Name = "Quantidade (para Ajuste: novo saldo)")]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "Informe o motivo."), StringLength(300)]
    [Display(Name = "Motivo")]
    public string Reason { get; set; } = string.Empty;
}

public class StockHistoryVm
{
    public Jogo Jogo { get; set; } = null!;
    public List<StockMovement> Movements { get; set; } = new();
}

// ---------- Cupons ----------
public class CouponForm : IValidatableObject
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Informe o código."), StringLength(50)]
    [RegularExpression(@"^[A-Za-z0-9_\-]+$", ErrorMessage = "Use apenas letras, números, hífen e sublinhado.")]
    [Display(Name = "Código")]
    public string Code { get; set; } = string.Empty;

    [StringLength(200)] [Display(Name = "Descrição")] public string? Description { get; set; }
    [Display(Name = "Tipo de desconto")] public DiscountType DiscountType { get; set; } = DiscountType.Percentage;
    [Range(0.01, 99999999, ErrorMessage = "Informe um valor maior que zero.")] [Display(Name = "Valor (% ou R$)")] public decimal Value { get; set; }
    [Display(Name = "Desconto máximo (R$, p/ percentual)")] public decimal? MaxDiscountAmount { get; set; }
    [Display(Name = "Início (horário da loja)")] public DateTime? StartsAtLocal { get; set; }
    [Display(Name = "Fim (horário da loja)")] public DateTime? EndsAtLocal { get; set; }
    [Display(Name = "Valor mínimo do pedido (R$)")] public decimal? MinOrderAmount { get; set; }
    [Range(1, 100000000)] [Display(Name = "Limite total de usos")] public int? MaxUses { get; set; }
    [Range(1, 100000)] [Display(Name = "Limite por cliente")] public int? MaxUsesPerCustomer { get; set; }
    [Display(Name = "Ativo")] public bool IsActive { get; set; } = true;
    [Display(Name = "Categorias elegíveis (vazio = todas)")] public List<int> CategoryIds { get; set; } = new();
    [Display(Name = "SKUs elegíveis, separados por vírgula (vazio = todos)")] public string? Skus { get; set; }
    public List<SelectListItem> Categories { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DiscountType == DiscountType.Percentage && Value > 100)
            yield return new ValidationResult("O percentual não pode passar de 100.", new[] { nameof(Value) });
        if (StartsAtLocal.HasValue && EndsAtLocal.HasValue && EndsAtLocal < StartsAtLocal)
            yield return new ValidationResult("A data final deve ser posterior à inicial.", new[] { nameof(EndsAtLocal) });
    }
}

// ---------- Banners ----------
public class BannerForm
{
    public int? Id { get; set; }
    [Required(ErrorMessage = "Informe o título."), StringLength(150)] [Display(Name = "Título")] public string Title { get; set; } = string.Empty;
    [StringLength(250)] [Display(Name = "Subtítulo")] public string? Subtitle { get; set; }
    [StringLength(300)] [Display(Name = "Link (caminho interno /... ou https://...)")] public string? LinkUrl { get; set; }
    [StringLength(50)] [Display(Name = "Texto do botão")] public string? ButtonText { get; set; }
    [Display(Name = "Ordem")] public int SortOrder { get; set; }
    [Display(Name = "Ativo")] public bool IsActive { get; set; } = true;
    public string? CurrentImage { get; set; }
}

// ---------- Auditoria ----------
public class AuditListVm
{
    public List<AuditLog> Items { get; set; } = new();
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}
