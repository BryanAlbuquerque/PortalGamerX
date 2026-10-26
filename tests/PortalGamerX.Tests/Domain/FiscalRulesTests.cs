using PortalGamerX.Domain;
using PortalGamerX.Domain.Entities;
using PortalGamerX.Domain.Rules;

namespace PortalGamerX.Tests.Domain;

public class FiscalRulesTests
{
    private static readonly FiscalServiceParams Ok = new("01.05", "0105", "6209100", 2m);

    private static StoreSettings CompleteStore() => new()
    {
        Cnpj = "11222333000181", MunicipalRegistration = "123456", City = "São Paulo", State = "SP", PostalCode = "01001000"
    };

    [Theory]
    [InlineData("01.05", true)]
    [InlineData("1.05", false)]
    [InlineData("0105", false)]
    [InlineData("01.5", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidServiceCode(string? code, bool expected) => Assert.Equal(expected, FiscalRules.IsValidServiceCode(code));

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("6209100", true)]
    [InlineData("6209-1/00", true)]
    [InlineData("62091", false)]
    public void IsValidCnae(string? cnae, bool expected) => Assert.Equal(expected, FiscalRules.IsValidCnae(cnae));

    [Theory]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(5, true)]
    [InlineData(5.01, false)]
    [InlineData(-1, false)]
    public void IsValidIssRate(double rate, bool expected) => Assert.Equal(expected, FiscalRules.IsValidIssRate((decimal)rate));

    [Fact]
    public void IssAmount_arredonda_em_centavos()
    {
        Assert.Equal(2.00m, FiscalRules.IssAmount(100m, 2m));
        Assert.Equal(1.67m, FiscalRules.IssAmount(83.33m, 2m));
        Assert.Equal(0m, FiscalRules.IssAmount(-10m, 5m));
    }

    [Fact]
    public void ItemDescription_informa_licenca_plataforma_e_loja()
    {
        var d = FiscalRules.ItemDescription("Jogo X", GamePlatform.PlayStation5, ActivationStore.PlayStationStore);
        Assert.Contains("Licença de uso", d);
        Assert.Contains("Jogo X", d);
        Assert.Contains("PlayStation 5", d);
        Assert.Contains("PlayStation Store", d);
        Assert.DoesNotContain("Outra", FiscalRules.ItemDescription("Jogo X", GamePlatform.Outro, ActivationStore.Steam));
    }

    [Fact]
    public void CheckIssuer_ApontaPendencias()
    {
        var vazio = FiscalRules.CheckIssuer(new StoreSettings(), Ok);
        Assert.Equal(5, vazio.Count); // CNPJ, IM, cidade, UF e CEP
        Assert.Empty(FiscalRules.CheckIssuer(CompleteStore(), Ok));
    }

    [Fact]
    public void CheckIssuer_valida_parametros_do_servico()
    {
        var p = FiscalRules.CheckIssuer(CompleteStore(), new FiscalServiceParams("105", null, "12", 9m));
        Assert.Equal(3, p.Count);
    }

    [Fact]
    public void CheckCustomer_exige_cidade_uf_e_cep()
    {
        Assert.NotEmpty(FiscalRules.CheckCustomer(new Order()));
        Assert.Empty(FiscalRules.CheckCustomer(new Order { BillingCity = "Rio de Janeiro", BillingState = "RJ", BillingPostalCode = "20040020" }));
        Assert.Contains(FiscalRules.CheckCustomer(new Order
        {
            BillingCity = "Rio", BillingState = "RJ", BillingPostalCode = "20040020", CustomerDocument = "123"
        }), x => x.Contains("CPF/CNPJ"));
    }
}
