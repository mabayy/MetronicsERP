using ErpMetronic.Domain.Enums;
using ErpMetronic.Infrastructure.Persistence;
using ErpMetronic.Infrastructure.Services;
using ErpMetronic.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

[Authorize]
public class StockController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IStockService _stock;
    private readonly ICurrencyService _currency;

    public StockController(ApplicationDbContext db, IStockService stock, ICurrencyService currency)
    {
        _db = db;
        _stock = stock;
        _currency = currency;
    }

    // ---------- Riwayat & Saldo ----------

    public async Task<IActionResult> Movements(MovementType? type, int? productId)
    {
        var query = _db.StockMovements
            .Include(m => m.Product)
            .Include(m => m.Warehouse)
            .Include(m => m.DestinationWarehouse)
            .AsQueryable();

        if (type.HasValue) query = query.Where(m => m.Type == type);
        if (productId.HasValue) query = query.Where(m => m.ProductId == productId);

        ViewBag.FilterType = type;
        ViewBag.ProductId = productId;
        ViewBag.Products = await ProductSelectAsync(productId);
        return View(await query.OrderByDescending(m => m.MovementDate).ThenByDescending(m => m.Id).Take(300).ToListAsync());
    }

    public async Task<IActionResult> Balances(int? warehouseId)
    {
        var query = _db.ProductStocks
            .Include(s => s.Product)
            .Include(s => s.Warehouse)
            .AsQueryable();

        if (warehouseId.HasValue) query = query.Where(s => s.WarehouseId == warehouseId);

        ViewBag.WarehouseId = warehouseId;
        ViewBag.Warehouses = await WarehouseSelectAsync(warehouseId);
        return View(await query
            .OrderBy(s => s.Product!.Name).ThenBy(s => s.Warehouse!.Name)
            .ToListAsync());
    }

    // ---------- Stock In ----------

    public async Task<IActionResult> In()
    {
        await PopulateAsync();
        return View(new StockInViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> In(StockInViewModel model)
    {
        if (!ModelState.IsValid) { await PopulateAsync(); return View(model); }
        var result = await _stock.StockInAsync(model.ProductId, model.WarehouseId, model.Quantity, model.MovementDate, model.Note, User.Identity?.Name);
        return AfterMove(result, nameof(In), model, "Stok masuk berhasil dicatat.");
    }

    // ---------- Stock Out ----------

    public async Task<IActionResult> Out()
    {
        await PopulateAsync();
        return View(new StockOutViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Out(StockOutViewModel model)
    {
        if (!ModelState.IsValid) { await PopulateAsync(); return View(model); }
        var result = await _stock.StockOutAsync(model.ProductId, model.WarehouseId, model.Quantity, model.MovementDate, model.Note, User.Identity?.Name);
        return AfterMove(result, nameof(Out), model, "Stok keluar berhasil dicatat.");
    }

    // ---------- Transfer ----------

    public async Task<IActionResult> Transfer()
    {
        await PopulateAsync();
        return View(new StockTransferViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Transfer(StockTransferViewModel model)
    {
        if (!ModelState.IsValid) { await PopulateAsync(); return View(model); }
        var result = await _stock.TransferAsync(model.ProductId, model.SourceWarehouseId, model.DestinationWarehouseId, model.Quantity, model.MovementDate, model.Note, User.Identity?.Name);
        return AfterMove(result, nameof(Transfer), model, "Transfer stok berhasil dicatat.");
    }

    // ---------- Adjustment ----------

    public async Task<IActionResult> Adjust()
    {
        await PopulateAsync();
        return View(new StockAdjustmentViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Adjust(StockAdjustmentViewModel model)
    {
        if (!ModelState.IsValid) { await PopulateAsync(); return View(model); }
        var result = await _stock.AdjustAsync(model.ProductId, model.WarehouseId, model.CountedQuantity, model.MovementDate, model.Note, User.Identity?.Name);
        return AfterMove(result, nameof(Adjust), model, "Penyesuaian stok berhasil dicatat.");
    }

    // ---------- AJAX: saldo stok saat ini ----------

    [HttpGet]
    public async Task<IActionResult> GetBalance(int productId, int warehouseId)
    {
        if (productId == 0 || warehouseId == 0) return Json(new { quantity = (int?)null });
        var qty = await _stock.GetBalanceAsync(productId, warehouseId);
        return Json(new { quantity = qty });
    }

    // ---------- helpers ----------

    private IActionResult AfterMove(StockResult result, string action, object model, string successMessage)
    {
        if (result.Success)
        {
            TempData["Success"] = $"{successMessage} (No: {result.Movement!.ReferenceNumber})";
            return RedirectToAction(nameof(Movements));
        }
        ModelState.AddModelError(string.Empty, result.Error ?? "Operasi gagal.");
        PopulateAsync().GetAwaiter().GetResult();
        return View(action, model);
    }

    private async Task PopulateAsync(int? selectedProduct = null, int? selectedWarehouse = null)
    {
        ViewBag.Products = await ProductSelectAsync(selectedProduct);
        ViewBag.Warehouses = await WarehouseSelectAsync(selectedWarehouse);
    }

    private async Task<SelectList> ProductSelectAsync(int? selected)
    {
        var products = await _db.Products
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, Display = p.Sku + " — " + p.Name })
            .ToListAsync();
        return new SelectList(products, "Id", "Display", selected);
    }

    private async Task<SelectList> WarehouseSelectAsync(int? selected)
    {
        var warehouses = await _db.Warehouses.OrderBy(w => w.Name).ToListAsync();
        return new SelectList(warehouses, "Id", "Name", selected);
    }

    // ---------- Kartu Stok (Stock Card) ----------

    public async Task<IActionResult> Card(int? productId, int? warehouseId, DateTime? from, DateTime? to)
    {
        ViewBag.Products = await ProductSelectAsync(productId);
        ViewBag.Warehouses = await WarehouseSelectAsync(warehouseId);
        ViewBag.ProductId = productId;
        ViewBag.WarehouseId = warehouseId;
        ViewBag.From = from;
        ViewBag.To = to;

        if (productId is null)
            return View(new List<StockCardRow>());

        var movements = await _db.StockMovements
            .Where(m => m.ProductId == productId)
            .Include(m => m.Warehouse).Include(m => m.DestinationWarehouse)
            .OrderBy(m => m.MovementDate).ThenBy(m => m.Id)
            .ToListAsync();

        // Pengaruh tiap pergerakan terhadap saldo pada lingkup terpilih (total / per gudang)
        int Delta(Domain.Entities.StockMovement m) => m.Type switch
        {
            MovementType.StockIn => warehouseId is null || m.WarehouseId == warehouseId ? m.Quantity : 0,
            MovementType.StockOut => warehouseId is null || m.WarehouseId == warehouseId ? -m.Quantity : 0,
            MovementType.Adjustment => warehouseId is null || m.WarehouseId == warehouseId ? m.Quantity : 0,
            MovementType.Transfer => warehouseId is null
                ? 0
                : (m.WarehouseId == warehouseId ? -m.Quantity : (m.DestinationWarehouseId == warehouseId ? m.Quantity : 0)),
            _ => 0
        };

        var opening = from.HasValue ? movements.Where(m => m.MovementDate < from.Value).Sum(Delta) : 0;
        var running = opening;
        var rows = new List<StockCardRow>();
        foreach (var m in movements)
        {
            if (from.HasValue && m.MovementDate < from.Value) continue;
            if (to.HasValue && m.MovementDate > to.Value) continue;
            var d = Delta(m);
            if (d == 0) continue; // tidak memengaruhi lingkup ini
            running += d;
            rows.Add(new StockCardRow
            {
                Date = m.MovementDate,
                Reference = m.ReferenceNumber,
                Type = m.Type,
                In = d > 0 ? d : 0,
                Out = d < 0 ? -d : 0,
                Balance = running,
                Note = m.Note,
                Warehouse = warehouseId is null && m.Type == MovementType.Transfer ? null : m.Warehouse?.Name
            });
        }

        ViewBag.Opening = opening;
        ViewBag.Closing = running;
        return View(rows);
    }

    // ---------- Nilai Persediaan (Inventory Valuation) ----------

    public async Task<IActionResult> Valuation(int? warehouseId)
    {
        var query = _db.ProductStocks
            .Include(s => s.Product).ThenInclude(p => p!.Currency)
            .Include(s => s.Warehouse)
            .Where(s => s.Quantity != 0)
            .AsQueryable();
        if (warehouseId.HasValue) query = query.Where(s => s.WarehouseId == warehouseId);

        var stocks = await query.OrderBy(s => s.Product!.Name).ThenBy(s => s.Warehouse!.Name).ToListAsync();
        var baseCurrency = await _currency.GetBaseCurrencyAsync();

        var rows = new List<InventoryValuationRow>();
        decimal grandTotal = 0;
        foreach (var s in stocks)
        {
            var unitCost = s.Product!.PurchasePrice;
            decimal? unitCostBase = unitCost;
            if (baseCurrency is not null && s.Product.CurrencyId is int cid && cid != baseCurrency.Id)
                unitCostBase = await _currency.ConvertAsync(unitCost, cid, baseCurrency.Id, DateTime.Today);

            var value = unitCostBase.HasValue ? unitCostBase.Value * s.Quantity : (decimal?)null;
            if (value.HasValue) grandTotal += value.Value;

            rows.Add(new InventoryValuationRow
            {
                Sku = s.Product.Sku,
                ProductName = s.Product.Name,
                Warehouse = s.Warehouse!.Name,
                Quantity = s.Quantity,
                UnitCostBase = unitCostBase,
                Value = value
            });
        }

        ViewBag.Warehouses = await WarehouseSelectAsync(warehouseId);
        ViewBag.WarehouseId = warehouseId;
        ViewBag.BaseCurrency = baseCurrency;
        ViewBag.GrandTotal = grandTotal;
        return View(rows);
    }
}

public class StockCardRow
{
    public DateTime Date { get; set; }
    public string Reference { get; set; } = string.Empty;
    public MovementType Type { get; set; }
    public int In { get; set; }
    public int Out { get; set; }
    public int Balance { get; set; }
    public string? Note { get; set; }
    public string? Warehouse { get; set; }
}

public class InventoryValuationRow
{
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal? UnitCostBase { get; set; }
    public decimal? Value { get; set; }
}
