using EcommerceBase.Web.Domain.Rules;

namespace EcommerceBase.Tests.Domain;

public class MoneyTests
{
    [Theory]
    [InlineData(1234.5, "R$ 1.234,50")]
    [InlineData(0, "R$ 0,00")]
    [InlineData(1000000.99, "R$ 1.000.000,99")]
    [InlineData(5, "R$ 5,00")]
    public void ToBrl_formata_no_padrao_brasileiro(double value, string expected) =>
        Assert.Equal(expected, ((decimal)value).ToBrl());

    [Fact]
    public void Round_arredonda_meio_para_cima() => Assert.Equal(1.01m, Money.Round(1.005m));

    [Fact]
    public void ToBrl_nulo_retorna_traco() => Assert.Equal("-", ((decimal?)null).ToBrl());
}
