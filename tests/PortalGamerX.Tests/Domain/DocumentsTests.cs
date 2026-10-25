using PortalGamerX.Domain.Rules;

namespace PortalGamerX.Tests.Domain;

public class DocumentsTests
{
    [Theory]
    [InlineData("529.982.247-25", true)]
    [InlineData("52998224725", true)]
    [InlineData("111.111.111-11", false)]
    [InlineData("529.982.247-24", false)]
    [InlineData("123", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Cpf(string? input, bool expected) => Assert.Equal(expected, Documents.IsValidCpf(input));

    [Theory]
    [InlineData("11.222.333/0001-81", true)]
    [InlineData("11222333000181", true)]
    [InlineData("11.222.333/0001-82", false)]
    [InlineData("00000000000000", false)]
    [InlineData("", false)]
    public void Cnpj(string? input, bool expected) => Assert.Equal(expected, Documents.IsValidCnpj(input));

    [Fact]
    public void CpfOuCnpj()
    {
        Assert.True(Documents.IsValidCpfOrCnpj("52998224725"));
        Assert.True(Documents.IsValidCpfOrCnpj("11222333000181"));
        Assert.False(Documents.IsValidCpfOrCnpj("12345"));
    }

    [Theory]
    [InlineData("01310-100", true)]
    [InlineData("01310100", true)]
    [InlineData("0131010", false)]
    public void Cep(string input, bool expected) => Assert.Equal(expected, Documents.IsValidPostalCode(input));

    [Fact]
    public void FormatPostalCode() => Assert.Equal("01310-100", Documents.FormatPostalCode("01310100"));

    [Fact]
    public void OnlyDigits() => Assert.Equal("12345", Documents.OnlyDigits("1.2-3/4 5"));

    [Theory]
    [InlineData("Camiseta Básica Preta", "camiseta-basica-preta")]
    [InlineData("  Ação & Reação!! ", "acao-reacao")]
    [InlineData("", "item")]
    [InlineData("###", "item")]
    public void Slug(string input, string expected) => Assert.Equal(expected, SlugHelper.Generate(input));

    [Fact]
    public void NumeroDoPedido_tem_formato_e_nao_repete()
    {
        var now = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc);
        var a = OrderNumberGenerator.Generate(now);
        var b = OrderNumberGenerator.Generate(now);
        Assert.Matches(@"^EB-260309-[A-Z2-9]{6}$", a);
        Assert.NotEqual(a, b);
    }
}
