using EcommerceBase.Domain.Enums;

namespace EcommerceBase.Domain.Rules;

public static class Money
{
    /// <summary>Arredondamento comercial para 2 casas (metade para longe de zero).</summary>
    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

public static class PricingRules
{
    /// <summary>Preço promocional só vale se for positivo e menor que o preço normal.</summary>
    public static bool IsOnSale(decimal price, decimal? promotionalPrice) =>
        promotionalPrice.HasValue && promotionalPrice.Value > 0m && promotionalPrice.Value < price;

    public static decimal EffectivePrice(decimal price, decimal? promotionalPrice) =>
        IsOnSale(price, promotionalPrice) ? Money.Round(promotionalPrice!.Value) : Money.Round(price);
}

/// <summary>Linha já precificada pelo servidor (nunca pelo navegador).</summary>
public readonly record struct PricedLine(int ProductId, int? CategoryId, decimal UnitPrice, int Quantity)
{
    public decimal LineTotal => Money.Round(UnitPrice * Quantity);
}

public sealed record OrderTotals(decimal Subtotal, decimal Discount, decimal Shipping, decimal Total);

public static class PricingCalculator
{
    public static decimal Subtotal(IEnumerable<PricedLine> lines) =>
        Money.Round(lines.Sum(l => l.LineTotal));

    /// <summary>
    /// Calcula os totais. O desconto nunca supera o subtotal e o total nunca fica negativo.
    /// </summary>
    public static OrderTotals Calculate(IReadOnlyCollection<PricedLine> lines, decimal discount, decimal shipping)
    {
        var subtotal = Subtotal(lines);
        var safeDiscount = Math.Min(Math.Max(Money.Round(discount), 0m), subtotal);
        var safeShipping = Math.Max(Money.Round(shipping), 0m);
        var total = Money.Round(subtotal - safeDiscount + safeShipping);
        return new OrderTotals(subtotal, safeDiscount, safeShipping, total);
    }
}

public sealed record CouponSnapshot(
    int Id,
    string Code,
    DiscountType Type,
    decimal Value,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    decimal? MinimumOrderAmount,
    int? MaxUses,
    int? MaxUsesPerCustomer,
    bool IsActive,
    int UsedCount,
    int? CategoryId,
    int? ProductId);

public sealed record CouponEvaluation(bool IsValid, string? Error, decimal Discount)
{
    public static CouponEvaluation Invalid(string error) => new(false, error, 0m);
}

public static class CouponRules
{
    /// <summary>
    /// Avalia um cupom contra as linhas do carrinho. <paramref name="customerUses"/> é a quantidade de vezes
    /// que o cliente atual já usou o cupom (0 para visitantes).
    /// </summary>
    public static CouponEvaluation Evaluate(
        CouponSnapshot coupon,
        IReadOnlyCollection<PricedLine> lines,
        DateTime nowUtc,
        int customerUses)
    {
        if (!coupon.IsActive)
            return CouponEvaluation.Invalid("Este cupom não está ativo.");
        if (coupon.StartsAtUtc.HasValue && nowUtc < coupon.StartsAtUtc.Value)
            return CouponEvaluation.Invalid("Este cupom ainda não está válido.");
        if (coupon.EndsAtUtc.HasValue && nowUtc > coupon.EndsAtUtc.Value)
            return CouponEvaluation.Invalid("Este cupom expirou.");
        if (coupon.MaxUses.HasValue && coupon.UsedCount >= coupon.MaxUses.Value)
            return CouponEvaluation.Invalid("Este cupom atingiu o limite de utilizações.");
        if (coupon.MaxUsesPerCustomer.HasValue && customerUses >= coupon.MaxUsesPerCustomer.Value)
            return CouponEvaluation.Invalid("Você já utilizou este cupom o número máximo de vezes.");

        var orderSubtotal = PricingCalculator.Subtotal(lines);
        if (coupon.MinimumOrderAmount.HasValue && orderSubtotal < coupon.MinimumOrderAmount.Value)
        {
            var minimum = coupon.MinimumOrderAmount.Value
                .ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                .Replace('.', ',');
            return CouponEvaluation.Invalid($"Este cupom exige um pedido mínimo de R$ {minimum}.");
        }

        var eligible = lines.Where(l =>
                (!coupon.ProductId.HasValue || l.ProductId == coupon.ProductId.Value) &&
                (!coupon.CategoryId.HasValue || l.CategoryId == coupon.CategoryId.Value))
            .ToList();

        var baseAmount = Money.Round(eligible.Sum(l => l.LineTotal));
        if (baseAmount <= 0m)
            return CouponEvaluation.Invalid("Este cupom não se aplica aos produtos do seu carrinho.");

        decimal discount = coupon.Type == DiscountType.Percentage
            ? Money.Round(baseAmount * coupon.Value / 100m)
            : coupon.Value;

        discount = Math.Min(Money.Round(discount), baseAmount);
        if (discount <= 0m)
            return CouponEvaluation.Invalid("Este cupom não gera desconto para o seu carrinho.");

        return new CouponEvaluation(true, null, discount);
    }
}
