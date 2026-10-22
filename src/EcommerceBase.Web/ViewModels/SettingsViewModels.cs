using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EcommerceBase.Web.ViewModels;

public class CompanySettingsForm
{
    [StringLength(200)] [Display(Name = "Razão social")] public string? LegalName { get; set; }
    [StringLength(200)] [Display(Name = "Nome fantasia")] public string? TradeName { get; set; }
    [StringLength(18)] [Display(Name = "CNPJ")] public string? Cnpj { get; set; }
    [StringLength(30)] [Display(Name = "Inscrição estadual")] public string? StateRegistration { get; set; }
    [StringLength(30)] [Display(Name = "Inscrição municipal")] public string? MunicipalRegistration { get; set; }
    [EmailAddress(ErrorMessage = "E-mail inválido.")] [StringLength(256)] [Display(Name = "E-mail comercial")] public string? Email { get; set; }
    [StringLength(30)] [Display(Name = "Telefone")] public string? Phone { get; set; }
    [StringLength(9)] [Display(Name = "CEP")] public string? PostalCode { get; set; }
    [StringLength(200)] [Display(Name = "Logradouro")] public string? Street { get; set; }
    [StringLength(20)] [Display(Name = "Número")] public string? Number { get; set; }
    [StringLength(100)] [Display(Name = "Complemento")] public string? Complement { get; set; }
    [StringLength(100)] [Display(Name = "Bairro")] public string? District { get; set; }
    [StringLength(100)] [Display(Name = "Cidade")] public string? City { get; set; }
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Use a sigla da UF.")] [Display(Name = "UF")] public string? State { get; set; }
    [StringLength(60)] [Display(Name = "País")] public string? Country { get; set; }
    [Url(ErrorMessage = "URL inválida.")] [StringLength(200)] [Display(Name = "Site")] public string? Website { get; set; }
    [StringLength(1000)] [Display(Name = "Contato e suporte")] public string? SupportInfo { get; set; }
}

public class VisualSettingsForm
{
    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Use o formato #RRGGBB.")] [Display(Name = "Cor primária")] public string PrimaryColor { get; set; } = "#0d6efd";
    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Use o formato #RRGGBB.")] [Display(Name = "Cor secundária (cabeçalho/rodapé)")] public string SecondaryColor { get; set; } = "#212529";
    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Use o formato #RRGGBB.")] [Display(Name = "Cor de destaque")] public string AccentColor { get; set; } = "#ff6b35";
    [StringLength(1000)] [Display(Name = "Texto do rodapé")] public string? FooterText { get; set; }
    [Display(Name = "Sobre nós")] public string? AboutText { get; set; }
    [Display(Name = "Termos de uso")] public string? TermsText { get; set; }
    [Display(Name = "Política de privacidade")] public string? PrivacyText { get; set; }
    [Display(Name = "Política de entrega")] public string? ShippingPolicyText { get; set; }
    [Display(Name = "Trocas e devoluções")] public string? ReturnPolicyText { get; set; }

    public IFormFile? LogoFile { get; set; }
    public IFormFile? FaviconFile { get; set; }
    public bool RemoveLogo { get; set; }
    public bool RemoveFavicon { get; set; }
    public string? LogoPath { get; set; }
    public string? FaviconPath { get; set; }
}

public class GeneralSettingsForm
{
    [Required(ErrorMessage = "Informe o nome da loja."), StringLength(120)] [Display(Name = "Nome comercial da loja")] public string StoreName { get; set; } = string.Empty;
    [Required, StringLength(10)] [Display(Name = "Moeda")] public string Currency { get; set; } = "BRL";
    [Required, StringLength(60)] [Display(Name = "Fuso horário (ID IANA/Windows)")] public string TimeZoneId { get; set; } = "America/Sao_Paulo";
    [Range(0, 100000)] [Display(Name = "Limite de estoque baixo")] public int LowStockThreshold { get; set; } = 5;
    [Range(15, 10080)] [Display(Name = "Expirar pedidos não pagos após (minutos)")] public int PendingOrderExpirationMinutes { get; set; } = 120;
    [Range(1, 99)] [Display(Name = "Quantidade máxima por produto no carrinho")] public int MaxItemQuantityPerProduct { get; set; } = 20;
}

