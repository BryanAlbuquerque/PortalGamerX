using PortalGamerX.Data;
using PortalGamerX.Domain;
using PortalGamerX.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace PortalGamerX.Services;

public interface IDashboardService
{
    Task<DashboardVm> GetAsync();
}

/// <summary>Todos os indicadores vêm de consultas reais. Sem dados, os valores são zero.</summary>
public class DashboardService : IDashboardService
{
    private static readonly OrderStatus[] Revenue = { OrderStatus.Paid, OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Delivered };
    private readonly AppDbContext _db;
    private readonly IStoreSettingsService _store;

    public DashboardService(AppDbContext db, IStoreSettingsService store)
    {
        _db = db;
        _store = store;
    }

    public async Task<DashboardVm> GetAsync()
    {
        var settings = await _store.GetAsync();
        var now = DateTime.UtcNow;
        var since30 = now.AddDays(-30);

        var vm = new DashboardVm
        {
            LowStockThreshold = settings.LowStockThreshold,
            TotalOrders = await _db.Orders.CountAsync(),
            PendingOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.PendingPayment),
            PaidOrders = await _db.Orders.CountAsync(o => Revenue.Contains(o.Status)),
            CancellationRequests = await _db.Orders.CountAsync(o => o.CancellationRequested && o.Status != OrderStatus.Cancelled),
            JogoCount = await _db.Jogos.CountAsync(),
            OrdersLast30Days = await _db.Orders.CountAsync(o => Revenue.Contains(o.Status) && o.PaidAtUtc >= since30),
            SalesLast30Days = (await _db.Orders.Where(o => Revenue.Contains(o.Status) && o.PaidAtUtc >= since30)
                .SumAsync(o => (decimal?)o.Total)) ?? 0m,
            CustomerCount = await (from ur in _db.UserRoles
                                   join r in _db.Roles on ur.RoleId equals r.Id
                                   where r.Name == Roles.Customer
                                   select ur.UserId).CountAsync(),
            LowStock = await _db.Jogos.AsNoTracking()
                .Where(p => p.IsActive && p.StockQuantity <= settings.LowStockThreshold)
                .OrderBy(p => p.StockQuantity).ThenBy(p => p.Name).Take(10).ToListAsync(),
            RecentOrders = await _db.Orders.AsNoTracking().OrderByDescending(o => o.CreatedAtUtc).Take(8).ToListAsync()
        };

        var since14 = now.AddDays(-14);
        var paid = await _db.Orders.AsNoTracking()
            .Where(o => Revenue.Contains(o.Status) && o.PaidAtUtc >= since14)
            .Select(o => new { o.PaidAtUtc, o.Total }).ToListAsync();

        var today = _store.ToLocal(now, settings).Date;
        for (var i = 13; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            var rows = paid.Where(p => p.PaidAtUtc.HasValue && _store.ToLocal(p.PaidAtUtc.Value, settings).Date == day).ToList();
            vm.Daily.Add(new DailySales { Label = day.ToString("dd/MM"), Total = rows.Sum(r => r.Total), Orders = rows.Count });
        }
        return vm;
    }
}
