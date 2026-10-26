using PortalGamerX.Domain.Rules;

namespace PortalGamerX.Tests.Domain;

public class KeyRulesTests
{
    [Theory]
    [InlineData("ABCDE-12345-FGHIJ", true)]
    [InlineData("  ABCD-1234  ", true)]
    [InlineData("AB", false)]
    [InlineData("ABCD 1234", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid(string? code, bool expected) => Assert.Equal(expected, KeyRules.IsValid(code));

    [Fact]
    public void Tamanho_maximo()
    {
        Assert.True(KeyRules.IsValid(new string('A', KeyRules.MaxLength)));
        Assert.False(KeyRules.IsValid(new string('A', KeyRules.MaxLength + 1)));
    }

    [Fact]
    public void Hash_ignora_caixa_hifens_e_espacos_nas_pontas()
    {
        Assert.Equal(KeyRules.Hash("ABCD-1234-WXYZ"), KeyRules.Hash(" abcd1234wxyz "));
        Assert.NotEqual(KeyRules.Hash("ABCD-1234-WXYZ"), KeyRules.Hash("ABCD-1234-WXYA"));
        Assert.Equal(64, KeyRules.Hash("ABCD-1234").Length);
    }

    [Fact]
    public void Hint_e_Mask_nao_expoem_a_key()
    {
        Assert.Equal("WXYZ", KeyRules.Hint("ABCD-1234-WXYZ"));
        Assert.DoesNotContain("ABCD", KeyRules.Mask(KeyRules.Hint("ABCD-1234-WXYZ")));
        Assert.EndsWith("WXYZ", KeyRules.Mask("WXYZ"));
    }

    [Fact]
    public void Parse_separa_valida_e_remove_repetidas()
    {
        var (codes, invalid, repeated) = KeyRules.Parse("AAAA-1111\nBBBB-2222\r\n\n  aaaa1111 \nCC\nDDDD-3333;EEEE-4444,FFFF 5555");
        Assert.Equal(new[] { "AAAA-1111", "BBBB-2222", "DDDD-3333", "EEEE-4444" }, codes);
        Assert.Equal(2, invalid);    // "CC" e "FFFF 5555"
        Assert.Equal(1, repeated);   // "aaaa1111"
    }

    [Fact]
    public void Parse_texto_vazio()
    {
        var (codes, invalid, repeated) = KeyRules.Parse(null);
        Assert.Empty(codes);
        Assert.Equal(0, invalid);
        Assert.Equal(0, repeated);
    }
}
