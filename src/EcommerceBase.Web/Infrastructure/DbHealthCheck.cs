using EcommerceBase.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EcommerceBase.Web.Infrastructure;

public class DbHealthCheck : IHealthCheck
{
    private readonly AppDbContext _db;
    public DbHealthCheck(AppDbContext db) => _db = db;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await _db.Database.CanConnectAsync(cancellationToken)) return HealthCheckResult.Unhealthy("Sem conexão com o banco.");
            var pending = await _db.Database.GetPendingMigrationsAsync(cancellationToken);
            return pending.Any() ? HealthCheckResult.Degraded("Migrations pendentes.") : HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Falha ao consultar o banco.", ex);
        }
    }
}
