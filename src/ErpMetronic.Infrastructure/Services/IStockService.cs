using ErpMetronic.Domain.Entities;

namespace ErpMetronic.Infrastructure.Services;

public record StockResult(bool Success, string? Error = null, StockMovement? Movement = null)
{
    public static StockResult Ok(StockMovement m) => new(true, null, m);
    public static StockResult Fail(string error) => new(false, error);
}

/// <summary>Operasi manajemen stok: masuk, keluar, transfer, penyesuaian.</summary>
public interface IStockService
{
    Task<int> GetBalanceAsync(int productId, int warehouseId);

    Task<StockResult> StockInAsync(int productId, int warehouseId, int quantity, DateTime date, string? note, string? user);
    Task<StockResult> StockOutAsync(int productId, int warehouseId, int quantity, DateTime date, string? note, string? user);
    Task<StockResult> TransferAsync(int productId, int sourceWarehouseId, int destinationWarehouseId, int quantity, DateTime date, string? note, string? user);
    Task<StockResult> AdjustAsync(int productId, int warehouseId, int countedQuantity, DateTime date, string? note, string? user);
}
