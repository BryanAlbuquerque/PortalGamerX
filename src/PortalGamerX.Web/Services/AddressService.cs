using PortalGamerX.Data;
using PortalGamerX.Domain.Entities;
using PortalGamerX.Domain.Rules;
using PortalGamerX.Infrastructure;
using PortalGamerX.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace PortalGamerX.Services;

public interface IAddressService
{
    Task<List<Address>> ListAsync(Guid userId);
    Task<Address?> GetAsync(Guid userId, int id);
    Task<OperationResult> SaveAsync(Guid userId, AddressInput input);
    Task<OperationResult> DeleteAsync(Guid userId, int id);
    Task<OperationResult> SetDefaultAsync(Guid userId, int id);
}

/// <summary>Todas as operações validam a PROPRIEDADE do endereço pelo userId autenticado (nunca confia no ID da URL).</summary>
public class AddressService : IAddressService
{
    private static readonly HashSet<string> Ufs = new(StringComparer.OrdinalIgnoreCase)
    {
        "AC","AL","AP","AM","BA","CE","DF","ES","GO","MA","MT","MS","MG","PA","PB","PR","PE","PI","RJ","RN","RS","RO","RR","SC","SP","SE","TO"
    };

    private readonly AppDbContext _db;
    public AddressService(AppDbContext db) => _db = db;

    public Task<List<Address>> ListAsync(Guid userId) =>
        _db.Addresses.AsNoTracking().Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault).ThenBy(a => a.Id).ToListAsync();

    public Task<Address?> GetAsync(Guid userId, int id) =>
        _db.Addresses.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

    public async Task<OperationResult> SaveAsync(Guid userId, AddressInput input)
    {
        if (!Documents.IsValidPostalCode(input.PostalCode)) return OperationResult.Fail("CEP inválido.");
        if (!Ufs.Contains(input.State.Trim())) return OperationResult.Fail("UF inválida.");

        Address? address;
        if (input.Id.HasValue)
        {
            address = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == input.Id.Value && a.UserId == userId);
            if (address == null) return OperationResult.Fail("Endereço não encontrado.");
        }
        else
        {
            if (await _db.Addresses.CountAsync(a => a.UserId == userId) >= 10)
                return OperationResult.Fail("Limite de 10 endereços atingido.");
            address = new Address { UserId = userId };
            _db.Addresses.Add(address);
        }

        address.Label = input.Label.Trim();
        address.RecipientName = input.RecipientName.Trim();
        address.Phone = input.Phone.NullIfEmpty();
        address.PostalCode = Documents.OnlyDigits(input.PostalCode);
        address.Street = input.Street.Trim();
        address.Number = input.Number.Trim();
        address.Complement = input.Complement.NullIfEmpty();
        address.District = input.District.Trim();
        address.City = input.City.Trim();
        address.State = input.State.Trim().ToUpperInvariant();

        var hasOther = await _db.Addresses.AnyAsync(a => a.UserId == userId && (input.Id == null || a.Id != input.Id.Value));
        if (input.IsDefault || !hasOther)
        {
            var others = await _db.Addresses.Where(a => a.UserId == userId && a.IsDefault && (input.Id == null || a.Id != input.Id.Value)).ToListAsync();
            foreach (var o in others) o.IsDefault = false;
            address.IsDefault = true;
        }

        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeleteAsync(Guid userId, int id)
    {
        var address = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (address == null) return OperationResult.Fail("Endereço não encontrado.");
        var wasDefault = address.IsDefault;
        _db.Addresses.Remove(address);
        await _db.SaveChangesAsync();
        if (wasDefault)
        {
            var next = await _db.Addresses.Where(a => a.UserId == userId).OrderBy(a => a.Id).FirstOrDefaultAsync();
            if (next != null) { next.IsDefault = true; await _db.SaveChangesAsync(); }
        }
        return OperationResult.Ok();
    }

    public async Task<OperationResult> SetDefaultAsync(Guid userId, int id)
    {
        var all = await _db.Addresses.Where(a => a.UserId == userId).ToListAsync();
        var target = all.FirstOrDefault(a => a.Id == id);
        if (target == null) return OperationResult.Fail("Endereço não encontrado.");
        foreach (var a in all) a.IsDefault = a.Id == id;
        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }
}
