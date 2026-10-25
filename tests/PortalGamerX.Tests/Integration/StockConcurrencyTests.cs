using PortalGamerX.Data;
using PortalGamerX.Domain;
using PortalGamerX.Domain.Entities;
using PortalGamerX.Services;
using Microsoft.EntityFrameworkCore;

namespace PortalGamerX.Tests.Integration;

public class StockConcurrencyTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(PortalGamerX.Data.ConnectionStringResolver.FromUrl(PostgresFactAttribute.ConnectionString!)).Options);

    [PostgresFact]
    public async Task Reservas_concorrentes_nunca_vendem_mais_que_o_estoque()
    {
        int jogoId;
        await using (var setup = NewDb())
        {
            await setup.Database.EnsureDeletedAsync();
            await setup.Database.EnsureCreatedAsync();
            var cat = new Category { Name = "Teste", Slug = "teste" };
            setup.Categories.Add(cat);
            await setup.SaveChangesAsync();
            var p = new Jogo { Name = "Item", Slug = "item", Sku = "T-1", Price = 10m, StockQuantity = 5, CategoryId = cat.Id };
            setup.Jogos.Add(p);
            await setup.SaveChangesAsync();
            jogoId = p.Id;
        }

        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(async _ =>
        {
            await using var db = NewDb();
            return await new StockService(db).TryReserveAsync(jogoId, 1, Guid.NewGuid(), "teste");
        }));

        Assert.Equal(5, results.Count(r => r));
        await using var check = NewDb();
        Assert.Equal(0, await check.Jogos.Where(p => p.Id == jogoId).Select(p => p.StockQuantity).FirstAsync());
        Assert.Equal(5, await check.StockMovements.CountAsync(m => m.JogoId == jogoId && m.Type == StockMovementType.OrderReservation));
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
