using PortalGamerX.Data;
using PortalGamerX.Domain;
using PortalGamerX.Domain.Entities;
using PortalGamerX.Domain.Rules;
using PortalGamerX.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace PortalGamerX.Tests.Integration;

public class StockConcurrencyTests
{
    private static readonly IKeyVault Vault = new KeyVault(new EphemeralDataProtectionProvider());

    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(PortalGamerX.Data.ConnectionStringResolver.FromUrl(PostgresFactAttribute.ConnectionString!)).Options);

    private static StockService NewStock(AppDbContext db) => new(db, Vault, NullLogger<StockService>.Instance);

    private static async Task<int> SetupJogoAsync(int keys)
    {
        await using var setup = NewDb();
        await setup.Database.EnsureDeletedAsync();
        await setup.Database.EnsureCreatedAsync();
        var cat = new Category { Name = "Teste", Slug = "teste" };
        setup.Categories.Add(cat);
        await setup.SaveChangesAsync();
        var p = new Jogo { Name = "Item", Slug = "item", Sku = "T-1", Price = 10m, CategoryId = cat.Id };
        setup.Jogos.Add(p);
        await setup.SaveChangesAsync();
        if (keys > 0)
        {
            var text = string.Join("\n", Enumerable.Range(1, keys).Select(i => $"KEY-{i:D4}-TESTE"));
            var r = await NewStock(setup).ImportKeysAsync(p.Id, text, "teste", null, "teste");
            Assert.True(r.Success);
            Assert.Equal(keys, r.Imported);
        }
        return p.Id;
    }

    [PostgresFact]
    public async Task Reservas_concorrentes_nunca_vendem_mais_keys_que_o_estoque()
    {
        var jogoId = await SetupJogoAsync(5);

        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(async _ =>
        {
            await using var db = NewDb();
            await using var tx = await db.Database.BeginTransactionAsync();
            var ok = await NewStock(db).TryReserveAsync(jogoId, 1, Guid.NewGuid(), "teste");
            if (ok) await tx.CommitAsync(); else await tx.RollbackAsync();
            return ok;
        }));

        Assert.Equal(5, results.Count(r => r));
        await using var check = NewDb();
        Assert.Equal(0, await check.Jogos.Where(p => p.Id == jogoId).Select(p => p.StockQuantity).FirstAsync());
        Assert.Equal(5, await check.GameKeys.CountAsync(k => k.JogoId == jogoId && k.Status == GameKeyStatus.Reserved));
        // cada key foi reservada para um único pedido
        Assert.Equal(5, await check.GameKeys.Where(k => k.Status == GameKeyStatus.Reserved).Select(k => k.OrderId).Distinct().CountAsync());
    }

    [PostgresFact]
    public async Task Ciclo_reservar_vender_e_entregar_keys_decifradas()
    {
        var jogoId = await SetupJogoAsync(3);
        var orderId = Guid.NewGuid();

        await using (var db = NewDb())
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            Assert.True(await NewStock(db).TryReserveAsync(jogoId, 2, orderId, "teste"));
            await tx.CommitAsync();
        }

        await using var db2 = NewDb();
        var stock = NewStock(db2);
        Assert.Equal(2, await stock.MarkSoldAsync(orderId));
        Assert.Equal(2, await stock.MarkSoldAsync(orderId)); // idempotente

        var keys = await stock.GetDeliveredKeysAsync(orderId, includeCode: true);
        Assert.Equal(2, keys.Count);
        Assert.All(keys, k => Assert.StartsWith("KEY-", k.Code));

        var masked = await stock.GetDeliveredKeysAsync(orderId, includeCode: false);
        Assert.All(masked, k => Assert.Null(k.Code));

        // keys no banco estão cifradas
        Assert.DoesNotContain(await db2.GameKeys.Select(k => k.CodeEncrypted).ToListAsync(), c => c.Contains("KEY-"));
        Assert.Equal(1, await db2.Jogos.Where(p => p.Id == jogoId).Select(p => p.StockQuantity).FirstAsync());
    }

    [PostgresFact]
    public async Task Liberar_pedido_devolve_keys_reservadas_ao_estoque()
    {
        var jogoId = await SetupJogoAsync(2);
        var orderId = Guid.NewGuid();
        await using var db = NewDb();
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            Assert.True(await NewStock(db).TryReserveAsync(jogoId, 2, orderId, "teste"));
            await tx.CommitAsync();
        }
        await NewStock(db).ReleaseAsync(jogoId, orderId, "cancelado");
        Assert.Equal(2, await db.Jogos.Where(p => p.Id == jogoId).Select(p => p.StockQuantity).FirstAsync());
        Assert.Equal(2, await db.GameKeys.CountAsync(k => k.JogoId == jogoId && k.Status == GameKeyStatus.Available && k.OrderId == null));
    }

    [PostgresFact]
    public async Task Importacao_ignora_duplicadas_e_invalidas()
    {
        var jogoId = await SetupJogoAsync(0);
        await using var db = NewDb();
        var stock = NewStock(db);
        var r1 = await stock.ImportKeysAsync(jogoId, "AAAA-1111\nBBBB-2222\nCC\naaaa1111", null, null, "t");
        Assert.True(r1.Success);
        Assert.Equal(2, r1.Imported);
        Assert.Equal(1, r1.Invalid);
        Assert.Equal(1, r1.RepeatedInText);

        var r2 = await stock.ImportKeysAsync(jogoId, "AAAA-1111\nCCCC-3333", null, null, "t");
        Assert.Equal(1, r2.Imported);
        Assert.Equal(1, r2.Duplicates);
        Assert.Equal(3, r2.NewBalance);

        var r3 = await stock.ImportKeysAsync(jogoId, "AAAA-1111", null, null, "t");
        Assert.False(r3.Success);
    }

    [PostgresFact]
    public async Task Invalidar_key_disponivel_reduz_o_saldo()
    {
        var jogoId = await SetupJogoAsync(2);
        await using var db = NewDb();
        var keyId = await db.GameKeys.Select(k => k.Id).FirstAsync();
        var r = await NewStock(db).VoidKeyAsync(keyId, "defeituosa", null, "t");
        Assert.True(r.Success);
        Assert.Equal(1, r.NewBalance);
        Assert.False((await NewStock(db).VoidKeyAsync(keyId, "de novo", null, "t")).Success);
        Assert.False((await NewStock(db).VoidKeyAsync(keyId, "", null, "t")).Success);
    }

    [PostgresFact]
    public async Task Constraint_impede_estoque_negativo()
    {
        await using var db = NewDb();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        var cat = new Category { Name = "Teste", Slug = "teste" };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();
        db.Jogos.Add(new Jogo { Name = "Neg", Slug = "neg", Sku = "N-1", Price = 10m, StockQuantity = -1, CategoryId = cat.Id });
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
