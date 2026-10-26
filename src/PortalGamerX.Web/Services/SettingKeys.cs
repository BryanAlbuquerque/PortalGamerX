namespace PortalGamerX.Services;

public static class SettingKeys
{
    // Pagamentos
    public const string PaymentProvider = "payment.provider";           // MercadoPago | (vazio)
    public const string PaymentEnabled = "payment.enabled";
    public const string MpEnvironment = "payment.mp.environment";      // sandbox | production
    public const string MpAccessToken = "payment.mp.accessToken";      // secreto
    public const string MpPublicKey = "payment.mp.publicKey";
    public const string MpWebhookSecret = "payment.mp.webhookSecret";  // secreto

    // E-mail
    public const string EmailProvider = "email.provider";              // Smtp
    public const string SmtpHost = "email.smtp.host";
    public const string SmtpPort = "email.smtp.port";
    public const string SmtpSecurity = "email.smtp.security";          // StartTls | SslOnConnect | None
    public const string SmtpUser = "email.smtp.user";
    public const string SmtpPassword = "email.smtp.password";          // secreto
    public const string EmailFromAddress = "email.from.address";
    public const string EmailFromName = "email.from.name";
    public static string EmailNotify(string kind) => $"email.notify.{kind}";
    public static string EmailTemplateSubject(string kind) => $"email.template.{kind}.subject";
    public static string EmailTemplateBody(string kind) => $"email.template.{kind}.body";

    // Fiscal
    public const string FiscalProvider = "fiscal.provider";            // (vazio = não configurado)
    public const string FiscalEnvironment = "fiscal.environment";      // homologacao | producao
    public const string FiscalDocumentType = "fiscal.documentType";    // NFSe (licenciamento de software = ISS)
    public const string FiscalTaxRegime = "fiscal.taxRegime";
    public const string FiscalServiceCode = "fiscal.serviceCode";                  // item da lista LC 116 (padrão 01.05)
    public const string FiscalMunicipalTaxCode = "fiscal.municipalTaxCode";        // código de tributação do município
    public const string FiscalCnae = "fiscal.cnae";
    public const string FiscalIssRate = "fiscal.issRate";                          // alíquota de ISS em %
    public const string FiscalSeries = "fiscal.series";
    public const string FiscalToken = "fiscal.token";                  // secreto
    public const string FiscalCertificatePassword = "fiscal.certificatePassword"; // secreto

    public static readonly string[] SecretKeys =
    {
        MpAccessToken, MpWebhookSecret, SmtpPassword, FiscalToken, FiscalCertificatePassword
    };
}
