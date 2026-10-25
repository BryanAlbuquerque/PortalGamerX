using PortalGamerX.Domain.Entities;

namespace PortalGamerX.ViewModels;

public class DailySales
{
    public string Label { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int Orders { get; set; }
}

public class DashboardVm
{
    public int TotalOrders { get; set; }
    public decimal SalesLast30Days { get; set; }
    public int OrdersLast30Days { get; set; }
    public int PendingOrders { get; set; }
    public int PaidOrders { get; set; }
    public int CancellationRequests { get; set; }
    public int JogoCount { get; set; }
    public int CustomerCount { get; set; }
    public int LowStockThreshold { get; set; }
    public List<Jogo> LowStock { get; set; } = new();
    public List<Order> RecentOrders { get; set; } = new();
    public List<DailySales> Daily { get; set; } = new();
}
