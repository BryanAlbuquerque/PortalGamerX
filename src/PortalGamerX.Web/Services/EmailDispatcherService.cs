using PortalGamerX.Data;
using PortalGamerX.Domain;
using PortalGamerX.Integrations.Email;
using Microsoft.EntityFrameworkCore;

namespace PortalGamerX.Services;

/// <summary>Processa a fila de e-mails em segundo plano, com novas tentativas e backoff.</summary>
public class EmailDispatcherService : BackgroundService
{
    private const int MaxAttempts = 5;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<EmailDispatcherService> _logger;

    public EmailDispatcherService(IServiceScopeFactory scopes, ILogger<EmailDispatcherService> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessBatchAsync(stoppingToken); }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Erro no despachante de e-mails."); }

            try { await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var provider = scope.ServiceProvider.GetRequiredService<IEmailProvider>();

        var now = DateTime.UtcNow;
        var batch = await db.EmailOutbox
            .Where(e => e.Status == EmailStatus.Pending && e.NextAttemptUtc <= now)
            .OrderBy(e => e.Id)
            .Take(20)
            .ToListAsync(ct);

        foreach (var mail in batch)
        {
            var result = await provider.SendAsync(new EmailMessage(mail.ToAddress, mail.ToName, mail.Subject, mail.BodyHtml), ct);
            mail.Attempts++;
            if (result.Success)
            {
                mail.Status = EmailStatus.Sent;
                mail.SentAtUtc = DateTime.UtcNow;
                mail.LastError = null;
                if (mail.IsSensitive) mail.BodyHtml = "[conteúdo removido após o envio]";
            }
            else
            {
                mail.LastError = result.Error;
                if (mail.Attempts >= MaxAttempts) mail.Status = EmailStatus.Failed;
                else mail.NextAttemptUtc = DateTime.UtcNow.AddMinutes(Math.Pow(2, mail.Attempts));
            }
            await db.SaveChangesAsync(ct);
        }
    }
}
