using EcommerceBase.Application.Common;
using EcommerceBase.Application.Dtos;
using EcommerceBase.Application.Inputs;
using EcommerceBase.Domain.Entities;
using EcommerceBase.Domain.Enums;

namespace EcommerceBase.Application.Abstractions;

// ===================== LOJA / CONFIGURAÇÕES =====================

/// <summary>Leitura (com cache) dos dados da loja. Fonte única: tabela StoreProfile.</summary>
public interface IStoreProfileProvider
{
    Task<StoreProfile> GetAsync(CancellationToken ct = default);
    void Invalidate();
}

public sealed record ImageUploadResult(bool Succeeded, string? Key, string? Error);

public interface IImageService
{
    /// <summary>Valida tamanho, extensão e assinatura real (magic bytes) e salva. Tipos: JPEG, PNG, WebP, GIF e ICO (favicon).</summary>
    Task<ImageUploadResult> SaveImageAsync(UploadedFile file, ImageKind kind, CancellationToken ct = default);
}

public interface IStoreSettingsService
{
    Task<CompanySettingsInput> GetCompanyAsync(CancellationToken ct = default);
    Task<Result> SaveCompanyAsync(CompanySettingsInput input, CancellationToken ct = default);

    Task<BrandingSettingsInput> GetBrandingAsync(CancellationToken ct = default);
    Task<Result> SaveBrandingAsync(BrandingSettingsInput input, UploadedFile? logo, UploadedFile? favicon, CancellationToken ct = default);

    Task<GeneralSettingsInput> GetGeneralAsync(CancellationToken ct = default);
    Task<Result> SaveGeneralAsync(GeneralSettingsInput input, CancellationToken ct = default);
}

public sealed record IntegrationView(
    IntegrationKind Kind,
    string ProviderKey,
    bool IsEnabled,
    bool IsSandbox,
    IReadOnlyDictionary<string, string> Values,
    /// <summary>Segredos já salvos: nome -> versão mascarada (ex.: "••••1a2b"). Nunca o valor completo.</summary>
    IReadOnlyDictionary<string, string> MaskedSecrets,
    DateTime? LastTestAtUtc,
    bool? LastTestSucceeded,
    string? LastTestMessage);

public interface IIntegrationSettingsService
{
    Task<IntegrationView> GetViewAsync(IntegrationKind kind, CancellationToken ct = default);
    Task<Result> SaveAsync(IntegrationKind kind, IntegrationSaveRequest request, CancellationToken ct = default);
    Task RecordTestResultAsync(IntegrationKind kind, bool succeeded, string message, CancellationToken ct = default);
}

/// <summary>Acesso interno (somente servidor) às configurações e segredos já decifrados.</summary>
public interface IIntegrationConfigReader
{
    Task<IntegrationConfig> GetAsync(IntegrationKind kind, CancellationToken ct = default);
}

