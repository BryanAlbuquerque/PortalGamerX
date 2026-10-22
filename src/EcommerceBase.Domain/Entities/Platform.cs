using EcommerceBase.Domain.Enums;

namespace EcommerceBase.Domain.Entities;

/// <summary>
/// Linha única (Id = 1) com os dados da empresa, identidade visual e parâmetros gerais.
/// É a fonte única desses dados: os módulos fiscal, e-mail e frete leem daqui.
/// </summary>
public class StoreProfile
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    // Geral
    public string StoreName { get; set; } = "Minha Loja";
    public string Currency { get; set; } = "BRL";
    public string TimeZoneId { get; set; } = "America/Sao_Paulo";
    public int OrderExpirationMinutes { get; set; } = 1440;
    public int DefaultLowStockThreshold { get; set; } = 5;

    // Empresa
    public string? LegalName { get; set; }
    public string? TradeName { get; set; }
    public string? Cnpj { get; set; }
    public string? StateRegistration { get; set; }
    public string? MunicipalRegistration { get; set; }
    public string? ContactEmail { get; set; }
    public string? Phone { get; set; }
    public string? PostalCode { get; set; }
    public string? Street { get; set; }
    public string? Number { get; set; }
    public string? Complement { get; set; }
    public string? District { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string Country { get; set; } = "Brasil";
    public string? Website { get; set; }
    public string? SupportInfo { get; set; }
    /// <summary>Regime tributário (1 = Simples Nacional, 2 = Simples excesso, 3 = Regime Normal).</summary>
    public int? TaxRegimeCode { get; set; }

    // Identidade visual
    public string? LogoKey { get; set; }
    public string? FaviconKey { get; set; }
    public string PrimaryColor { get; set; } = "#0d6efd";
    public string SecondaryColor { get; set; } = "#212529";
    public string AccentColor { get; set; } = "#ffc107";
    public string? FooterText { get; set; }
    public string? AboutTitle { get; set; }
    public string? AboutText { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}

public class IntegrationSetting
{
    public int Id { get; set; }
    public IntegrationKind Kind { get; set; }
    public string ProviderKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsSandbox { get; set; } = true;
    /// <summary>Valores não secretos (JSON de dicionário).</summary>
    public string ValuesJson { get; set; } = "{}";
    /// <summary>Segredos protegidos com ASP.NET Core Data Protection (JSON de dicionário, criptografado).</summary>
    public string EncryptedSecrets { get; set; } = string.Empty;
    public DateTime? LastTestAtUtc { get; set; }
    public bool? LastTestSucceeded { get; set; }
    public string? LastTestMessage { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class Banner
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string ImageKey { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class ContentPage
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    /// <summary>Texto simples (parágrafos separados por linha em branco). Nunca é renderizado como HTML bruto.</summary>
    public string Body { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool ShowInFooter { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class EmailTemplate
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
}

public class EmailMessage
{
    public long Id { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    /// <summary>Esvaziado após o envio bem-sucedido (pode conter links com token).</summary>
    public string BodyHtml { get; set; } = string.Empty;
    public string TemplateKey { get; set; } = string.Empty;
    public EmailStatus Status { get; set; } = EmailStatus.Pending;
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime NextAttemptAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
}

public class AdminNotification
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Link { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>Armazenamento de arquivos no banco (opção para hospedagens sem disco persistente).</summary>
public class StoredFile
{
    public string Key { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public long Length { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class AuditLog
{
    public long Id { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class FiscalDocument
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public string Provider { get; set; } = string.Empty;
    /// <summary>Tipo do documento: "NFe" (modelo 55). NFC-e e NFS-e não são suportadas nesta versão.</summary>
    public string DocumentType { get; set; } = "NFe";
    /// <summary>Referência única enviada ao provedor (idempotência da emissão).</summary>
    public string Reference { get; set; } = string.Empty;
    public FiscalDocumentStatus Status { get; set; } = FiscalDocumentStatus.Pending;
    public bool IsHomologation { get; set; }
    public string? Number { get; set; }
    public string? Series { get; set; }
    public string? AccessKey { get; set; }
    public string? Protocol { get; set; }
    public string? XmlPath { get; set; }
    public string? DanfePath { get; set; }
    public string? ProviderStatus { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? AuthorizedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }

    public ICollection<FiscalEvent> Events { get; set; } = new List<FiscalEvent>();
}

public class FiscalEvent
{
    public long Id { get; set; }
    public int FiscalDocumentId { get; set; }
    public FiscalDocument? FiscalDocument { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
