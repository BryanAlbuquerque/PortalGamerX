using EcommerceBase.Web.Data;
using EcommerceBase.Web.Domain.Entities;
using EcommerceBase.Web.Infrastructure;

namespace EcommerceBase.Web.Services;

public interface IAuditService
{
    Task LogAsync(string action, string? entityType = null, string? entityId = null, string? details = null);
}

/// <summary>Registra ações administrativas relevantes. Nunca passe segredos em <c>details</c>.</summary>
public class AuditService : IAuditService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;

    public AuditService(AppDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task LogAsync(string action, string? entityType = null, string? entityId = null, string? details = null)
    {
        var ctx = _http.HttpContext;
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = ctx?.User.GetUserId(),
            UserName = ctx?.User.Identity?.Name,
            Action = action.Truncate(80),
            EntityType = entityType?.Truncate(60),
            EntityId = entityId?.Truncate(60),
            Details = details?.Truncate(1000),
            IpAddress = ctx?.Connection.RemoteIpAddress?.ToString().Truncate(60),
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
