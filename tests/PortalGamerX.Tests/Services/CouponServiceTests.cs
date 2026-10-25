using PortalGamerX.Data;
using PortalGamerX.Domain;
using PortalGamerX.Domain.Entities;
using PortalGamerX.Domain.Rules;
using PortalGamerX.Services;
using Microsoft.EntityFrameworkCore;

namespace PortalGamerX.Tests.Services;

public class CouponServiceTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static readonly PricingLine[] Lines = { new(1, 1, "S1", 100m, 1) };

    [Fact]
    public async Task Codigo_e_normalizado_sem_diferenciar_maiusculas()
    {
        await using var db = NewDb();
        db.Coupons.Add(new Coupon { Code = "BEMVINDO10", DiscountType = DiscountType.Percentage, Value = 10m });
        await db.SaveChangesAsync();

        var r = await new CouponService(db).EvaluateAsync("  bemvindo10 ", Lines, null);
        Assert.True(r.IsValid);
        Assert.Equal(10m, r.Discount);
    }

    [Fact]
    public async Task Cupom_inexistente_ou_vazio_e_rejeitado()
    {
        await using var db = NewDb();
        var svc = new CouponService(db);
        Assert.False((await svc.EvaluateAsync("NAOEXISTE", Lines, null)).IsValid);
        Assert.False((await svc.EvaluateAsync("", Lines, null)).IsValid);
        Assert.False((await svc.EvaluateAsync(null, Lines, null)).IsValid);
    }

    [Fact]
    public async Task Limite_por_cliente_considera_os_usos_registrados()
    {
        await using var db = NewDb();
        var user = Guid.NewGuid();
        var coupon = new Coupon { Code = "UMA", DiscountType = DiscountType.Percentage, Value = 10m, MaxUsesPerCustomer = 1 };
        db.Coupons.Add(coupon);
        await db.SaveChangesAsync();
        db.CouponUsages.Add(new CouponUsage { CouponId = coupon.Id, UserId = user, OrderId = Guid.NewGuid(), DiscountAmount = 10m });
        await db.SaveChangesAsync();

        var svc = new CouponService(db);
        Assert.False((await svc.EvaluateAsync("UMA", Lines, user)).IsValid);
        Assert.True((await svc.EvaluateAsync("UMA", Lines, Guid.NewGuid())).IsValid);
    }
}
