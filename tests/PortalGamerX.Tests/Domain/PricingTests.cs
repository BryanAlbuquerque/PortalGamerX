using PortalGamerX.Domain.Rules;

namespace PortalGamerX.Tests.Domain;

public class PricingTests
{
    private static PricingLine Line(decimal price, int qty, int jogo = 1, int category = 1) => new(jogo, category, "SKU" + jogo, price, qty);

    [Fact]
    public void Soma_subtotal_e_desconto()
    {
        var r = PricingCalculator.Calculate(new[] { Line(10.10m, 3), Line(5m, 2, 2) }, 5m);
        Assert.Equal(40.30m, r.Subtotal);
        Assert.Equal(5m, r.Discount);
        Assert.Equal(35.30m, r.Total);
    }

    [Fact]
    public void Desconto_nunca_excede_o_subtotal()
    {
        var r = PricingCalculator.Calculate(new[] { Line(10m, 1) }, 999m);
        Assert.Equal(10m, r.Discount);
        Assert.Equal(0m, r.Total);
    }

    [Fact]
    public void Desconto_negativo_e_ignorado()
    {
        var r = PricingCalculator.Calculate(new[] { Line(10m, 1) }, -5m);
        Assert.Equal(0m, r.Discount);
        Assert.Equal(10m, r.Total);
    }
}
