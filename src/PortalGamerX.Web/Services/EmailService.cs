using System.Net;
using PortalGamerX.Data;
using PortalGamerX.Domain;
using PortalGamerX.Domain.Entities;
using PortalGamerX.Integrations;
using PortalGamerX.Integrations.Email;

namespace PortalGamerX.Services;

public static class EmailKinds
{
    public const string ConfirmEmail = "ConfirmEmail";
    public const string ResetPassword = "ResetPassword";
    public const string OrderConfirmation = "OrderConfirmation";
    public const string PaymentUpdate = "PaymentUpdate";
    public const string OrderUpdate = "OrderUpdate";
    /// <summary>E-mail com as keys compradas. Obrigatório (não pode ser desligado) e marcado como sensível.</summary>
    public const string KeysDelivered = "KeysDelivered";
    public const string AdminNewOrder = "AdminNewOrder";

    /// <summary>Tipos que o administrador pode ligar/desligar.</summary>
    public static readonly string[] Optional = { OrderConfirmation, PaymentUpdate, OrderUpdate, AdminNewOrder };
    public static readonly string[] All = { ConfirmEmail, ResetPassword, OrderConfirmation, PaymentUpdate, OrderUpdate, KeysDelivered, AdminNewOrder };

    public static string Label(string kind) => kind switch
    {
        ConfirmEmail => "Confirmação de e-mail",
        ResetPassword => "Recuperação de senha",
        OrderConfirmation => "Confirmação de pedido",
        PaymentUpdate => "Atualização de pagamento",
        OrderUpdate => "Atualização do pedido",
        KeysDelivered => "Entrega das keys",
        AdminNewOrder => "Aviso de novo pedido (administrador)",
        _ => kind
    };
}

public static class EmailTemplates
{
    public static (string Subject, string Body) Default(string kind) => kind switch
    {
        EmailKinds.ConfirmEmail => ("Confirme seu e-mail - {{StoreName}}",
            "<p>Olá, {{CustomerName}}!</p><p>Confirme seu e-mail clicando no link abaixo:</p><p><a href=\"{{Link}}\">Confirmar e-mail</a></p><p>Se você não criou esta conta, ignore esta mensagem.</p>"),
        EmailKinds.ResetPassword => ("Redefinição de senha - {{StoreName}}",
            "<p>Olá, {{CustomerName}}!</p><p>Recebemos um pedido para redefinir sua senha. Clique no link abaixo (válido por tempo limitado):</p><p><a href=\"{{Link}}\">Redefinir senha</a></p><p>Se você não solicitou, ignore esta mensagem.</p>"),
        EmailKinds.OrderConfirmation => ("Recebemos seu pedido {{OrderNumber}} - {{StoreName}}",
            "<p>Olá, {{CustomerName}}!</p><p>Recebemos seu pedido <strong>{{OrderNumber}}</strong> no valor de <strong>{{Total}}</strong>.</p><p>Acompanhe em: <a href=\"{{Link}}\">{{Link}}</a></p><p>O pedido só é confirmado após a aprovação do pagamento.</p>"),
        EmailKinds.PaymentUpdate => ("Pagamento do pedido {{OrderNumber}}: {{Status}}",
            "<p>Olá, {{CustomerName}}!</p><p>O pagamento do pedido <strong>{{OrderNumber}}</strong> está: <strong>{{Status}}</strong>.</p><p><a href=\"{{Link}}\">Ver pedido</a></p>"),
        EmailKinds.OrderUpdate => ("Atualização do pedido {{OrderNumber}}: {{Status}}",
            "<p>Olá, {{CustomerName}}!</p><p>Seu pedido <strong>{{OrderNumber}}</strong> agora está: <strong>{{Status}}</strong>.</p><p><a href=\"{{Link}}\">Ver pedido</a></p>"),
        EmailKinds.KeysDelivered => ("Suas keys do pedido {{OrderNumber}} - {{StoreName}}",
            "<p>Olá, {{CustomerName}}!</p><p>Pagamento aprovado! Aqui estão as keys do pedido <strong>{{OrderNumber}}</strong>:</p>{{KeysHtml}}<p>Elas também ficam disponíveis em <a href=\"{{Link}}\">Minha conta › Pedidos</a>.</p><p><strong>Guarde suas keys com segurança e não as compartilhe.</strong> Cada key só pode ser ativada uma vez, na loja e região indicadas.</p>"),
        EmailKinds.AdminNewOrder => ("Novo pedido {{OrderNumber}} ({{Total}})",
            "<p>Novo pedido <strong>{{OrderNumber}}</strong> de {{CustomerName}} no valor de {{Total}}.</p><p><a href=\"{{Link}}\">Abrir no painel</a></p>"),
        _ => ("{{StoreName}}", "<p>{{Message}}</p>")
    };

