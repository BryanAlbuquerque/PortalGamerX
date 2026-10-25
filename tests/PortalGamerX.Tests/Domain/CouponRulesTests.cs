using PortalGamerX.Domain;
using PortalGamerX.Domain.Entities;
using PortalGamerX.Domain.Rules;

namespace PortalGamerX.Tests.Domain;

public class CouponRulesTests
{
    private static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    private static PricingLine Line(int jogo, int category, decimal price, int qty = 1) => new(jogo, category, "S" + jogo, price, qty);
    private static Coupon Pct(decimal v) => new() { Code = "X", DiscountType = DiscountType.Percentage, Value = v };

    [Fact]
    public void Percentual_simples()
    {
        var r = CouponRules.Evaluate(Pct(10), new[] { Line(1, 1, 100m) }, Now, 0, 0);
        Assert.True(r.IsValid);
        Assert.Equal(10m, r.Discount);
    }

    [Fact]
    public void Percentual_respeita_desconto_maximo()
    {
        var c = Pct(50); c.MaxDiscountAmount = 20m;
        var r = CouponRules.Evaluate(c, new[] { Line(1, 1, 200m) }, Now, 0, 0);
        Assert.Equal(20m, r.Discount);
    }

    [Fact]
    public void Valor_fixo_nao_passa_do_subtotal()
    {
        var c = new Coupon { Code = "X", DiscountType = DiscountType.FixedAmount, Value = 500m };
        var r = CouponRules.Evaluate(c, new[] { Line(1, 1, 80m) }, Now, 0, 0);
        Assert.Equal(80m, r.Discount);
    }

    [Fact]
    public void Inativo_expirado_e_futuro_sao_rejeitados()
    {
        var inactive = Pct(10); inactive.IsActive = false;
        Assert.False(CouponRules.Evaluate(inactive, new[] { Line(1, 1, 10m) }, Now, 0, 0).IsValid);

        var expired = Pct(10); expired.EndsAtUtc = Now.AddDays(-1);
        Assert.False(CouponRules.Evaluate(expired, new[] { Line(1, 1, 10m) }, Now, 0, 0).IsValid);

        var future = Pct(10); future.StartsAtUtc = Now.AddDays(1);
        Assert.False(CouponRules.Evaluate(future, new[] { Line(1, 1, 10m) }, Now, 0, 0).IsValid);
    }

    [Fact]
    public void Limites_de_uso_total_e_por_cliente()
    {
        var c = Pct(10); c.MaxUses = 5; c.MaxUsesPerCustomer = 1;
        Assert.False(CouponRules.Evaluate(c, new[] { Line(1, 1, 10m) }, Now, 5, 0).IsValid);
        Assert.False(CouponRules.Evaluate(c, new[] { Line(1, 1, 10m) }, Now, 2, 1).IsValid);
        Assert.True(CouponRules.Evaluate(c, new[] { Line(1, 1, 10m) }, Now, 2, 0).IsValid);
    }

    [Fact]
    public void Pedido_minimo()
    {
        var c = Pct(10); c.MinOrderAmount = 100m;
        Assert.False(CouponRules.Evaluate(c, new[] { Line(1, 1, 99.99m) }, Now, 0, 0).IsValid);
        Assert.True(CouponRules.Evaluate(c, new[] { Line(1, 1, 100m) }, Now, 0, 0).IsValid);
    }

    [Fact]
    public void Restricao_por_categoria_aplica_so_nos_itens_elegiveis()
    {
        var c = Pct(10);
        c.Categories.Add(new Category { Id = 7, Name = "Elegível" });
        var lines = new[] { Line(1, 7, 100m), Line(2, 8, 900m) };
        var r = CouponRules.Evaluate(c, lines, Now, 0, 0);
        Assert.True(r.IsValid);
        Assert.Equal(10m, r.Discount);
    }

    [Fact]
    public void Restricao_sem_itens_elegiveis_e_rejeitada()
    {
        var c = Pct(10);
        c.Categories.Add(new Category { Id = 7, Name = "Elegível" });
        Assert.False(CouponRules.Evaluate(c, new[] { Line(2, 8, 50m) }, Now, 0, 0).IsValid);
    }

    [Fact]
    public void Percentual_invalido_e_rejeitado()
    {
        Assert.False(CouponRules.Evaluate(Pct(150), new[] { Line(1, 1, 10m) }, Now, 0, 0).IsValid);
        Assert.False(CouponRules.Evaluate(Pct(0), new[] { Line(1, 1, 10m) }, Now, 0, 0).IsValid);
    }

    [Fact]
    public void Carrinho_vazio_e_rejeitado() =>
        Assert.False(CouponRules.Evaluate(Pct(10), Array.Empty<PricingLine>(), Now, 0, 0).IsValid);
}