public class PaymentSettingsForm
{
    [Display(Name = "Integração habilitada")] public bool Enabled { get; set; }
    [Display(Name = "Provedor")] public string Provider { get; set; } = "MercadoPago";
    [Display(Name = "Ambiente")] public string Environment { get; set; } = "sandbox";
    [StringLength(200)] [Display(Name = "Public Key")] public string? PublicKey { get; set; }
    [StringLength(500)] [Display(Name = "Access Token (deixe em branco para manter)")] public string? AccessToken { get; set; }
    [StringLength(500)] [Display(Name = "Chave secreta do webhook (deixe em branco para manter)")] public string? WebhookSecret { get; set; }
    public bool HasAccessToken { get; set; }
    public bool HasWebhookSecret { get; set; }
    public string WebhookUrl { get; set; } = string.Empty;
}

public class ShippingSettingsForm
{
    [Display(Name = "Provedor de frete")] public string Provider { get; set; } = "None";
    [StringLength(9)] [Display(Name = "CEP de origem")] public string? OriginZip { get; set; }
    [Display(Name = "Frete grátis a partir de (R$) - vazio desativa")] public decimal? FreeMinimum { get; set; }
    [Range(0, 60)] [Display(Name = "Prazo de manuseio (dias)")] public int HandlingDays { get; set; }
    [Display(Name = "Ambiente do Melhor Envio")] public string MeEnvironment { get; set; } = "sandbox";
    [StringLength(2000)] [Display(Name = "Token do Melhor Envio (deixe em branco para manter)")] public string? MeToken { get; set; }
    [StringLength(100)] [Display(Name = "Serviços habilitados (IDs separados por vírgula, vazio = todos)")] public string? MeServices { get; set; }
    [EmailAddress] [StringLength(200)] [Display(Name = "E-mail técnico de contato (exigido pelo Melhor Envio)")] public string? MeContactEmail { get; set; }
    public bool HasMeToken { get; set; }
}

public class EmailTemplateForm
{
    public string Kind { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Optional { get; set; }
    public bool Notify { get; set; } = true;
    [StringLength(300)] public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class EmailSettingsForm
{
    [StringLength(200)] [Display(Name = "Servidor SMTP")] public string? Host { get; set; }
    [Range(1, 65535)] [Display(Name = "Porta")] public int Port { get; set; } = 587;
    [Display(Name = "Segurança")] public string Security { get; set; } = "StartTls";
    [StringLength(200)] [Display(Name = "Usuário")] public string? User { get; set; }
    [StringLength(500)] [Display(Name = "Senha (deixe em branco para manter)")] public string? Password { get; set; }
    [EmailAddress] [StringLength(200)] [Display(Name = "E-mail do remetente")] public string? FromAddress { get; set; }
    [StringLength(150)] [Display(Name = "Nome do remetente")] public string? FromName { get; set; }
    public bool HasPassword { get; set; }
    public List<EmailTemplateForm> Templates { get; set; } = new();
}

public class FiscalSettingsForm
{
    [Display(Name = "Provedor fiscal")] public string Provider { get; set; } = "None";
    [Display(Name = "Ambiente")] public string Environment { get; set; } = "homologacao";
    [Display(Name = "Tipo de documento")] public string DocumentType { get; set; } = "NFe";
    [StringLength(60)] [Display(Name = "Regime tributário")] public string? TaxRegime { get; set; }
    [StringLength(10)] [Display(Name = "Série")] public string? Series { get; set; }
    [StringLength(500)] [Display(Name = "Token/credencial do provedor (deixe em branco para manter)")] public string? Token { get; set; }
    [StringLength(500)] [Display(Name = "Senha do certificado (deixe em branco para manter)")] public string? CertificatePassword { get; set; }
    public bool HasToken { get; set; }
    public bool HasCertificatePassword { get; set; }
    public bool ProviderConfigured { get; set; }
}
