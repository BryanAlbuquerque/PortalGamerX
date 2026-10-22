using EcommerceBase.Web.Data;
using EcommerceBase.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EcommerceBase.Web.Services;

public interface IStoreSettingsService
{
    /// <summary>Retorna uma cópia somente leitura (sem rastreamento). Se a tabela não existir ainda, retorna padrões.</summary>
    Task<StoreSettings> GetAsync();
    Task SaveAsync(Action<StoreSettings> apply);
    DateTime ToLocal(DateTime utc, StoreSettings settings);
    DateTime ToUtc(DateTime local, StoreSettings settings);
}

public class StoreSettingsService : IStoreSettingsService
{
    private const string CacheKey = "store-settings";
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<StoreSettingsService> _logger;

    public StoreSettingsService(AppDbContext db, IMemoryCache cache, ILogger<StoreSettingsService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<StoreSettings> GetAsync()
    {
        if (_cache.TryGetValue(CacheKey, out StoreSettings? cached) && cached != null) return cached;
        try
        {
            var s = await _db.StoreSettings.AsNoTracking().FirstOrDefaultAsync() ?? new StoreSettings();
            _cache.Set(CacheKey, s, TimeSpan.FromMinutes(5));
            return s;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Não foi possível carregar as configurações da loja; usando padrões.");
            return new StoreSettings();
        }
    }

    public async Task SaveAsync(Action<StoreSettings> apply)
    {
        var s = await _db.StoreSettings.FirstOrDefaultAsync();
        if (s == null)
        {
            s = new StoreSettings();
            _db.StoreSettings.Add(s);
        }
        apply(s);
        s.Id = 1;
        s.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        _cache.Remove(CacheKey);
    }

    private static TimeZoneInfo Resolve(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch { return TimeZoneInfo.CreateCustomTimeZone("BRT", TimeSpan.FromHours(-3), "BRT", "BRT"); }
    }

    public DateTime ToLocal(DateTime utc, StoreSettings settings) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Resolve(settings.TimeZoneId));

    public DateTime ToUtc(DateTime local, StoreSettings settings) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Resolve(settings.TimeZoneId));
}
