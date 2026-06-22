using ErpMetronic.Domain.Entities;

namespace ErpMetronic.Infrastructure.Services;

public record StockResult(bool Success, string? Error = null, StockMovement? Movement = null, decimal UnitCost = 0)
{
    public static StockResult Ok(StockMovement m, decimal unitCost = 0) => new(true, null, m, unitCost);
    public static StockResult Fail(string error) => new(false, error);
}

/// <summary>Operasi manajemen stok: masuk, keluar, transfer, penyesuaian.</summary>
public interface IStockService
{
    Task<int> GetBalanceAsync(int productId, int warehouseId);

    /// <summary>Stok masuk. Bila <paramref name="unitCost"/> diisi, biaya rata-rata bergerak diperbarui.
    /// Untuk produk berlacak Lot/Serial, sub-ledger lot/serial ikut dicatat (auto bila tak diisi).</summary>
    Task<StockResult> StockInAsync(int productId, int warehouseId, int quantity, DateTime date, string? note, string? user,
        decimal? unitCost = null, string? lotNumber = null, DateTime? expiry = null, IEnumerable<string>? serials = null);
    /// <summary>Stok keluar. Mengembalikan biaya rata-rata produk saat itu (untuk HPP) pada <see cref="StockResult.UnitCost"/>.</summary>
    Task<StockResult> StockOutAsync(int productId, int warehouseId, int quantity, DateTime date, string? note, string? user);
    Task<StockResult> TransferAsync(int productId, int sourceWarehouseId, int destinationWarehouseId, int quantity, DateTime date, string? note, string? user);
    Task<StockResult> AdjustAsync(int productId, int warehouseId, int countedQuantity, DateTime date, string? note, string? user);
}
