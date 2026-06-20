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

/// <summary>Retur Penjualan — barang kembali: stok masuk + jurnal (Dr Pendapatan, Cr Piutang).</summary>
[Authorize]
public class SalesReturnsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IStockService _stock;
    private readonly IDocumentNumberService _docNumber;
    private readonly IJournalService _journal;

    public SalesReturnsController(ApplicationDbContext db, IStockService stock, IDocumentNumberService docNumber, IJournalService journal)
    {
        _db = db; _stock = stock; _docNumber = docNumber; _journal = journal;
    }

    public async Task<IActionResult> Index()
        => View(await _db.SalesReturns.Include(r => r.Customer).Include(r => r.Lines)
            .OrderByDescending(r => r.ReturnDate).ThenByDescending(r => r.Id).Take(300).ToListAsync());

    public async Task<IActionResult> Create() { await PopulateAsync(); return View(new SalesReturnCreateViewModel()); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SalesReturnCreateViewModel model)
    {
        var lines = model.Lines.Where(l => l.ProductId > 0 && l.Quantity > 0).ToList();
        if (lines.Count == 0) ModelState.AddModelError(string.Empty, "Minimal satu baris produk dengan jumlah > 0.");
        if (!ModelState.IsValid) { await PopulateAsync(); return View(model); }

        await using var tx = await _db.Database.BeginTransactionAsync();
        var doc = new SalesReturn
        {
            ReferenceNumber = await _docNumber.NextAsync(DocumentCodes.SalesReturn, model.ReturnDate),
            ReturnDate = model.ReturnDate, CustomerId = model.CustomerId, WarehouseId = model.WarehouseId,
            Note = model.Note, CreatedBy = User.Identity?.Name,
            Lines = lines.Select(l => new SalesReturnLine { ProductId = l.ProductId, Quantity = l.Quantity, UnitPrice = l.UnitPrice }).ToList()
        };
        _db.SalesReturns.Add(doc);
        await _db.SaveChangesAsync();

        foreach (var l in lines)
            await _stock.StockInAsync(l.ProductId, model.WarehouseId, l.Quantity, model.ReturnDate, $"Retur Penjualan {doc.ReferenceNumber}", User.Identity?.Name);

        await _journal.PostAsync(model.ReturnDate, $"Retur Penjualan {doc.ReferenceNumber}", "SalesReturn", doc.Id, new[]
        {
            (AccountCodes.SalesRevenue, doc.Total, 0m),
            (AccountCodes.AccountsReceivable, 0m, doc.Total)
        }, User.Identity?.Name);

        await tx.CommitAsync();
        TempData["Success"] = $"Retur Penjualan {doc.ReferenceNumber} tersimpan (stok bertambah, jurnal diposting).";
        return RedirectToAction(nameof(Details), new { id = doc.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var doc = await _db.SalesReturns.Include(r => r.Customer).Include(r => r.Warehouse)
            .Include(r => r.Lines).ThenInclude(l => l.Product).FirstOrDefaultAsync(r => r.Id == id);
        if (doc is null) return NotFound();
        return View(doc);
    }

    private async Task PopulateAsync()
    {
        ViewBag.Customers = new SelectList(await _db.Customers.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
        ViewBag.Warehouses = new SelectList(await _db.Warehouses.OrderBy(w => w.Name).ToListAsync(), "Id", "Name");
        ViewBag.Products = await _db.Products.OrderBy(p => p.Name).Select(p => new { p.Id, Display = p.Sku + " — " + p.Name }).ToListAsync();
    }
}
