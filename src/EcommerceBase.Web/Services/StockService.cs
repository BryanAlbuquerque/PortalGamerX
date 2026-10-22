using EcommerceBase.Web.Data;
using EcommerceBase.Web.Domain;
using EcommerceBase.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcommerceBase.Web.Services;

public record StockResult(bool Success, string? Error, int? NewBalance = null);

public interface IStockService
{
    /// <summary>Baixa atômica: só decrementa se houver saldo (UPDATE ... WHERE estoque &gt;= qtd). Deve rodar dentro de transação do pedido.</summary>
    Task<bool> TryReserveAsync(int productId, int quantity, Guid orderId, string reason);
    Task ReleaseAsync(int productId, int quantity, Guid orderId, string reason);
    Task<StockResult> AdminMoveAsync(int productId, StockMovementType type, int quantity, string reason, Guid? userId, string? userName);
}

public class StockService : IStockService
{
    private readonly AppDbContext _db;
    public StockService(AppDbContext db) => _db = db;

    public async Task<bool> TryReserveAsync(int productId, int quantity, Guid orderId, string reason)
    {
        if (quantity <= 0) return false;
        var affected = await _db.Products
            .Where(p => p.Id == productId && p.IsActive && p.IsAvailable && p.StockQuantity >= quantity)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.StockQuantity, p => p.StockQuantity - quantity)
                .SetProperty(p => p.UpdatedAtUtc, DateTime.UtcNow));
        if (affected == 0) return false;

        var balance = await _db.Products.Where(p => p.Id == productId).Select(p => p.StockQuantity).FirstAsync();
        _db.StockMovements.Add(new StockMovement
        {
            ProductId = productId,
            Type = StockMovementType.OrderReservation,
            Quantity = -quantity,
            BalanceAfter = balance,
            Reason = reason,
            OrderId = orderId,
            UserName = "sistema"
        });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task ReleaseAsync(int productId, int quantity, Guid orderId, string reason)
    {
        if (quantity <= 0) return;
        await _db.Products.Where(p => p.Id == productId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.StockQuantity, p => p.StockQuantity + quantity)
                .SetProperty(p => p.UpdatedAtUtc, DateTime.UtcNow));
        var balance = await _db.Products.Where(p => p.Id == productId).Select(p => p.StockQuantity).FirstAsync();
        _db.StockMovements.Add(new StockMovement
        {
            ProductId = productId,
            Type = StockMovementType.OrderRelease,
            Quantity = quantity,
            BalanceAfter = balance,
            Reason = reason,
            OrderId = orderId,
            UserName = "sistema"
        });
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Entrada/Saída: <paramref name="quantity"/> é sempre positiva (o tipo define o sinal).
    /// Ajuste: <paramref name="quantity"/> é o NOVO saldo absoluto (&gt;= 0). Motivo é obrigatório.
    /// </summary>
    public async Task<StockResult> AdminMoveAsync(int productId, StockMovementType type, int quantity, string reason, Guid? userId, string? userName)
    {
        if (string.IsNullOrWhiteSpace(reason)) return new StockResult(false, "Informe o motivo da movimentação.");
        if (type is StockMovementType.OrderReservation or StockMovementType.OrderRelease)
            return new StockResult(false, "Tipo de movimentação inválido.");

        var current = await _db.Products.Where(p => p.Id == productId).Select(p => (int?)p.StockQuantity).FirstOrDefaultAsync();
        if (current == null) return new StockResult(false, "Produto não encontrado.");

        int delta;
        switch (type)
        {
            case StockMovementType.Entry:
                if (quantity <= 0) return new StockResult(false, "A quantidade de entrada deve ser maior que zero.");
                delta = quantity;
                break;
            case StockMovementType.Exit:
                if (quantity <= 0) return new StockResult(false, "A quantidade de saída deve ser maior que zero.");
                delta = -quantity;
                break;
            default:
                if (quantity < 0) return new StockResult(false, "O novo saldo não pode ser negativo.");
                delta = quantity - current.Value;
                break;
        }

        // Atualização atômica: impede saldo negativo mesmo com concorrência.
        var affected = await _db.Products
            .Where(p => p.Id == productId && p.StockQuantity + delta >= 0)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.StockQuantity, p => p.StockQuantity + delta)
                .SetProperty(p => p.UpdatedAtUtc, DateTime.UtcNow));
        if (affected == 0) return new StockResult(false, "Estoque insuficiente para esta saída.");

        var balance = await _db.Products.Where(p => p.Id == productId).Select(p => p.StockQuantity).FirstAsync();
        _db.StockMovements.Add(new StockMovement
        {
            ProductId = productId,
            Type = type,
            Quantity = delta,
            BalanceAfter = balance,
            Reason = reason.Trim(),
            UserId = userId,
            UserName = userName
        });
        await _db.SaveChangesAsync();
        return new StockResult(true, null, balance);
    }
}
