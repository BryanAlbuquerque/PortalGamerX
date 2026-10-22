using EcommerceBase.Web.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace EcommerceBase.Tests.Infrastructure;

public class ConnectionStringTests
{
    [Fact]
    public void DatabaseUrl_do_Render_e_convertida()
    {
        var cs = ConnectionStringResolver.FromUrl("postgres://usuario:p%40ss%3Aword@db.exemplo.com:5433/minhaloja");
        var b = new NpgsqlConnectionStringBuilder(cs);
        Assert.Equal("db.exemplo.com", b.Host);
        Assert.Equal(5433, b.Port);
        Assert.Equal("minhaloja", b.Database);
        Assert.Equal("usuario", b.Username);
        Assert.Equal("p@ss:word", b.Password);
    }

    [Fact]
    public void Porta_padrao_5432()
    {
        var b = new NpgsqlConnectionStringBuilder(ConnectionStringResolver.FromUrl("postgresql://u:p@host/db"));
        Assert.Equal(5432, b.Port);
    }

    [Fact]
    public void SslMode_configuravel()
    {
        var b = new NpgsqlConnectionStringBuilder(ConnectionStringResolver.FromUrl("postgres://u:p@host/db", "Require"));
        Assert.Equal(SslMode.Require, b.SslMode);
    }

    [Fact]
    public void DatabaseUrl_tem_prioridade_sobre_ConnectionStrings()
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DATABASE_URL"] = "postgres://u:p@urlhost/urldb",
            ["ConnectionStrings:Default"] = "Host=outro;Database=outro;Username=x;Password=y"
        }).Build();
        Assert.Equal("urlhost", new NpgsqlConnectionStringBuilder(ConnectionStringResolver.Resolve(cfg)).Host);
    }

    [Fact]
    public void Sem_configuracao_lanca_erro_claro()
    {
        var cfg = new ConfigurationBuilder().Build();
        Assert.Throws<InvalidOperationException>(() => ConnectionStringResolver.Resolve(cfg));
    }
}
