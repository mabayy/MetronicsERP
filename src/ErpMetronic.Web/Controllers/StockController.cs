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

    public StockController(ApplicationDbContext db, IStockService stock)
    {
        _db = db;
        _stock = stock;
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
}
