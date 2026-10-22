using EcommerceBase.Application.Inputs;
using EcommerceBase.Domain.Enums;

namespace EcommerceBase.Application.Dtos;

public class AdminProductQuery
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    /// <summary>all | active | inactive | low</summary>
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AdminProductListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal EffectivePrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public bool IsLowStock { get; set; }
    public string? ImageKey { get; set; }
}

public class ProductEditDto
{
    public ProductInput Input { get; set; } = new();
    public List<ProductImageDto> Images { get; set; } = new();
    public int StockQuantity { get; set; }
}

public class CategoryAdminItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
}

public class BrandAdminItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
}

public class CouponListItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    public decimal Value { get; set; }
    public int UsedCount { get; set; }
    public int? MaxUses { get; set; }
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public bool IsActive { get; set; }
}

public class CouponEvaluationResult
{
    public bool IsValid { get; set; }
    public string? Error { get; set; }
    public decimal Discount { get; set; }
    public int? CouponId { get; set; }
    public string? Code { get; set; }

    public static CouponEvaluationResult Invalid(string error) => new() { IsValid = false, Error = error };
}

public class StockOverviewItem
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; }
    public bool IsLow { get; set; }
    public bool IsActive { get; set; }
}

public class StockMovementDto
{
    public long Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public StockMovementType Type { get; set; }
    public int QuantityDelta { get; set; }
    public int BalanceAfter { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string? PerformedBy { get; set; }
}

public class CustomerListItem
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public int OrderCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CustomerDetailDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Document { get; set; }
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<AddressDto> Addresses { get; set; } = new();
    public List<OrderSummaryDto> Orders { get; set; } = new();
}

public class DailySalesPoint
{
    public DateTime Date { get; set; }
    public decimal Total { get; set; }
    public int Orders { get; set; }
}

public class DashboardDto
{
    public int PeriodDays { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int PaidOrders { get; set; }
    public decimal SalesInPeriod { get; set; }
    public int ProductCount { get; set; }
    public int LowStockCount { get; set; }
    public int CustomerCount { get; set; }
    public int UnreadNotifications { get; set; }
    public List<OrderSummaryDto> RecentOrders { get; set; } = new();
    public List<StockOverviewItem> LowStockProducts { get; set; } = new();
    public List<DailySalesPoint> SalesByDay { get; set; } = new();
    public Dictionary<OrderStatus, int> OrdersByStatus { get; set; } = new();
}

public class NotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Link { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class AuditLogDto
{
    public long Id { get; set; }
    public string? UserEmail { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class BannerAdminItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ImageKey { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
}

public class ContentPageAdminItem
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool ShowInFooter { get; set; }
    public int SortOrder { get; set; }
}
