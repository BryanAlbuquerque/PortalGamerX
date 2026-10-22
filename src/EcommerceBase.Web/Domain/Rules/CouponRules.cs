using EcommerceBase.Web.Domain.Entities;

namespace EcommerceBase.Web.Domain.Rules;

public record CouponEvaluation(bool IsValid, string? Error, decimal Discount)
{
    public static CouponEvaluation Fail(string error) => new(false, error, 0m);
}

public static class CouponRules
{
    /// <summary>
    /// Avalia um cupom contra as linhas do carrinho. Função pura: contagens de uso e data são informadas pelo chamador.
    /// Se o cupom tiver categorias/produtos, o desconto incide apenas sobre as linhas elegíveis.
    /// </summary>
    public static CouponEvaluation Evaluate(Coupon coupon, IReadOnlyList<PricingLine> lines, DateTime nowUtc, int totalUses, int userUses)
    {
        if (!coupon.IsActive) return CouponEvaluation.Fail("Cupom inativo.");
        if (coupon.StartsAtUtc.HasValue && nowUtc < coupon.StartsAtUtc.Value) return CouponEvaluation.Fail("Cupom ainda não está válido.");
        if (coupon.EndsAtUtc.HasValue && nowUtc > coupon.EndsAtUtc.Value) return CouponEvaluation.Fail("Cupom expirado.");
        if (coupon.MaxUses.HasValue && totalUses >= coupon.MaxUses.Value) return CouponEvaluation.Fail("Cupom esgotado.");
        if (coupon.MaxUsesPerCustomer.HasValue && userUses >= coupon.MaxUsesPerCustomer.Value)
            return CouponEvaluation.Fail("Você já utilizou este cupom o número máximo de vezes.");
        if (lines.Count == 0) return CouponEvaluation.Fail("O carrinho está vazio.");

        var subtotal = Money.Round(lines.Sum(l => l.LineTotal));
        if (coupon.MinOrderAmount.HasValue && subtotal < coupon.MinOrderAmount.Value)
            return CouponEvaluation.Fail($"Pedido mínimo para este cupom: {coupon.MinOrderAmount.Value.ToBrl()}.");

        var hasRestriction = coupon.Categories.Count > 0 || coupon.Products.Count > 0;
        IEnumerable<PricingLine> eligible = lines;
        if (hasRestriction)
        {
            var categoryIds = coupon.Categories.Select(c => c.Id).ToHashSet();
            var productIds = coupon.Products.Select(p => p.Id).ToHashSet();
            eligible = lines.Where(l => productIds.Contains(l.ProductId) || categoryIds.Contains(l.CategoryId));
        }

        var eligibleSubtotal = Money.Round(eligible.Sum(l => l.LineTotal));
        if (eligibleSubtotal <= 0) return CouponEvaluation.Fail("Cupom não se aplica aos itens do carrinho.");

        decimal discount;
        if (coupon.DiscountType == DiscountType.Percentage)
        {
            if (coupon.Value <= 0 || coupon.Value > 100) return CouponEvaluation.Fail("Cupom configurado incorretamente.");
            discount = Money.Round(eligibleSubtotal * coupon.Value / 100m);
            if (coupon.MaxDiscountAmount.HasValue) discount = Math.Min(discount, coupon.MaxDiscountAmount.Value);
        }
        else
        {
            if (coupon.Value <= 0) return CouponEvaluation.Fail("Cupom configurado incorretamente.");
            discount = Math.Min(Money.Round(coupon.Value), eligibleSubtotal);
        }

        discount = Math.Min(discount, subtotal);
        return new CouponEvaluation(true, null, Money.Round(discount));
    }
}