public sealed class IntegrationConfig
{
    public IntegrationKind Kind { get; init; }
    public string ProviderKey { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public bool IsSandbox { get; init; } = true;
    public IReadOnlyDictionary<string, string> Values { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> Secrets { get; init; } = new Dictionary<string, string>();

    public string Value(string key, string fallback = "") =>
        Values.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : fallback;

    public string Secret(string key) =>
        Secrets.TryGetValue(key, out var v) ? v : string.Empty;
}

public interface IAuditService
{
    Task LogAsync(string action, string? entityType = null, string? entityId = null, string? details = null, CancellationToken ct = default);
    Task<PagedResult<AuditLogDto>> ListAsync(int page, int pageSize, CancellationToken ct = default);
}

public interface IAdminNotificationService
{
    Task NotifyAsync(string title, string message, string? link = null, CancellationToken ct = default);
    Task<List<NotificationDto>> ListRecentAsync(int count, CancellationToken ct = default);
    Task MarkAllReadAsync(CancellationToken ct = default);
}

// ===================== CATÁLOGO =====================

public interface ICatalogService
{
    Task<HomeDto> GetHomeAsync(CancellationToken ct = default);
    Task<PagedResult<ProductCardDto>> SearchAsync(ProductListQuery query, CancellationToken ct = default);
    Task<ProductDetailDto?> GetProductAsync(string slug, CancellationToken ct = default);
    Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
    Task<CategoryDto?> GetCategoryAsync(string slug, CancellationToken ct = default);
    Task<ContentPageDto?> GetPageAsync(string slug, CancellationToken ct = default);
    Task<List<ContentPageDto>> GetFooterPagesAsync(CancellationToken ct = default);
}

public interface IAdminCatalogService
{
    Task<PagedResult<AdminProductListItem>> ListProductsAsync(AdminProductQuery query, CancellationToken ct = default);
    Task<ProductEditDto?> GetProductAsync(int id, CancellationToken ct = default);
    Task<Result<int>> SaveProductAsync(ProductInput input, CancellationToken ct = default);
    Task<Result> SetProductActiveAsync(int id, bool active, CancellationToken ct = default);
    Task<Result> AddProductImagesAsync(int productId, IReadOnlyList<UploadedFile> files, CancellationToken ct = default);
    Task<Result> SetPrimaryImageAsync(int productId, int imageId, CancellationToken ct = default);
    Task<Result> RemoveProductImageAsync(int productId, int imageId, CancellationToken ct = default);

    Task<List<CategoryAdminItem>> ListCategoriesAsync(CancellationToken ct = default);
    Task<CategoryInput?> GetCategoryAsync(int id, CancellationToken ct = default);
    Task<Result<int>> SaveCategoryAsync(CategoryInput input, CancellationToken ct = default);
    Task<Result> DeleteCategoryAsync(int id, CancellationToken ct = default);

    Task<List<BrandAdminItem>> ListBrandsAsync(CancellationToken ct = default);
    Task<BrandInput?> GetBrandAsync(int id, CancellationToken ct = default);
    Task<Result<int>> SaveBrandAsync(BrandInput input, CancellationToken ct = default);
    Task<Result> DeleteBrandAsync(int id, CancellationToken ct = default);

    Task<List<SelectOption>> CategoryOptionsAsync(CancellationToken ct = default);
    Task<List<SelectOption>> BrandOptionsAsync(CancellationToken ct = default);
}

public interface IAdminContentService
{
    Task<List<BannerAdminItem>> ListBannersAsync(CancellationToken ct = default);
    Task<BannerInput?> GetBannerAsync(int id, CancellationToken ct = default);
    Task<Result<int>> SaveBannerAsync(BannerInput input, UploadedFile? image, CancellationToken ct = default);
    Task<Result> DeleteBannerAsync(int id, CancellationToken ct = default);

    Task<List<ContentPageAdminItem>> ListPagesAsync(CancellationToken ct = default);
    Task<ContentPageInput?> GetPageAsync(int id, CancellationToken ct = default);
    Task<Result<int>> SavePageAsync(ContentPageInput input, CancellationToken ct = default);
    Task<Result> DeletePageAsync(int id, CancellationToken ct = default);
}

// ===================== ESTOQUE =====================

public interface IStockService
{
    Task<PagedResult<StockOverviewItem>> OverviewAsync(string? search, bool onlyLow, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<StockMovementDto>> MovementsAsync(int? productId, int page, int pageSize, CancellationToken ct = default);
    /// <summary>Entrada, saída ou ajuste administrativo. Nunca deixa o estoque negativo.</summary>
    Task<Result> AdjustAsync(StockAdjustmentInput input, Guid? userId, CancellationToken ct = default);
}

// ===================== CARRINHO / CUPOM / FRETE / CHECKOUT =====================

public interface ICartService
{
    Task<CartDto> GetAsync(CartOwner owner, CancellationToken ct = default);
    Task<int> CountAsync(CartOwner owner, CancellationToken ct = default);
    Task<Result> AddAsync(CartOwner owner, int productId, int quantity, CancellationToken ct = default);
    Task<Result> SetQuantityAsync(CartOwner owner, int productId, int quantity, CancellationToken ct = default);
    Task<Result> RemoveAsync(CartOwner owner, int productId, CancellationToken ct = default);
    Task<Result> ApplyCouponAsync(CartOwner owner, string code, CancellationToken ct = default);
    Task<Result> RemoveCouponAsync(CartOwner owner, CancellationToken ct = default);
    /// <summary>Após o login, une o carrinho do visitante ao do usuário.</summary>
    Task MergeAnonymousAsync(string anonymousToken, Guid userId, CancellationToken ct = default);
}

public interface ICouponService
{
    Task<CouponEvaluationResult> EvaluateAsync(string code, IReadOnlyCollection<Domain.Rules.PricedLine> lines, Guid? userId, CancellationToken ct = default);

    Task<PagedResult<CouponListItem>> ListAsync(int page, int pageSize, CancellationToken ct = default);
    Task<CouponInput?> GetAsync(int id, CancellationToken ct = default);
    Task<Result<int>> SaveAsync(CouponInput input, CancellationToken ct = default);
    Task<Result> SetActiveAsync(int id, bool active, CancellationToken ct = default);
}

public interface IShippingService
{
    /// <summary>Cota o frete do carrinho do usuário para o CEP informado.</summary>
    Task<ShippingQuoteResultDto> QuoteAsync(CartOwner owner, string destinationPostalCode, CancellationToken ct = default);
    /// <summary>Revalida a opção escolhida (por código) recotando no servidor. Retorna null se não existir mais.</summary>
    Task<ShippingOptionDto?> ResolveOptionAsync(CartOwner owner, string destinationPostalCode, string optionCode, CancellationToken ct = default);
}

public interface ICheckoutService
{
    Task<CheckoutSummaryDto> GetSummaryAsync(Guid userId, CancellationToken ct = default);
    Task<CheckoutResult> PlaceOrderAsync(Guid userId, CheckoutRequest request, CancellationToken ct = default);
}

// ===================== PEDIDOS =====================

public interface IOrderService
{
    Task<PagedResult<OrderSummaryDto>> ListForCustomerAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<OrderDetailDto?> GetForCustomerAsync(Guid userId, string orderNumber, CancellationToken ct = default);
    Task<Result> CancelByCustomerAsync(Guid userId, string orderNumber, CancellationToken ct = default);
    Task<Result> RequestCancellationAsync(Guid userId, string orderNumber, string reason, CancellationToken ct = default);

    Task<PagedResult<OrderSummaryDto>> SearchAsync(AdminOrderQuery query, CancellationToken ct = default);
    Task<OrderDetailDto?> GetForAdminAsync(int orderId, CancellationToken ct = default);
    Task<Result> ChangeStatusAsync(int orderId, OrderStatus newStatus, string? note, Guid? adminId, CancellationToken ct = default);
    Task<Result> SetTrackingAsync(int orderId, string trackingCode, Guid? adminId, CancellationToken ct = default);
    Task<Result> SetAdminNotesAsync(int orderId, string? notes, Guid? adminId, CancellationToken ct = default);

    /// <summary>Cancela pedidos aguardando pagamento vencidos, devolvendo o estoque reservado. Retorna a quantidade cancelada.</summary>
    Task<int> ExpireStaleOrdersAsync(CancellationToken ct = default);
}

/// <summary>Operações de estoque ligadas ao ciclo de vida do pedido (uso interno entre serviços).</summary>
public interface IOrderLifecycle
{
    /// <summary>Cancela o pedido e devolve a reserva de estoque. Idempotente para pedidos já cancelados.</summary>
    Task<Result> CancelAndReleaseStockAsync(int orderId, string reason, Guid? userId, CancellationToken ct = default);
}

public interface IPaymentService
{
    /// <summary>Cria (ou reutiliza) o checkout no provedor para o pedido do cliente.</summary>
    Task<Result<string>> StartPaymentAsync(int orderId, CancellationToken ct = default);
    Task<Result<string>> StartPaymentForCustomerAsync(Guid userId, string orderNumber, CancellationToken ct = default);

    /// <summary>Processa a notificação: valida autenticidade, consulta o provedor e reconcilia o pedido.</summary>
    Task<WebhookOutcome> ProcessWebhookAsync(WebhookHeaders headers, string? topic, CancellationToken ct = default);

    /// <summary>Consulta o provedor e reconcilia o estado de um pedido (usado no retorno do cliente e na rotina de conciliação).</summary>
    Task<Result> SyncOrderPaymentAsync(int orderId, CancellationToken ct = default);
    Task<int> ReconcilePendingAsync(CancellationToken ct = default);

    Task<Result> RefundAsync(int paymentId, decimal? amount, Guid? adminId, CancellationToken ct = default);
}

public enum WebhookOutcome
{
    Processed,
    Ignored,
    InvalidSignature,
    TemporaryError
}

public interface IPaymentMethodCatalog
{
    Task<List<PaymentMethodDto>> GetEnabledAsync(CancellationToken ct = default);
}

// ===================== CONTA / CLIENTES =====================

public interface IAccountService
{
    Task<ProfileInput?> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<Result> UpdateProfileAsync(Guid userId, ProfileInput input, CancellationToken ct = default);

    Task<List<AddressDto>> ListAddressesAsync(Guid userId, CancellationToken ct = default);
    Task<AddressInput?> GetAddressAsync(Guid userId, int addressId, CancellationToken ct = default);
    Task<Result<int>> SaveAddressAsync(Guid userId, AddressInput input, CancellationToken ct = default);
    Task<Result> DeleteAddressAsync(Guid userId, int addressId, CancellationToken ct = default);
    Task<Result> SetDefaultAddressAsync(Guid userId, int addressId, CancellationToken ct = default);
}

public interface IAdminCustomerService
{
    Task<PagedResult<CustomerListItem>> ListAsync(string? search, int page, int pageSize, CancellationToken ct = default);
    Task<CustomerDetailDto?> GetAsync(Guid id, CancellationToken ct = default);
    /// <summary>Bloqueia/desbloqueia o acesso. Não permite afetar administradores nem a si mesmo.</summary>
    Task<Result> SetActiveAsync(Guid id, bool active, Guid performedBy, CancellationToken ct = default);
}

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(int periodDays, CancellationToken ct = default);
}

// ===================== E-MAIL / FISCAL =====================

public static class EmailTemplateKeys
{
    public const string ConfirmEmail = "confirm-email";
    public const string ResetPassword = "reset-password";
    public const string OrderConfirmation = "order-confirmation";
    public const string PaymentUpdate = "payment-update";
    public const string ShippingUpdate = "shipping-update";
    public const string AdminAlert = "admin-alert";
    public const string Test = "test";

    public static readonly string[] All =
    {
        ConfirmEmail, ResetPassword, OrderConfirmation, PaymentUpdate, ShippingUpdate, AdminAlert, Test
    };
}

public interface IEmailService
{
    /// <summary>Renderiza o modelo e coloca na fila (outbox). O envio é feito em segundo plano.</summary>
    Task QueueAsync(string templateKey, string toEmail, IReadOnlyDictionary<string, string> variables, CancellationToken ct = default);
    /// <summary>Envio imediato, sem fila (usado no e-mail de teste do painel). Reflete o erro real do provedor.</summary>
    Task<Result> SendTestAsync(string toEmail, CancellationToken ct = default);
    /// <summary>Processa a fila. Retorna a quantidade enviada com sucesso.</summary>
    Task<int> ProcessQueueAsync(int batchSize, CancellationToken ct = default);

    Task<List<EmailTemplate>> ListTemplatesAsync(CancellationToken ct = default);
    Task<Result> SaveTemplateAsync(string key, string subject, string bodyHtml, CancellationToken ct = default);
}

public interface IFiscalService
{
    Task<FiscalDocument?> GetForOrderAsync(int orderId, CancellationToken ct = default);
    Task<Result<int>> IssueAsync(int orderId, CancellationToken ct = default);
    Task<Result> RefreshAsync(int fiscalDocumentId, CancellationToken ct = default);
    Task<Result> CancelAsync(int fiscalDocumentId, string justification, CancellationToken ct = default);
    Task<List<FiscalEvent>> EventsAsync(int fiscalDocumentId, CancellationToken ct = default);
}

/// <summary>Atalhos para escolher o provedor ativo conforme a configuração do painel.</summary>
public interface IProviderResolver
{
    Task<IPaymentProvider?> GetPaymentProviderAsync(CancellationToken ct = default);
    Task<IReadOnlyList<IShippingProvider>> GetActiveShippingProvidersAsync(CancellationToken ct = default);
    Task<IEmailProvider?> GetEmailProviderAsync(CancellationToken ct = default);
    Task<IFiscalProvider?> GetFiscalProviderAsync(CancellationToken ct = default);
}
