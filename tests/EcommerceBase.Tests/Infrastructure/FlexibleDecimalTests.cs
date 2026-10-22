using EcommerceBase.Web.Infrastructure;

namespace EcommerceBase.Tests.Infrastructure;

public class FlexibleDecimalTests
{
    [Theory]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("10,5", 10.5)]
    [InlineData("10.5", 10.5)]
    [InlineData("R$ 99,90", 99.90)]
    [InlineData("0", 0)]
    public void Aceita_virgula_e_ponto(string raw, double expected)
    {
        Assert.True(FlexibleDecimalModelBinder.TryParse(raw, out var v));
        Assert.Equal((decimal)expected, v);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData(null)]
    public void Rejeita_invalidos(string? raw) => Assert.False(FlexibleDecimalModelBinder.TryParse(raw, out _));
}
