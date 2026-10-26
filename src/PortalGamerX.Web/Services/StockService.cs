using PortalGamerX.Data;
using PortalGamerX.Domain;
using PortalGamerX.Domain.Entities;
using PortalGamerX.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace PortalGamerX.Services;

public record StockResult(bool Success, string? Error, int? NewBalance = null);

public record KeyImportResult(bool Success, string? Error, int Imported, int Duplicates, int Invalid, int RepeatedInText, int NewBalance)
{
    public static KeyImportResult Fail(string error) => new(false, error, 0, 0, 0, 0, 0);
}

/// <summary>Key entregue ao cliente, já decifrada, com os dados necessários para ativá-la.</summary>
public record DeliveredKey(long KeyId, int JogoId, string JogoName, ActivationStore Store, KeyRegion Region, string? Instructions, string? Code);

/// <summary>
/// Estoque de KEYS. O saldo do jogo (<see cref="Jogo.StockQuantity"/>) é o número de keys disponíveis e só muda junto com o
/// inventário (<see cref="GameKey"/>): importar (+), reservar (-), liberar (+) e invalidar uma key disponível (-).
/// </summary>
public interface IStockService
{
    /// <summary>
    /// Reserva <paramref name="quantity"/> keys para o pedido (Disponível → Reservada) de forma atômica.
    /// Deve rodar dentro da transação do pedido; se retornar false, o chamador faz rollback.
    /// </summary>
    Task<bool> TryReserveAsync(int jogoId, int quantity, Guid orderId, string reason);

    /// <summary>Devolve ao estoque as keys ainda RESERVADAS do pedido (pedido cancelado/expirado).</summary>
    Task ReleaseAsync(int jogoId, Guid orderId, string reason);

    /// <summary>Pagamento confirmado: Reservada → Vendida. Retorna quantas keys do pedido estão vendidas (idempotente).</summary>
    Task<int> MarkSoldAsync(Guid orderId);

    /// <summary>Pedido já entregue foi reembolsado/estornado: invalida as keys vendidas (não voltam ao estoque).</summary>
    Task<int> VoidSoldKeysAsync(Guid orderId, string reason);

    /// <summary>Keys vendidas do pedido, decifradas (só para o dono do pedido ou administrador).</summary>
    Task<List<DeliveredKey>> GetDeliveredKeysAsync(Guid orderId, bool includeCode);

    Task<KeyImportResult> ImportKeysAsync(int jogoId, string? text, string? note, Guid? userId, string? userName);
    Task<StockResult> VoidKeyAsync(long keyId, string reason, Guid? userId, string? userName);

    /// <summary>Recalcula o saldo do jogo a partir das keys disponíveis (correção de divergência).</summary>
    Task<int> RecalculateAsync(int jogoId);
}

public class StockService : IStockService
{
    private readonly AppDbContext _db;
    private readonly IKeyVault _vault;
    private readonly ILogger<StockService> _logger;

    public StockService(AppDbContext db, IKeyVault vault, ILogger<StockService> logger)
    {
        _db = db;
        _vault = vault;
        _logger = logger;
    }

    private Task<int> BalanceAsync(int jogoId) =>
        _db.Jogos.Where(p => p.Id == jogoId).Select(p => p.StockQuantity).FirstAsync();

    private Task AddMovementAsync(int jogoId, StockMovementType type, int delta, int balance, string? reason, Guid? orderId, Guid? userId, string? userName)
    {
        _db.StockMovements.Add(new StockMovement
        {
            JogoId = jogoId, Type = type, Quantity = delta, BalanceAfter = balance,
            Reason = reason, OrderId = orderId, UserId = userId, UserName = userName ?? "sistema"
        });
        return _db.SaveChangesAsync();
    }

