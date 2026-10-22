namespace EcommerceBase.Web.Domain.Rules;

public record PricingLine(int ProductId, int CategoryId, string Sku, decimal UnitPrice, int Quantity)
{
    public decimal LineTotal => Money.Round(UnitPrice * Quantity);
}

public record PricingResult(decimal Subtotal, decimal Discount, decimal Shipping, decimal Total);

public static class PricingCalculator
{
    /// <summary>Calcula totais. O desconto nunca excede o subtotal; o total nunca é negativo.</summary>
    public static PricingResult Calculate(IEnumerable<PricingLine> lines, decimal discount, decimal shipping)
    {
        var subtotal = Money.Round(lines.Sum(l => l.LineTotal));
        var d = Math.Min(Math.Max(Money.Round(discount), 0m), subtotal);
        var s = Math.Max(Money.Round(shipping), 0m);
        var total = Money.Round(subtotal - d + s);
        return new PricingResult(subtotal, d, s, total);
    }
}
