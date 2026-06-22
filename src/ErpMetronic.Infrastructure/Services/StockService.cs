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

    public async Task<StockResult> StockInAsync(int productId, int warehouseId, int quantity, DateTime date, string? note, string? user,
        decimal? unitCost = null, string? lotNumber = null, DateTime? expiry = null, IEnumerable<string>? serials = null)
    {
        if (quantity <= 0) return StockResult.Fail("Jumlah harus lebih dari 0.");
        if (!await ValidateAsync(productId, warehouseId)) return StockResult.Fail("Produk atau gudang tidak valid.");

        var stock = await GetOrCreateStockAsync(productId, warehouseId);
        var product = await _db.Products.FirstAsync(p => p.Id == productId);

        // Perbarui biaya rata-rata bergerak bila biaya masuk diberikan.
        var movementCost = unitCost ?? product.AverageCost;
        if (unitCost is decimal c && c >= 0)
        {
            var qtyBefore = Math.Max(product.StockQuantity, 0);
            var newQty = qtyBefore + quantity;
            product.AverageCost = newQty > 0
                ? Math.Round((qtyBefore * product.AverageCost + quantity * c) / newQty, 4, MidpointRounding.AwayFromZero)
                : c;
        }

        stock.Quantity += quantity;
        product.StockQuantity += quantity;

        // Sub-ledger pelacakan (lot/serial) — otomatis konsisten dengan saldo gudang.
        if (product.TrackingType == ProductTrackingType.Lot)
            await ReceiveLotAsync(productId, warehouseId, quantity, lotNumber, expiry, date);
        else if (product.TrackingType == ProductTrackingType.Serial)
            await ReceiveSerialsAsync(productId, warehouseId, quantity, serials, date);

        var movement = await BuildMovementAsync(MovementType.StockIn, productId, warehouseId, null, quantity, date, note, user, movementCost);
        _db.StockMovements.Add(movement);
        await _db.SaveChangesAsync();
        return StockResult.Ok(movement, movementCost);
    }

    private async Task ReceiveLotAsync(int productId, int warehouseId, int quantity, string? lotNumber, DateTime? expiry, DateTime date)
    {
        var lot = (lotNumber ?? "").Trim();
        if (lot.Length == 0) lot = $"LOT-{date:yyyyMMdd}"; // auto bila tak diisi
        var existing = await _db.StockLots.FirstOrDefaultAsync(l => l.ProductId == productId && l.WarehouseId == warehouseId && l.LotNumber == lot);
        if (existing is null)
            _db.StockLots.Add(new StockLot { ProductId = productId, WarehouseId = warehouseId, LotNumber = lot, ExpiryDate = expiry, Quantity = quantity });
        else
        {
            existing.Quantity += quantity;
            if (expiry.HasValue) existing.ExpiryDate = expiry;
        }
    }

    private async Task ReceiveSerialsAsync(int productId, int warehouseId, int quantity, IEnumerable<string>? serials, DateTime date)
    {
        var list = (serials ?? Enumerable.Empty<string>())
            .Select(s => s?.Trim() ?? "").Where(s => s.Length > 0).Distinct().ToList();
        // Auto-generate placeholder bila jumlah serial tak sesuai (mis. tanpa input di alur PO/SO).
        if (list.Count != quantity)
        {
            var baseTick = date.ToString("yyyyMMddHHmmss");
            list = Enumerable.Range(1, quantity).Select(n => $"SN-{productId}-{baseTick}-{n}").ToList();
        }
        foreach (var sn in list)
        {
            if (await _db.SerialNumbers.AnyAsync(x => x.ProductId == productId && x.SerialNo == sn)) continue;
            _db.SerialNumbers.Add(new SerialNumber { ProductId = productId, SerialNo = sn, WarehouseId = warehouseId, IsInStock = true });
        }
    }

    public async Task<StockResult> StockOutAsync(int productId, int warehouseId, int quantity, DateTime date, string? note, string? user)
    {
        if (quantity <= 0) return StockResult.Fail("Jumlah harus lebih dari 0.");
        if (!await ValidateAsync(productId, warehouseId)) return StockResult.Fail("Produk atau gudang tidak valid.");

        var stock = await GetOrCreateStockAsync(productId, warehouseId);
        if (stock.Quantity < quantity)
            return StockResult.Fail($"Stok tidak mencukupi. Saldo saat ini: {stock.Quantity}.");

        var product = await _db.Products.FirstAsync(p => p.Id == productId);
        var cost = product.AverageCost; // biaya yang mengalir keluar (untuk HPP)

        stock.Quantity -= quantity;
        product.StockQuantity -= quantity;

        // Kurangi sub-ledger pelacakan: lot secara FEFO (kedaluwarsa terdekat dulu), serial yang tersedia.
        if (product.TrackingType == ProductTrackingType.Lot)
            await ConsumeLotsFefoAsync(productId, warehouseId, quantity);
        else if (product.TrackingType == ProductTrackingType.Serial)
            await ConsumeSerialsAsync(productId, warehouseId, quantity);

        var movement = await BuildMovementAsync(MovementType.StockOut, productId, warehouseId, null, quantity, date, note, user, cost);
        _db.StockMovements.Add(movement);
        await _db.SaveChangesAsync();
        return StockResult.Ok(movement, cost);
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

        var avgCost = await _db.Products.Where(p => p.Id == productId).Select(p => p.AverageCost).FirstAsync();
        var movement = await BuildMovementAsync(MovementType.Transfer, productId, sourceWarehouseId, destinationWarehouseId, quantity, date, note, user, avgCost);
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

        var product = await _db.Products.FirstAsync(p => p.Id == productId);
        stock.Quantity = countedQuantity;
        product.StockQuantity += variance;

        var movement = await BuildMovementAsync(MovementType.Adjustment, productId, warehouseId, null, variance, date, note, user, product.AverageCost);
        _db.StockMovements.Add(movement);
        await _db.SaveChangesAsync();
        return StockResult.Ok(movement);
    }

    // Konsumsi lot secara FEFO: kedaluwarsa paling awal dulu (null = paling akhir). Best-effort.
    private async Task ConsumeLotsFefoAsync(int productId, int warehouseId, int quantity)
    {
        var remaining = quantity;
        var lots = await _db.StockLots
            .Where(l => l.ProductId == productId && l.WarehouseId == warehouseId && l.Quantity > 0)
            .OrderBy(l => l.ExpiryDate == null).ThenBy(l => l.ExpiryDate).ThenBy(l => l.Id)
            .ToListAsync();
        foreach (var lot in lots)
        {
            if (remaining <= 0) break;
            var take = Math.Min(lot.Quantity, remaining);
            lot.Quantity -= take;
            remaining -= take;
        }
    }

    // Tandai sejumlah unit serial yang masih in-stock di gudang ini sebagai keluar.
    private async Task ConsumeSerialsAsync(int productId, int warehouseId, int quantity)
    {
        var units = await _db.SerialNumbers
            .Where(s => s.ProductId == productId && s.WarehouseId == warehouseId && s.IsInStock)
            .OrderBy(s => s.Id).Take(quantity).ToListAsync();
        foreach (var u in units) { u.IsInStock = false; u.WarehouseId = null; }
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

    private async Task<StockMovement> BuildMovementAsync(MovementType type, int productId, int warehouseId, int? destWarehouseId, int quantity, DateTime date, string? note, string? user, decimal unitCost = 0)
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
            UnitCost = unitCost,
            Note = note,
            CreatedBy = user
        };
    }
}