    public async Task<bool> TryReserveAsync(int jogoId, int quantity, Guid orderId, string reason)
    {
        if (quantity <= 0) return false;

        // 1) Baixa atômica do saldo (impede vender mais do que há em estoque, mesmo com concorrência).
        var affected = await _db.Jogos
            .Where(p => p.Id == jogoId && p.IsActive && p.IsAvailable && p.StockQuantity >= quantity)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.StockQuantity, p => p.StockQuantity - quantity)
                .SetProperty(p => p.UpdatedAtUtc, DateTime.UtcNow));
        if (affected == 0) return false;

        // 2) Reserva as keys específicas (as mais antigas primeiro). SKIP LOCKED evita disputa por linhas bloqueadas.
        var now = DateTime.UtcNow;
        var available = (int)GameKeyStatus.Available;
        var reserved = (int)GameKeyStatus.Reserved;
        var count = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""GameKeys""
               SET ""Status"" = {reserved}, ""OrderId"" = {orderId}, ""ReservedAtUtc"" = {now}
             WHERE ""Id"" IN (SELECT ""Id"" FROM ""GameKeys""
                               WHERE ""JogoId"" = {jogoId} AND ""Status"" = {available}
                               ORDER BY ""Id"" LIMIT {quantity}
                               FOR UPDATE SKIP LOCKED)");
        if (count != quantity)
        {
            // Contador e inventário divergiram: o chamador desfaz a transação inteira.
            _logger.LogError("Divergência de estoque no jogo {Jogo}: saldo permitia {Qty} keys, mas só {Found} puderam ser reservadas.", jogoId, quantity, count);
            return false;
        }

        var balance = await BalanceAsync(jogoId);
        await AddMovementAsync(jogoId, StockMovementType.OrderReservation, -quantity, balance, reason, orderId, null, null);
        return true;
    }

    public async Task ReleaseAsync(int jogoId, Guid orderId, string reason)
    {
        var n = await _db.GameKeys
            .Where(k => k.OrderId == orderId && k.JogoId == jogoId && k.Status == GameKeyStatus.Reserved)
            .ExecuteUpdateAsync(s => s
                .SetProperty(k => k.Status, GameKeyStatus.Available)
                .SetProperty(k => k.OrderId, (Guid?)null)
                .SetProperty(k => k.ReservedAtUtc, (DateTime?)null));
        if (n == 0) return;

        await _db.Jogos.Where(p => p.Id == jogoId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.StockQuantity, p => p.StockQuantity + n)
                .SetProperty(p => p.UpdatedAtUtc, DateTime.UtcNow));
        var balance = await BalanceAsync(jogoId);
        await AddMovementAsync(jogoId, StockMovementType.OrderRelease, n, balance, reason, orderId, null, null);
    }

    public async Task<int> MarkSoldAsync(Guid orderId)
    {
        var now = DateTime.UtcNow;
        await _db.GameKeys
            .Where(k => k.OrderId == orderId && k.Status == GameKeyStatus.Reserved)
            .ExecuteUpdateAsync(s => s
                .SetProperty(k => k.Status, GameKeyStatus.Sold)
                .SetProperty(k => k.SoldAtUtc, (DateTime?)now));
        return await _db.GameKeys.CountAsync(k => k.OrderId == orderId && k.Status == GameKeyStatus.Sold);
    }

    public Task<int> VoidSoldKeysAsync(Guid orderId, string reason)
    {
        var note = reason.Length > 300 ? reason.Substring(0, 300) : reason; // fora da expression tree (sem operador de intervalo)
        return _db.GameKeys
            .Where(k => k.OrderId == orderId && k.Status == GameKeyStatus.Sold)
            .ExecuteUpdateAsync(s => s
                .SetProperty(k => k.Status, GameKeyStatus.Void)
                .SetProperty(k => k.Note, note));
    }

    public async Task<List<DeliveredKey>> GetDeliveredKeysAsync(Guid orderId, bool includeCode)
    {
        var rows = await _db.GameKeys.AsNoTracking()
            .Where(k => k.OrderId == orderId && k.Status == GameKeyStatus.Sold)
            .OrderBy(k => k.JogoId).ThenBy(k => k.Id)
            .Select(k => new
            {
                k.Id, k.JogoId, JogoName = k.Jogo!.Name, k.Jogo.ActivationStore, k.Jogo.Region, k.Jogo.ActivationInstructions, k.CodeEncrypted
            })
            .ToListAsync();

        return rows.Select(r => new DeliveredKey(
            r.Id, r.JogoId, r.JogoName, r.ActivationStore, r.Region, r.ActivationInstructions,
            includeCode ? (_vault.Unprotect(r.CodeEncrypted) ?? "[indisponível: contate o suporte]") : null)).ToList();
    }

    public async Task<KeyImportResult> ImportKeysAsync(int jogoId, string? text, string? note, Guid? userId, string? userName)
    {
        if (!await _db.Jogos.AnyAsync(p => p.Id == jogoId)) return KeyImportResult.Fail("Jogo não encontrado.");

        var (codes, invalid, repeated) = KeyRules.Parse(text);
        if (codes.Count == 0)
            return KeyImportResult.Fail(invalid > 0
                ? $"Nenhuma key válida encontrada ({invalid} linha(s) inválida(s): use 4 a {KeyRules.MaxLength} caracteres, sem espaços)."
                : "Cole ao menos uma key (uma por linha).");
        if (codes.Count > KeyRules.MaxPerImport)
            return KeyImportResult.Fail($"Limite de {KeyRules.MaxPerImport} keys por importação. Divida em lotes.");

        var hashes = codes.Select(KeyRules.Hash).ToList();
        var existing = (await _db.GameKeys.AsNoTracking().Where(k => hashes.Contains(k.CodeHash)).Select(k => k.CodeHash).ToListAsync()).ToHashSet();

        var cleanNote = string.IsNullOrWhiteSpace(note) ? null : (note.Trim().Length > 300 ? note.Trim()[..300] : note.Trim());
        var toAdd = new List<GameKey>();
        for (var i = 0; i < codes.Count; i++)
        {
            if (existing.Contains(hashes[i])) continue;
            toAdd.Add(new GameKey
            {
                JogoId = jogoId,
                CodeEncrypted = _vault.Protect(codes[i]),
                CodeHash = hashes[i],
                CodeHint = KeyRules.Hint(codes[i]),
                Status = GameKeyStatus.Available,
                Note = cleanNote,
                CreatedByName = userName
            });
        }
        var duplicates = codes.Count - toAdd.Count;
        if (toAdd.Count == 0)
            return new KeyImportResult(false, "Todas as keys informadas já estão cadastradas.", 0, duplicates, invalid, repeated, await BalanceAsync(jogoId));

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            _db.GameKeys.AddRange(toAdd);
            await _db.SaveChangesAsync();

            await _db.Jogos.Where(p => p.Id == jogoId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.StockQuantity, p => p.StockQuantity + toAdd.Count)
                    .SetProperty(p => p.UpdatedAtUtc, DateTime.UtcNow));
            var balance = await BalanceAsync(jogoId);
            await AddMovementAsync(jogoId, StockMovementType.Entry, toAdd.Count, balance,
                cleanNote == null ? "Importação de keys" : "Importação de keys: " + cleanNote, null, userId, userName);
            await tx.CommitAsync();
            return new KeyImportResult(true, null, toAdd.Count, duplicates, invalid, repeated, balance);
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync();
            _db.ChangeTracker.Clear();
            _logger.LogWarning(ex, "Falha ao importar keys do jogo {Jogo} (provável duplicidade concorrente).", jogoId);
            return KeyImportResult.Fail("Não foi possível importar (alguma key pode ter sido cadastrada ao mesmo tempo). Tente novamente.");
        }
    }

    public async Task<StockResult> VoidKeyAsync(long keyId, string reason, Guid? userId, string? userName)
    {
        if (string.IsNullOrWhiteSpace(reason)) return new StockResult(false, "Informe o motivo da invalidação.");
        var note = reason.Trim().Length > 300 ? reason.Trim()[..300] : reason.Trim();

        var key = await _db.GameKeys.AsNoTracking().Where(k => k.Id == keyId).Select(k => new { k.Id, k.JogoId, k.Status }).FirstOrDefaultAsync();
        if (key == null) return new StockResult(false, "Key não encontrada.");
        if (key.Status is GameKeyStatus.Reserved) return new StockResult(false, "A key está reservada para um pedido em andamento.");
        if (key.Status is GameKeyStatus.Void) return new StockResult(false, "A key já está invalidada.");

        var wasAvailable = key.Status == GameKeyStatus.Available;
        var n = await _db.GameKeys.Where(k => k.Id == keyId && k.Status == key.Status)
            .ExecuteUpdateAsync(s => s.SetProperty(k => k.Status, GameKeyStatus.Void).SetProperty(k => k.Note, note));
        if (n == 0) return new StockResult(false, "A key mudou de situação. Recarregue a página.");

        int balance;
        if (wasAvailable)
        {
            await _db.Jogos.Where(p => p.Id == key.JogoId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.StockQuantity, p => p.StockQuantity - 1)
                    .SetProperty(p => p.UpdatedAtUtc, DateTime.UtcNow));
            balance = await BalanceAsync(key.JogoId);
            await AddMovementAsync(key.JogoId, StockMovementType.Exit, -1, balance, "Key invalidada: " + note, null, userId, userName);
        }
        else balance = await BalanceAsync(key.JogoId);
        return new StockResult(true, null, balance);
    }

    public async Task<int> RecalculateAsync(int jogoId)
    {
        var count = await _db.GameKeys.CountAsync(k => k.JogoId == jogoId && k.Status == GameKeyStatus.Available);
        await _db.Jogos.Where(p => p.Id == jogoId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, count).SetProperty(p => p.UpdatedAtUtc, DateTime.UtcNow));
        return count;
    }
}
