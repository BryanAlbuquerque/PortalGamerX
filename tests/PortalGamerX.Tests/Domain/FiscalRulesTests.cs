using PortalGamerX.Domain;
using PortalGamerX.Domain.Entities;
using PortalGamerX.Domain.Rules;

namespace PortalGamerX.Tests.Domain;

public class FiscalRulesTests
{
    [Fact]
    public void DefaultNcm_DiscoUsa85234990()
        => Assert.Equal("85234990", FiscalRules.DefaultNcm(GameMediaType.Disc));

    [Fact]
    public void DefaultNcm_CartuchoUsa95045000()
        => Assert.Equal("95045000", FiscalRules.DefaultNcm(GameMediaType.Cartridge));

    [Theory]
    [InlineData("85234990", true)]
    [InlineData("9504500", false)]
    [InlineData("950450000", false)]
    [InlineData("9504500A", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidNcm(string? ncm, bool expected) => Assert.Equal(expected, FiscalRules.IsValidNcm(ncm));

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("2106400", true)]
    [InlineData("210640", false)]
    [InlineData("21064AB", false)]
    public void IsValidCest(string? cest, bool expected) => Assert.Equal(expected, FiscalRules.IsValidCest(cest));

    [Theory]
    [InlineData(0, true)]
    [InlineData(8, true)]
    [InlineData(-1, false)]
    [InlineData(9, false)]
    public void IsValidOrigin(int origin, bool expected) => Assert.Equal(expected, FiscalRules.IsValidOrigin(origin));

    [Theory]
    [InlineData("SP", "SP", "5102")]
    [InlineData("sp", "SP", "5102")]
    [InlineData("SP", "RJ", "6108")]
    [InlineData("MG", "BA", "6108")]
    public void Cfop_VendaOnlineAConsumidorFinal(string emitente, string destino, string esperado)
        => Assert.Equal(esperado, FiscalRules.Cfop(emitente, destino));

    [Theory]
    [InlineData(null, "SP")]
    [InlineData("SP", null)]
    [InlineData("", "SP")]
    public void Cfop_SemUf_RetornaNulo(string? emitente, string? destino)
        => Assert.Null(FiscalRules.Cfop(emitente, destino));

    [Fact]
    public void ItemDescription_IncluiPlataforma()
    {
        Assert.Equal("Jogo X - PlayStation 5", FiscalRules.ItemDescription("Jogo X", GamePlatform.PlayStation5));
        Assert.Equal("Jogo X", FiscalRules.ItemDescription("Jogo X", GamePlatform.Outro));
    }

    [Fact]
    public void Jogo_EffectiveNcm_UsaPadraoQuandoVazio()
    {
        var disco = new Jogo { MediaType = GameMediaType.Disc };
        var cartucho = new Jogo { MediaType = GameMediaType.Cartridge };
        var manual = new Jogo { MediaType = GameMediaType.Disc, Ncm = "95045000" };
        Assert.Equal("85234990", disco.EffectiveNcm);
        Assert.Equal("95045000", cartucho.EffectiveNcm);
        Assert.Equal("95045000", manual.EffectiveNcm);
    }

    [Fact]
    public void CheckIssuer_ApontaPendencias()
    {
        var vazio = FiscalRules.CheckIssuer(new StoreSettings());
        Assert.Equal(4, vazio.Count);

        var completo = FiscalRules.CheckIssuer(new StoreSettings
        {
            Cnpj = "11222333000181", StateRegistration = "123456789", State = "SP", PostalCode = "01001000"
        });
        Assert.Empty(completo);
    }

    [Fact]
    public void CheckItem_NcmInvalido_GeraPendencia()
    {
        var g = new Jogo { Name = "Jogo", Ncm = "123" };
        Assert.Single(FiscalRules.CheckItem(g), p => p.Contains("NCM"));
        Assert.Empty(FiscalRules.CheckItem(new Jogo { Name = "Jogo" }));
    }
}
