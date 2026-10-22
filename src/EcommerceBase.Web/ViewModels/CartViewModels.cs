namespace EcommerceBase.Web.ViewModels;

public class CartLineView
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal OriginalPrice { get; set; }
    public int Quantity { get; set; }
    public int AvailableStock { get; set; }
    public decimal LineTotal { get; set; }
    public string? Issue { get; set; }
}

public class CartView
{
    public List<CartLineView> Lines { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string? CouponCode { get; set; }
    public string? CouponError { get; set; }
    public bool HasIssues => Lines.Any(l => l.Issue != null);
    public int ItemCount => Lines.Sum(l => l.Quantity);
    public bool IsEmpty => Lines.Count == 0;
}

public record CartResult(bool Success, string? Error = null);
