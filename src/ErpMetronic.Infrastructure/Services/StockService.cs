using ErpMetronic.Domain.Entities;
using ErpMetronic.Domain.Enums;
using ErpMetronic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Infrastructure.Services;

public class StockService : IStockService
{
    private readonly ApplicationDbContext _db;
    private readonly IDocumentNumberService _docNumber;

    public StockService(ApplicationDbContext db, IDocumentNumberService docNumber)
    {
        _db = db;
        _docNumber = docNumber;
    }

    public async Task<int> GetBalanceAsync(int productId, int warehouseId)
        => await _db.ProductStocks
            .Where(s => s.ProductId == productId && s.WarehouseId == warehouseId)
            .Select(s => (int?)s.Quantity)
            .FirstOrDefaultAsync() ?? 0;

    public async Task<StockResult> StockInAsync(int productId, int warehouseId, int quantity, DateTime date, string? note, string? user)
    {
        if (quantity <= 0) return StockResult.Fail("Jumlah harus lebih dari 0.");
        if (!await ValidateAsync(productId, warehouseId)) return StockResult.Fail("Produk atau gudang tidak valid.");

        var stock = await GetOrCreateStockAsync(productId, warehouseId);
        stock.Quantity += quantity;
        await AdjustProductTotalAsync(productId, quantity);

        var movement = await BuildMovementAsync(MovementType.StockIn, productId, warehouseId, null, quantity, date, note, user);
        _db.StockMovements.Add(movement);
        await _db.SaveChangesAsync();
        return StockResult.Ok(movement);
    }

    public async Task<StockResult> StockOutAsync(int productId, int warehouseId, int quantity, DateTime date, string? note, string? user)
    {
        if (quantity <= 0) return StockResult.Fail("Jumlah harus lebih dari 0.");
        if (!await ValidateAsync(productId, warehouseId)) return StockResult.Fail("Produk atau gudang tidak valid.");

        var stock = await GetOrCreateStockAsync(productId, warehouseId);
        if (stock.Quantity < quantity)
            return StockResult.Fail($"Stok tidak mencukupi. Saldo saat ini: {stock.Quantity}.");

        stock.Quantity -= quantity;
        await AdjustProductTotalAsync(productId, -quantity);

        var movement = await BuildMovementAsync(MovementType.StockOut, productId, warehouseId, null, quantity, date, note, user);
        _db.StockMovements.Add(movement);
        await _db.SaveChangesAsync();
        return StockResult.Ok(movement);
    }

    public async Task<StockResult> TransferAsync(int productId, int sourceWarehouseId, int destinationWarehouseId, int quantity, DateTime date, string? note, string? user)
    {
        if (quantity <= 0) return StockResult.Fail("Jumlah harus lebih dari 0.");
        if (sourceWarehouseId == destinationWarehouseId) return StockResult.Fail("Gudang asal dan tujuan harus berbeda.");
        if (!await ValidateAsync(productId, sourceWarehouseId) || !await _db.Warehouses.AnyAsync(w => w.Id == destinationWarehouseId))
            return StockResult.Fail("Produk atau gudang tidak valid.");

        var source = await GetOrCreateStockAsync(productId, sourceWarehouseId);
        if (source.Quantity < quantity)
            return StockResult.Fail($"Stok gudang asal tidak mencukupi. Saldo saat ini: {source.Quantity}.");

        var dest = await GetOrCreateStockAsync(productId, destinationWarehouseId);
        source.Quantity -= quantity;
        dest.Quantity += quantity;
        // Total stok produk tidak berubah pada transfer.

        var movement = await BuildMovementAsync(MovementType.Transfer, productId, sourceWarehouseId, destinationWarehouseId, quantity, date, note, user);
        _db.StockMovements.Add(movement);
        await _db.SaveChangesAsync();
        return StockResult.Ok(movement);
    }

    public async Task<StockResult> AdjustAsync(int productId, int warehouseId, int countedQuantity, DateTime date, string? note, string? user)
    {
        if (countedQuantity < 0) return StockResult.Fail("Jumlah hasil hitung tidak boleh negatif.");
        if (!await ValidateAsync(productId, warehouseId)) return StockResult.Fail("Produk atau gudang tidak valid.");

        var stock = await GetOrCreateStockAsync(productId, warehouseId);
        var variance = countedQuantity - stock.Quantity;
        if (variance == 0) return StockResult.Fail("Tidak ada selisih—saldo sudah sesuai.");

        stock.Quantity = countedQuantity;
        await AdjustProductTotalAsync(productId, variance);

        var movement = await BuildMovementAsync(MovementType.Adjustment, productId, warehouseId, null, variance, date, note, user);
        _db.StockMovements.Add(movement);
        await _db.SaveChangesAsync();
        return StockResult.Ok(movement);
    }

    // ---------- helpers ----------

    private async Task<bool> ValidateAsync(int productId, int warehouseId)
        => await _db.Products.AnyAsync(p => p.Id == productId)
           && await _db.Warehouses.AnyAsync(w => w.Id == warehouseId);

    private async Task<ProductStock> GetOrCreateStockAsync(int productId, int warehouseId)
    {
        var stock = await _db.ProductStocks.FirstOrDefaultAsync(s => s.ProductId == productId && s.WarehouseId == warehouseId);
        if (stock is null)
        {
            stock = new ProductStock { ProductId = productId, WarehouseId = warehouseId, Quantity = 0 };
            _db.ProductStocks.Add(stock);
        }
        return stock;
    }

    private async Task AdjustProductTotalAsync(int productId, int delta)
    {
        var product = await _db.Products.FirstAsync(p => p.Id == productId);
        product.StockQuantity += delta;
    }

    private async Task<StockMovement> BuildMovementAsync(MovementType type, int productId, int warehouseId, int? destWarehouseId, int quantity, DateTime date, string? note, string? user)
    {
        var docCode = type switch
        {
            MovementType.StockIn => Domain.Constants.DocumentCodes.StockIn,
            MovementType.StockOut => Domain.Constants.DocumentCodes.StockOut,
            MovementType.Transfer => Domain.Constants.DocumentCodes.StockTransfer,
            MovementType.Adjustment => Domain.Constants.DocumentCodes.StockAdjustment,
            _ => Domain.Constants.DocumentCodes.StockIn
        };
        return new StockMovement
        {
            ReferenceNumber = await _docNumber.NextAsync(docCode, date),
            MovementDate = date,
            Type = type,
            ProductId = productId,
            WarehouseId = warehouseId,
            DestinationWarehouseId = destWarehouseId,
            Quantity = quantity,
            Note = note,
            CreatedBy = user
        };
    }
}
