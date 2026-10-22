using EcommerceBase.Web.Domain.Rules;

namespace EcommerceBase.Tests.Domain;

public class PricingTests
{
    private static PricingLine Line(decimal price, int qty, int product = 1, int category = 1) => new(product, category, "SKU" + product, price, qty);

    [Fact]
    public void Soma_subtotal_desconto_e_frete()
    {
        var r = PricingCalculator.Calculate(new[] { Line(10.10m, 3), Line(5m, 2, 2) }, 5m, 12.34m);
        Assert.Equal(40.30m, r.Subtotal);
        Assert.Equal(5m, r.Discount);
        Assert.Equal(12.34m, r.Shipping);
        Assert.Equal(47.64m, r.Total);
    }

    [Fact]
    public void Desconto_nunca_excede_o_subtotal()
    {
        var r = PricingCalculator.Calculate(new[] { Line(10m, 1) }, 999m, 0m);
        Assert.Equal(10m, r.Discount);
        Assert.Equal(0m, r.Total);
    }

    [Fact]
    public void Valores_negativos_sao_ignorados()
    {
        var r = PricingCalculator.Calculate(new[] { Line(10m, 1) }, -5m, -3m);
        Assert.Equal(0m, r.Discount);
        Assert.Equal(0m, r.Shipping);
        Assert.Equal(10m, r.Total);
    }
}
