using PortalGamerX.Domain;
using PortalGamerX.Services;

namespace PortalGamerX.Tests.Services;

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
    public void Variaveis_terminadas_em_Html_sao_inseridas_como_estao()
    {
        var html = EmailTemplates.Render("{{KeysHtml}}|{{Nome}}", new Dictionary<string, string> { ["KeysHtml"] = "<ul><li>ok</li></ul>", ["Nome"] = "<b>x</b>" });
        Assert.Contains("<ul><li>ok</li></ul>", html);
        Assert.Contains("&lt;b&gt;", html);
    }

    [Fact]
    public void BuildKeysHtml_codifica_nome_key_e_instrucoes()
    {
        var html = OrderService.BuildKeysHtml(new[]
        {
            new DeliveredKey(1, 7, "Jogo <b>X</b>", ActivationStore.Steam, KeyRegion.Global, "Abra o Steam.\nCole a key <i>aqui</i>", "AAAA-<1>-BBBB")
        });
        Assert.DoesNotContain("<b>X</b>", html);
        Assert.DoesNotContain("<i>", html);
        Assert.Contains("AAAA-&lt;1&gt;-BBBB", html);
        Assert.Contains("Steam", html);
        Assert.Contains("<br />", html);
    }

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

    [Fact]
    public void Entrega_das_keys_e_obrigatoria()
    {
        Assert.Contains(EmailKinds.KeysDelivered, EmailKinds.All);
        Assert.DoesNotContain(EmailKinds.KeysDelivered, EmailKinds.Optional);
    }
}
