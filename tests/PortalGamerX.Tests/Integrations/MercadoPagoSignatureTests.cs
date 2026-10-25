using PortalGamerX.Domain;
using PortalGamerX.Integrations.Payments;

namespace PortalGamerX.Tests.Integrations;

public class MercadoPagoSignatureTests
{
    private const string Secret = "segredo-de-teste";

    private static string Header(string dataId, string requestId, string ts, string secret = Secret)
    {
        var hex = MercadoPagoSignature.ComputeHex(secret, MercadoPagoSignature.BuildManifest(dataId, requestId, ts));
        return $"ts={ts},v1={hex}";
    }

    [Fact]
    public void Manifesto_segue_o_formato_documentado() =>
        Assert.Equal("id:123456;request-id:abc-1;ts:1700000000;", MercadoPagoSignature.BuildManifest("123456", "abc-1", "1700000000"));

    [Fact]
    public void Manifesto_omite_campos_ausentes() =>
        Assert.Equal("ts:1700000000;", MercadoPagoSignature.BuildManifest(null, null, "1700000000"));

    [Fact]
    public void Id_alfanumerico_e_normalizado_para_minusculas() =>
        Assert.Equal("id:abc123;ts:1;", MercadoPagoSignature.BuildManifest("ABC123", null, "1"));

    [Fact]
    public void Assinatura_correta_e_aceita() =>
        Assert.True(MercadoPagoSignature.IsValid(Secret, Header("123456", "req-1", "1700000000"), "req-1", "123456"));

    [Fact]
    public void Segredo_errado_e_rejeitado() =>
        Assert.False(MercadoPagoSignature.IsValid(Secret, Header("123456", "req-1", "1700000000", "outro"), "req-1", "123456"));

    [Fact]
    public void Dado_adulterado_e_rejeitado()
    {
        var header = Header("123456", "req-1", "1700000000");
        Assert.False(MercadoPagoSignature.IsValid(Secret, header, "req-1", "999999"));
        Assert.False(MercadoPagoSignature.IsValid(Secret, header, "req-2", "123456"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("lixo")]
    [InlineData("ts=1")]
    [InlineData("v1=abc")]
    public void Cabecalho_invalido_e_rejeitado(string? header) =>
        Assert.False(MercadoPagoSignature.IsValid(Secret, header, "r", "1"));

    [Fact]
    public void Segredo_vazio_sempre_rejeita() =>
        Assert.False(MercadoPagoSignature.IsValid("", Header("1", "r", "1", ""), "r", "1"));

    [Theory]
    [InlineData("approved", PaymentStatus.Approved)]
    [InlineData("APPROVED", PaymentStatus.Approved)]
    [InlineData("pending", PaymentStatus.Pending)]
    [InlineData("in_process", PaymentStatus.InProcess)]
    [InlineData("rejected", PaymentStatus.Rejected)]
    [InlineData("cancelled", PaymentStatus.Cancelled)]
    [InlineData("refunded", PaymentStatus.Refunded)]
    [InlineData("charged_back", PaymentStatus.ChargedBack)]
    [InlineData("algo-novo", PaymentStatus.Pending)]
    [InlineData(null, PaymentStatus.Pending)]
    public void Mapeamento_de_status(string? raw, PaymentStatus expected) =>
        Assert.Equal(expected, MercadoPagoProvider.MapStatus(raw));
}
