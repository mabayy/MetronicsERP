using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Persistence;
using ErpMetronic.Infrastructure.Services;
using ErpMetronic.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

/// <summary>Pengeluaran/Pengiriman Barang (ke pelanggan) — saat dibuat langsung memposting Stok Keluar.</summary>
[Authorize]
public class DeliveryOrdersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IStockService _stock;
    private readonly IDocumentNumberService _docNumber;

    public DeliveryOrdersController(ApplicationDbContext db, IStockService stock, IDocumentNumberService docNumber)
    {
        _db = db;
        _stock = stock;
        _docNumber = docNumber;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.DeliveryOrders
            .Include(d => d.Customer).Include(d => d.Warehouse).Include(d => d.Lines)
            .OrderByDescending(d => d.DeliveryDate).ThenByDescending(d => d.Id)
            .Take(300).ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateAsync();
        return View(new DeliveryCreateViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DeliveryCreateViewModel model)
    {
        var lines = model.Lines.Where(l => l.ProductId > 0 && l.Quantity > 0).ToList();
        if (lines.Count == 0)
            ModelState.AddModelError(string.Empty, "Minimal satu baris produk dengan jumlah > 0.");

        // Gabungkan kuantitas produk yang sama agar validasi saldo akurat
        var grouped = lines.GroupBy(l => l.ProductId).Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) }).ToList();

        if (ModelState.IsValid)
        {
            foreach (var g in grouped)
            {
                var available = await _stock.GetBalanceAsync(g.ProductId, model.WarehouseId);
                if (available < g.Qty)
                {
                    var name = await _db.Products.Where(p => p.Id == g.ProductId).Select(p => p.Name).FirstOrDefaultAsync();
                    ModelState.AddModelError(string.Empty, $"Stok '{name}' tidak mencukupi di gudang ini (tersedia {available}, diminta {g.Qty}).");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateAsync();
            return View(model);
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        var doc = new DeliveryOrder
        {
            ReferenceNumber = await _docNumber.NextAsync(Domain.Constants.DocumentCodes.DeliveryOrder, model.DeliveryDate),
            DeliveryDate = model.DeliveryDate,
            CustomerId = model.CustomerId,
            WarehouseId = model.WarehouseId,
            Note = model.Note,
            CreatedBy = User.Identity?.Name,
            Lines = lines.Select(l => new DeliveryOrderLine { ProductId = l.ProductId, Quantity = l.Quantity, UnitPrice = l.UnitPrice }).ToList()
        };
        _db.DeliveryOrders.Add(doc);
        await _db.SaveChangesAsync();

        // Posting otomatis ke Stok Keluar
        foreach (var l in lines)
        {
            var result = await _stock.StockOutAsync(l.ProductId, model.WarehouseId, l.Quantity, model.DeliveryDate,
                $"Pengiriman {doc.ReferenceNumber}", User.Identity?.Name);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Gagal memposting stok keluar.");
                await PopulateAsync();
                return View(model); // transaksi otomatis rollback (belum commit)
            }
        }

        await tx.CommitAsync();
        TempData["Success"] = $"Pengiriman {doc.ReferenceNumber} tersimpan & stok berkurang.";
        return RedirectToAction(nameof(Details), new { id = doc.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var doc = await _db.DeliveryOrders
            .Include(d => d.Customer).Include(d => d.Warehouse)
            .Include(d => d.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (doc is null) return NotFound();
        return View(doc);
    }

    private async Task PopulateAsync()
    {
        ViewBag.Customers = new SelectList(await _db.Customers.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
        ViewBag.Warehouses = new SelectList(await _db.Warehouses.OrderBy(w => w.Name).ToListAsync(), "Id", "Name");
        ViewBag.Products = await _db.Products.OrderBy(p => p.Name)
            .Select(p => new { p.Id, Display = p.Sku + " — " + p.Name }).ToListAsync();
    }
}
