using PortalGamerX.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace PortalGamerX.Integrations.Email;

public class SmtpEmailProvider : IEmailProvider
{
    private readonly IIntegrationSettings _settings;
    private readonly ILogger<SmtpEmailProvider> _logger;

    public SmtpEmailProvider(IIntegrationSettings settings, ILogger<SmtpEmailProvider> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public string Name => "Smtp";

    public async Task<bool> IsConfiguredAsync() =>
        await _settings.HasValueAsync(SettingKeys.SmtpHost) && await _settings.HasValueAsync(SettingKeys.EmailFromAddress);

    public async Task<ProviderResult<string>> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        if (!await IsConfiguredAsync()) return ProviderResult<string>.Fail("SMTP não configurado (servidor e remetente são obrigatórios).");

        var host = (await _settings.GetAsync(SettingKeys.SmtpHost))!;
        var port = int.TryParse(await _settings.GetAsync(SettingKeys.SmtpPort), out var p) ? p : 587;
        var security = await _settings.GetOrDefaultAsync(SettingKeys.SmtpSecurity, "StartTls");
        var user = await _settings.GetAsync(SettingKeys.SmtpUser);
        var pass = await _settings.GetAsync(SettingKeys.SmtpPassword);
        var fromAddress = (await _settings.GetAsync(SettingKeys.EmailFromAddress))!;
        var fromName = await _settings.GetOrDefaultAsync(SettingKeys.EmailFromName, fromAddress);

        var option = security switch
        {
            "SslOnConnect" => SecureSocketOptions.SslOnConnect,
            "None" => SecureSocketOptions.None,
            _ => SecureSocketOptions.StartTls
        };

        try
        {
            var mime = new MimeMessage();
            mime.From.Add(new MailboxAddress(fromName, fromAddress));
            mime.To.Add(new MailboxAddress(message.ToName ?? string.Empty, message.ToAddress));
            mime.Subject = message.Subject;
            mime.Body = new BodyBuilder { HtmlBody = message.BodyHtml }.ToMessageBody();

            using var client = new SmtpClient();
            client.Timeout = 20000;
            await client.ConnectAsync(host, port, option, ct);
            if (!string.IsNullOrWhiteSpace(user))
                await client.AuthenticateAsync(user, pass ?? string.Empty, ct);
            var response = await client.SendAsync(mime, ct);
            await client.DisconnectAsync(true, ct);
            return ProviderResult<string>.Ok(response ?? "ok");
        }
        catch (Exception ex)
        {
            // Não registra corpo/destinatário (podem conter token). Apenas o tipo e a mensagem do erro.
            _logger.LogWarning("Falha no envio SMTP: {Type}: {Message}", ex.GetType().Name, ex.Message);
            return ProviderResult<string>.Fail($"{ex.GetType().Name}: {ex.Message}".Truncate500());
        }
    }
}

internal static class StringTruncateExtensions
{
    public static string Truncate500(this string s) => s.Length <= 500 ? s : s.Substring(0, 500);
}
