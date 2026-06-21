using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Persistence;
using ErpMetronic.Infrastructure.Services;
using ErpMetronic.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

/// <summary>Penerimaan Barang (dari pemasok) — saat dibuat langsung memposting Stok Masuk.</summary>
[Authorize]
public class GoodsReceiptsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IStockService _stock;
    private readonly IDocumentNumberService _docNumber;

    public GoodsReceiptsController(ApplicationDbContext db, IStockService stock, IDocumentNumberService docNumber)
    {
        _db = db;
        _stock = stock;
        _docNumber = docNumber;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.GoodsReceipts
            .Include(g => g.Supplier).Include(g => g.Warehouse).Include(g => g.Lines)
            .OrderByDescending(g => g.ReceiptDate).ThenByDescending(g => g.Id)
            .Take(300).ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateAsync();
        return View(new GoodsReceiptCreateViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GoodsReceiptCreateViewModel model)
    {
        var lines = model.Lines.Where(l => l.ProductId > 0 && l.Quantity > 0).ToList();
        if (lines.Count == 0)
            ModelState.AddModelError(string.Empty, "Minimal satu baris produk dengan jumlah > 0.");

        if (!ModelState.IsValid)
        {
            await PopulateAsync();
            return View(model);
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        var receipt = new GoodsReceipt
        {
            ReferenceNumber = await _docNumber.NextAsync(Domain.Constants.DocumentCodes.GoodsReceipt, model.ReceiptDate),
            ReceiptDate = model.ReceiptDate,
            SupplierId = model.SupplierId,
            WarehouseId = model.WarehouseId,
            Note = model.Note,
            CreatedBy = User.Identity?.Name,
            Lines = lines.Select(l => new GoodsReceiptLine { ProductId = l.ProductId, Quantity = l.Quantity, UnitCost = l.UnitCost }).ToList()
        };
        _db.GoodsReceipts.Add(receipt);
        await _db.SaveChangesAsync();

        // Posting otomatis ke Stok Masuk (biaya beli memperbarui rata-rata bergerak)
        foreach (var l in lines)
            await _stock.StockInAsync(l.ProductId, model.WarehouseId, l.Quantity, model.ReceiptDate,
                $"Penerimaan {receipt.ReferenceNumber}", User.Identity?.Name, l.UnitCost);

        await tx.CommitAsync();
        TempData["Success"] = $"Penerimaan {receipt.ReferenceNumber} tersimpan & stok bertambah.";
        return RedirectToAction(nameof(Details), new { id = receipt.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var doc = await _db.GoodsReceipts
            .Include(g => g.Supplier).Include(g => g.Warehouse)
            .Include(g => g.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(g => g.Id == id);
        if (doc is null) return NotFound();
        return View(doc);
    }

    private async Task PopulateAsync()
    {
        ViewBag.Suppliers = new SelectList(await _db.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync(), "Id", "Name");
        ViewBag.Warehouses = new SelectList(await _db.Warehouses.OrderBy(w => w.Name).ToListAsync(), "Id", "Name");
        ViewBag.Products = await _db.Products.OrderBy(p => p.Name)
            .Select(p => new { p.Id, Display = p.Sku + " — " + p.Name }).ToListAsync();
    }
}
