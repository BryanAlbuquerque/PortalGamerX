using EcommerceBase.Web.Services;

namespace EcommerceBase.Tests.Services;

public class EmailTemplateTests
{
    [Fact]
    public void Valores_sao_codificados_contra_injecao_de_html()
    {
        var html = EmailTemplates.Render("<p>Olá, {{CustomerName}}</p>", new Dictionary<string, string> { ["CustomerName"] = "<script>alert(1)</script>" });
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void Variaveis_desconhecidas_viram_vazio() =>
        Assert.Equal("a  b", EmailTemplates.Render("a {{Inexistente}} b", new Dictionary<string, string>()));

    [Fact]
    public void Todos_os_tipos_possuem_modelo_padrao()
    {
        foreach (var kind in EmailKinds.All)
        {
            var (subject, body) = EmailTemplates.Default(kind);
            Assert.False(string.IsNullOrWhiteSpace(subject), kind);
            Assert.False(string.IsNullOrWhiteSpace(body), kind);
        }
    }

    [Fact]
    public void Tipos_opcionais_sao_subconjunto_de_todos() =>
        Assert.All(EmailKinds.Optional, k => Assert.Contains(k, EmailKinds.All));
}
