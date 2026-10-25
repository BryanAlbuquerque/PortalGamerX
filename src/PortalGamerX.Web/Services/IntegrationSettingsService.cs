using PortalGamerX.Data;
using PortalGamerX.Domain.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace PortalGamerX.Services;

public interface IIntegrationSettings
{
    Task<string?> GetAsync(string key);
    Task<string> GetOrDefaultAsync(string key, string defaultValue);
    Task<bool> GetBoolAsync(string key, bool defaultValue = false);
    Task<bool> HasValueAsync(string key);
    /// <summary>Define o valor. Para chaves secretas, valor vazio/nulo MANTÉM o valor atual.</summary>
    Task SetAsync(string key, string? value, string? updatedBy);
    Task SetBoolAsync(string key, bool value, string? updatedBy);
}

public class IntegrationSettingsService : IIntegrationSettings
{
    private const string CacheKey = "integration-settings";
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IDataProtector _protector;

    public IntegrationSettingsService(AppDbContext db, IMemoryCache cache, IDataProtectionProvider dp)
    {
        _db = db;
        _cache = cache;
        // NÃO renomear: este "purpose" faz parte da chave de criptografia; alterá-lo torna ilegíveis as configurações já salvas no banco.
        _protector = dp.CreateProtector("EcommerceBase.IntegrationSettings.v1");
    }

    private async Task<Dictionary<string, string?>> LoadAsync()
    {
        if (_cache.TryGetValue(CacheKey, out Dictionary<string, string?>? cached) && cached != null) return cached;

        var rows = await _db.IntegrationSettings.AsNoTracking().ToListAsync();
        var dict = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var r in rows)
        {
            if (r.IsSecret && !string.IsNullOrEmpty(r.Value))
            {
                try { dict[r.Key] = _protector.Unprotect(r.Value); }
                catch { dict[r.Key] = null; } // chave de proteção diferente/perdida: trata como não configurado
            }
            else dict[r.Key] = r.Value;
        }
        _cache.Set(CacheKey, dict, TimeSpan.FromMinutes(5));
        return dict;
    }

    public async Task<string?> GetAsync(string key)
    {
        var d = await LoadAsync();
        return d.TryGetValue(key, out var v) ? v : null;
    }

    public async Task<string> GetOrDefaultAsync(string key, string defaultValue)
    {
        var v = await GetAsync(key);
        return string.IsNullOrWhiteSpace(v) ? defaultValue : v;
    }

    public async Task<bool> GetBoolAsync(string key, bool defaultValue = false)
    {
        var v = await GetAsync(key);
        return bool.TryParse(v, out var b) ? b : defaultValue;
    }

    public async Task<bool> HasValueAsync(string key) => !string.IsNullOrWhiteSpace(await GetAsync(key));

    public async Task SetAsync(string key, string? value, string? updatedBy)
    {
        var isSecret = SettingKeys.SecretKeys.Contains(key);
        if (isSecret && string.IsNullOrWhiteSpace(value)) return; // mantém o segredo atual

        var row = await _db.IntegrationSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (row == null)
        {
            row = new IntegrationSetting { Key = key };
            _db.IntegrationSettings.Add(row);
        }
        var clean = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        row.IsSecret = isSecret;
        row.Value = isSecret && clean != null ? _protector.Protect(clean) : clean;
        row.UpdatedAtUtc = DateTime.UtcNow;
        row.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync();
        _cache.Remove(CacheKey);
    }

    public Task SetBoolAsync(string key, bool value, string? updatedBy) =>
        SetAsync(key, value ? "true" : "false", updatedBy);
}
