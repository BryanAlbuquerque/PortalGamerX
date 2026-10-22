namespace EcommerceBase.Web.Integrations.Email;

public record EmailMessage(string ToAddress, string? ToName, string Subject, string BodyHtml);

public interface IEmailProvider
{
    string Name { get; }
    Task<bool> IsConfiguredAsync();
    /// <summary>Retorna Fail se o provedor rejeitar a mensagem. Nunca considera sucesso sem confirmação do servidor SMTP.</summary>
    Task<ProviderResult<string>> SendAsync(EmailMessage message, CancellationToken ct = default);
}
