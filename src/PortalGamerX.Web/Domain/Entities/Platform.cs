using System.ComponentModel.DataAnnotations;

namespace PortalGamerX.Domain.Entities;

/// <summary>Linha única (Id = 1) com dados da empresa, identidade visual e parâmetros gerais.</summary>
public class StoreSettings
{
    public int Id { get; set; } = 1;

    // Geral
    [MaxLength(120)] public string StoreName { get; set; } = "PortalGamerX";
    [MaxLength(10)] public string Currency { get; set; } = "BRL";
    [MaxLength(60)] public string TimeZoneId { get; set; } = "America/Sao_Paulo";
    public int LowStockThreshold { get; set; } = 5;
    public int PendingOrderExpirationMinutes { get; set; } = 120;
    public int MaxItemQuantityPerJogo { get; set; } = 20;

    // Empresa
    [MaxLength(200)] public string? LegalName { get; set; }
    [MaxLength(200)] public string? TradeName { get; set; }
    [MaxLength(14)] public string? Cnpj { get; set; }
    [MaxLength(30)] public string? StateRegistration { get; set; }
    [MaxLength(30)] public string? MunicipalRegistration { get; set; }
    [MaxLength(256)] public string? Email { get; set; }
    [MaxLength(30)] public string? Phone { get; set; }
    [MaxLength(8)] public string? PostalCode { get; set; }
    [MaxLength(200)] public string? Street { get; set; }
    [MaxLength(20)] public string? Number { get; set; }
    [MaxLength(100)] public string? Complement { get; set; }
    [MaxLength(100)] public string? District { get; set; }
    [MaxLength(100)] public string? City { get; set; }
    [MaxLength(2)] public string? State { get; set; }
    [MaxLength(60)] public string? Country { get; set; } = "Brasil";
    [MaxLength(200)] public string? Website { get; set; }
    [MaxLength(1000)] public string? SupportInfo { get; set; }

    // Identidade visual
    [MaxLength(300)] public string? LogoPath { get; set; }
    [MaxLength(300)] public string? FaviconPath { get; set; }
    [MaxLength(7)] public string PrimaryColor { get; set; } = "#0d6efd";
    [MaxLength(7)] public string SecondaryColor { get; set; } = "#212529";
    [MaxLength(7)] public string AccentColor { get; set; } = "#ff6b35";
    [MaxLength(1000)] public string? FooterText { get; set; }

    // Textos institucionais (texto simples; renderizado com codificação HTML)
    public string? AboutText { get; set; }
    public string? TermsText { get; set; }
    public string? PrivacyText { get; set; }
    public string? ShippingPolicyText { get; set; }
    public string? ReturnPolicyText { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Configuração chave/valor. Valores secretos são cifrados com Data Protection.</summary>
public class IntegrationSetting
{
    public int Id { get; set; }
    [MaxLength(120)] public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public bool IsSecret { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(150)] public string? UpdatedBy { get; set; }
}

public class EmailOutbox
{
    public long Id { get; set; }
    [MaxLength(256)] public string ToAddress { get; set; } = string.Empty;
    [MaxLength(150)] public string? ToName { get; set; }
    [MaxLength(300)] public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    [MaxLength(40)] public string Kind { get; set; } = string.Empty;
    /// <summary>Corpo contém tokens/links sensíveis: é apagado após o envio.</summary>
    public bool IsSensitive { get; set; }
    public EmailStatus Status { get; set; } = EmailStatus.Pending;
    public int Attempts { get; set; }
    [MaxLength(500)] public string? LastError { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime NextAttemptUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SentAtUtc { get; set; }
}

public class FiscalDocument
{
    public int Id { get; set; }
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    [MaxLength(10)] public string DocumentType { get; set; } = "NFe";
    public FiscalStatus Status { get; set; } = FiscalStatus.NotRequested;
    [MaxLength(60)] public string? Provider { get; set; }
    [MaxLength(100)] public string? ExternalReference { get; set; }
    [MaxLength(20)] public string? Number { get; set; }
    [MaxLength(10)] public string? Series { get; set; }
    [MaxLength(60)] public string? AccessKey { get; set; }
    [MaxLength(60)] public string? Protocol { get; set; }
    [MaxLength(500)] public string? XmlUrl { get; set; }
    [MaxLength(500)] public string? PdfUrl { get; set; }
    [MaxLength(500)] public string? RejectionMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<FiscalEvent> Events { get; set; } = new();
}

public class FiscalEvent
{
    public int Id { get; set; }
    public int FiscalDocumentId { get; set; }
    public FiscalDocument? FiscalDocument { get; set; }
    [MaxLength(40)] public string EventType { get; set; } = string.Empty;
    [MaxLength(500)] public string? Message { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class AuditLog
{
    public long Id { get; set; }
    public Guid? UserId { get; set; }
    [MaxLength(150)] public string? UserName { get; set; }
    [MaxLength(80)] public string Action { get; set; } = string.Empty;
    [MaxLength(60)] public string? EntityType { get; set; }
    [MaxLength(60)] public string? EntityId { get; set; }
    [MaxLength(1000)] public string? Details { get; set; }
    [MaxLength(60)] public string? IpAddress { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
