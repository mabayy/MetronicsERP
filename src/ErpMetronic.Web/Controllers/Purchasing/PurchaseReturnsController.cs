using ErpMetronic.Domain.Constants;
using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Persistence;
using ErpMetronic.Infrastructure.Services;
using ErpMetronic.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

/// <summary>Retur Pembelian — barang dikembalikan ke pemasok: stok keluar + jurnal (Dr Hutang, Cr Persediaan).</summary>
[Authorize]
public class PurchaseReturnsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IStockService _stock;
    private readonly IDocumentNumberService _docNumber;
    private readonly IJournalService _journal;

    public PurchaseReturnsController(ApplicationDbContext db, IStockService stock, IDocumentNumberService docNumber, IJournalService journal)
    {
        _db = db; _stock = stock; _docNumber = docNumber; _journal = journal;
    }

    public async Task<IActionResult> Index()
        => View(await _db.PurchaseReturns.Include(r => r.Supplier).Include(r => r.Lines)
            .OrderByDescending(r => r.ReturnDate).ThenByDescending(r => r.Id).Take(300).ToListAsync());

    public async Task<IActionResult> Create() { await PopulateAsync(); return View(new PurchaseReturnCreateViewModel()); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseReturnCreateViewModel model)
    {
        var lines = model.Lines.Where(l => l.ProductId > 0 && l.Quantity > 0).ToList();
        if (lines.Count == 0) ModelState.AddModelError(string.Empty, "Minimal satu baris produk dengan jumlah > 0.");

        // Validasi ketersediaan stok (gabung per produk)
        if (ModelState.IsValid)
            foreach (var g in lines.GroupBy(l => l.ProductId).Select(g => new { g.Key, Qty = g.Sum(x => x.Quantity) }))
            {
                var avail = await _stock.GetBalanceAsync(g.Key, model.WarehouseId);
                if (avail < g.Qty)
                {
                    var nm = await _db.Products.Where(p => p.Id == g.Key).Select(p => p.Name).FirstOrDefaultAsync();
                    ModelState.AddModelError(string.Empty, $"Stok '{nm}' tidak mencukupi (tersedia {avail}, diminta {g.Qty}).");
                }
            }
        if (!ModelState.IsValid) { await PopulateAsync(); return View(model); }

        await using var tx = await _db.Database.BeginTransactionAsync();
        var doc = new PurchaseReturn
        {
            ReferenceNumber = await _docNumber.NextAsync(DocumentCodes.PurchaseReturn, model.ReturnDate),
            ReturnDate = model.ReturnDate, SupplierId = model.SupplierId, WarehouseId = model.WarehouseId,
            Note = model.Note, CreatedBy = User.Identity?.Name,
            Lines = lines.Select(l => new PurchaseReturnLine { ProductId = l.ProductId, Quantity = l.Quantity, UnitPrice = l.UnitPrice }).ToList()
        };
        _db.PurchaseReturns.Add(doc);
        await _db.SaveChangesAsync();

        foreach (var l in lines)
        {
            var r = await _stock.StockOutAsync(l.ProductId, model.WarehouseId, l.Quantity, model.ReturnDate, $"Retur Pembelian {doc.ReferenceNumber}", User.Identity?.Name);
            if (!r.Success) { ModelState.AddModelError(string.Empty, r.Error ?? "Gagal posting stok keluar."); await PopulateAsync(); return View(model); }
        }

        await _journal.PostAsync(model.ReturnDate, $"Retur Pembelian {doc.ReferenceNumber}", "PurchaseReturn", doc.Id, new[]
        {
            (AccountCodes.AccountsPayable, doc.Total, 0m),
            (AccountCodes.Inventory, 0m, doc.Total)
        }, User.Identity?.Name);

        await tx.CommitAsync();
        TempData["Success"] = $"Retur Pembelian {doc.ReferenceNumber} tersimpan (stok berkurang, jurnal diposting).";
        return RedirectToAction(nameof(Details), new { id = doc.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var doc = await _db.PurchaseReturns.Include(r => r.Supplier).Include(r => r.Warehouse)
            .Include(r => r.Lines).ThenInclude(l => l.Product).FirstOrDefaultAsync(r => r.Id == id);
        if (doc is null) return NotFound();
        return View(doc);
    }

    private async Task PopulateAsync()
    {
        ViewBag.Suppliers = new SelectList(await _db.Suppliers.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
        ViewBag.Warehouses = new SelectList(await _db.Warehouses.OrderBy(w => w.Name).ToListAsync(), "Id", "Name");
        ViewBag.Products = await _db.Products.OrderBy(p => p.Name).Select(p => new { p.Id, Display = p.Sku + " — " + p.Name }).ToListAsync();
    }
}
