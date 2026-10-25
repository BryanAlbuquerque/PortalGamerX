using PortalGamerX.Data;
using PortalGamerX.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace PortalGamerX.Services;

public interface ICouponService
{
    Task<CouponEvaluation> EvaluateAsync(string? code, IReadOnlyList<PricingLine> lines, Guid? userId);
    Task<int?> FindCouponIdAsync(string code);
}

public class CouponService : ICouponService
{
    private readonly AppDbContext _db;
    public CouponService(AppDbContext db) => _db = db;

    public static string Normalize(string code) => code.Trim().ToUpperInvariant();

    public async Task<int?> FindCouponIdAsync(string code)
    {
        var normalized = Normalize(code);
        return await _db.Coupons.Where(c => c.Code == normalized).Select(c => (int?)c.Id).FirstOrDefaultAsync();
    }

    public async Task<CouponEvaluation> EvaluateAsync(string? code, IReadOnlyList<PricingLine> lines, Guid? userId)
    {
        if (string.IsNullOrWhiteSpace(code)) return CouponEvaluation.Fail("Informe o código do cupom.");
        var normalized = Normalize(code);

        var coupon = await _db.Coupons
            .AsNoTracking()
            .Include(c => c.Categories)
            .Include(c => c.Jogos)
            .FirstOrDefaultAsync(c => c.Code == normalized);
        if (coupon == null) return CouponEvaluation.Fail("Cupom inválido.");

        var totalUses = await _db.CouponUsages.CountAsync(u => u.CouponId == coupon.Id);
        var userUses = userId.HasValue
            ? await _db.CouponUsages.CountAsync(u => u.CouponId == coupon.Id && u.UserId == userId.Value)
            : 0;

        return CouponRules.Evaluate(coupon, lines, DateTime.UtcNow, totalUses, userUses);
    }
}