    /// <summary>
    /// Substitui {{Chave}} por valores HTML-codificados. Valores desconhecidos viram vazio.
    /// Exceção: variáveis cujo nome termina em "Html" são inseridas como estão e só podem ser montadas pelo servidor
    /// (já com cada parte codificada); nunca com texto vindo do usuário sem codificar.
    /// </summary>
    public static string Render(string template, IReadOnlyDictionary<string, string> vars)
    {
        var result = template;
        foreach (var kv in vars)
            result = result.Replace("{{" + kv.Key + "}}",
                kv.Key.EndsWith("Html", StringComparison.Ordinal) ? kv.Value : WebUtility.HtmlEncode(kv.Value));
        return System.Text.RegularExpressions.Regex.Replace(result, @"\{\{[A-Za-z]+\}\}", string.Empty);
    }
}

public interface IEmailService
{
    Task EnqueueAsync(string kind, string toEmail, string? toName, Dictionary<string, string> vars, bool sensitive = false);
    Task<ProviderResult<string>> SendTestAsync(string toEmail);
}

public class EmailService : IEmailService
{
    private readonly AppDbContext _db;
    private readonly IIntegrationSettings _settings;
    private readonly IStoreSettingsService _store;
    private readonly IEmailProvider _provider;

    public EmailService(AppDbContext db, IIntegrationSettings settings, IStoreSettingsService store, IEmailProvider provider)
    {
        _db = db;
        _settings = settings;
        _store = store;
        _provider = provider;
    }

    public async Task EnqueueAsync(string kind, string toEmail, string? toName, Dictionary<string, string> vars, bool sensitive = false)
    {
        if (string.IsNullOrWhiteSpace(toEmail)) return;
        if (EmailKinds.Optional.Contains(kind) && !await _settings.GetBoolAsync(SettingKeys.EmailNotify(kind), true)) return;

        var store = await _store.GetAsync();
        var all = new Dictionary<string, string>(vars) { ["StoreName"] = store.StoreName };
        if (!all.ContainsKey("CustomerName")) all["CustomerName"] = toName ?? string.Empty;

        var def = EmailTemplates.Default(kind);
        var subjectTpl = await _settings.GetOrDefaultAsync(SettingKeys.EmailTemplateSubject(kind), def.Subject);
        var bodyTpl = await _settings.GetOrDefaultAsync(SettingKeys.EmailTemplateBody(kind), def.Body);

        _db.EmailOutbox.Add(new EmailOutbox
        {
            ToAddress = toEmail.Trim(),
            ToName = toName,
            Subject = EmailTemplates.Render(subjectTpl, all).Replace("\r", " ").Replace("\n", " "),
            BodyHtml = EmailTemplates.Render(bodyTpl, all),
            Kind = kind,
            IsSensitive = sensitive,
            Status = EmailStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            NextAttemptUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public Task<ProviderResult<string>> SendTestAsync(string toEmail) =>
        _provider.SendAsync(new EmailMessage(toEmail, null, "Teste de e-mail", "<p>Este é um e-mail de teste enviado pelo painel administrativo.</p>"));
}
