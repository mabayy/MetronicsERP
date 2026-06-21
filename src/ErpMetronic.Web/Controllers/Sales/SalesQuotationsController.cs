using ErpMetronic.Domain.Constants;
using ErpMetronic.Domain.Entities;
using ErpMetronic.Domain.Enums;
using ErpMetronic.Infrastructure.Persistence;
using ErpMetronic.Infrastructure.Services;
using ErpMetronic.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

/// <summary>Penawaran Penjualan (Sales Quotation): Draft → Terkirim → Diterima → konversi ke Sales Order.</summary>
[Authorize]
public class SalesQuotationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IDocumentNumberService _docNumber;
    private readonly ITaxService _tax;

    public SalesQuotationsController(ApplicationDbContext db, IDocumentNumberService docNumber, ITaxService tax)
    {
        _db = db;
        _docNumber = docNumber;
        _tax = tax;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.SalesQuotations.Include(q => q.Customer).Include(q => q.Currency).Include(q => q.Items)
            .OrderByDescending(q => q.QuotationDate).ThenByDescending(q => q.Id).Take(300).ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateAsync();
        return View(new SalesQuotationCreateViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SalesQuotationCreateViewModel model)
    {
        var items = model.Items.Where(i => i.ProductId > 0 && i.Quantity > 0).ToList();
        if (items.Count == 0) ModelState.AddModelError(string.Empty, "Minimal satu baris produk dengan jumlah > 0.");
        if (!ModelState.IsValid) { await PopulateAsync(); return View(model); }

        var (taxedItems, hdrPct, hdrAmt, whtId, whtRate, whtAmount) = await BuildTaxedItemsAsync(items, model.HeaderDiscountPercent, model.WithholdingTaxId);
        var q = new SalesQuotation
        {
            ReferenceNumber = await _docNumber.NextAsync(DocumentCodes.SalesQuotation, model.QuotationDate),
            QuotationDate = model.QuotationDate,
            ValidUntil = model.ValidUntil,
            CustomerId = model.CustomerId,
            WarehouseId = model.WarehouseId,
            CurrencyId = model.CurrencyId,
            Status = SalesQuotationStatus.Draft,
            Note = model.Note,
            HeaderDiscountPercent = hdrPct,
            HeaderDiscountAmount = hdrAmt,
            WithholdingTaxId = whtId,
            WithholdingRate = whtRate,
            WithholdingAmount = whtAmount,
            CreatedBy = User.Identity?.Name,
            Items = taxedItems
        };
        _db.SalesQuotations.Add(q);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Penawaran {q.ReferenceNumber} dibuat (Draft).";
        return RedirectToAction(nameof(Details), new { id = q.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var q = await LoadAsync(id);
        if (q is null) return NotFound();
        if (q.Status != SalesQuotationStatus.Draft)
        {
            TempData["Error"] = "Hanya penawaran Draft yang dapat diubah.";
            return RedirectToAction(nameof(Details), new { id });
        }
        await PopulateAsync();
        ViewBag.QuotationId = q.Id; ViewBag.ReferenceNumber = q.ReferenceNumber;
        return View(new SalesQuotationCreateViewModel
        {
            CustomerId = q.CustomerId, WarehouseId = q.WarehouseId, CurrencyId = q.CurrencyId,
            QuotationDate = q.QuotationDate, ValidUntil = q.ValidUntil, Note = q.Note,
            HeaderDiscountPercent = q.HeaderDiscountPercent, WithholdingTaxId = q.WithholdingTaxId,
            Items = q.Items.Select(i => new SalesQuotationLineInput { ProductId = i.ProductId, Quantity = i.Quantity, UnitPrice = i.UnitPrice, DiscountPercent = i.DiscountPercent, TaxId = i.TaxId }).ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SalesQuotationCreateViewModel model)
    {
        var q = await _db.SalesQuotations.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id);
        if (q is null) return NotFound();
        if (q.Status != SalesQuotationStatus.Draft)
        {
            TempData["Error"] = "Hanya penawaran Draft yang dapat diubah.";
            return RedirectToAction(nameof(Details), new { id });
        }
        var items = model.Items.Where(i => i.ProductId > 0 && i.Quantity > 0).ToList();
        if (items.Count == 0) ModelState.AddModelError(string.Empty, "Minimal satu baris produk dengan jumlah > 0.");
        if (!ModelState.IsValid) { await PopulateAsync(); ViewBag.QuotationId = q.Id; ViewBag.ReferenceNumber = q.ReferenceNumber; return View(model); }

        q.CustomerId = model.CustomerId; q.WarehouseId = model.WarehouseId; q.CurrencyId = model.CurrencyId;
        q.QuotationDate = model.QuotationDate; q.ValidUntil = model.ValidUntil; q.Note = model.Note;
        q.UpdatedAt = DateTime.UtcNow; q.UpdatedBy = User.Identity?.Name;
        _db.SalesQuotationItems.RemoveRange(q.Items);
        var (taxedItems, hdrPct, hdrAmt, whtId, whtRate, whtAmount) = await BuildTaxedItemsAsync(items, model.HeaderDiscountPercent, model.WithholdingTaxId);
        q.Items = taxedItems;
        q.HeaderDiscountPercent = hdrPct; q.HeaderDiscountAmount = hdrAmt;
        q.WithholdingTaxId = whtId; q.WithholdingRate = whtRate; q.WithholdingAmount = whtAmount;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Penawaran {q.ReferenceNumber} diperbarui.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var q = await _db.SalesQuotations
            .Include(x => x.Customer).Include(x => x.Warehouse).Include(x => x.Currency).Include(x => x.WithholdingTax)
            .Include(x => x.Items).ThenInclude(i => i.Product)
            .Include(x => x.Items).ThenInclude(i => i.Tax)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (q is null) return NotFound();
        return View(q);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(int id) => await Transition(id, SalesQuotationStatus.Draft, SalesQuotationStatus.Sent, "Penawaran ditandai Terkirim.");

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(int id) => await Transition(id, SalesQuotationStatus.Sent, SalesQuotationStatus.Accepted, "Penawaran diterima pelanggan.");

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id) => await Transition(id, SalesQuotationStatus.Sent, SalesQuotationStatus.Rejected, "Penawaran ditolak.");

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var q = await _db.SalesQuotations.FindAsync(id);
        if (q is null) return NotFound();
        if (q.Status != SalesQuotationStatus.Draft)
        {
            TempData["Error"] = "Hanya penawaran Draft yang dapat dihapus.";
            return RedirectToAction(nameof(Details), new { id });
        }
        _db.SalesQuotations.Remove(q);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Penawaran dihapus.";
        return RedirectToAction(nameof(Index));
    }

    // Konversi penawaran (Diterima) menjadi Sales Order — menyalin baris, diskon & pajak.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConvertToSo(int id)
    {
        var q = await LoadAsync(id);
        if (q is null) return NotFound();
        if (q.Status != SalesQuotationStatus.Accepted)
        {
            TempData["Error"] = "Hanya penawaran berstatus Diterima yang dapat dikonversi.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (q.ConvertedSalesOrderId is int existing)
        {
            TempData["Error"] = "Penawaran ini sudah dikonversi.";
            return RedirectToAction("Details", "SalesOrders", new { id = existing });
        }

        var so = new SalesOrder
        {
            ReferenceNumber = await _docNumber.NextAsync(DocumentCodes.SalesOrder, DateTime.Today),
            OrderDate = DateTime.Today,
            CustomerId = q.CustomerId,
            WarehouseId = q.WarehouseId,
            CurrencyId = q.CurrencyId,
            Status = SalesOrderStatus.Draft,
            Note = $"Dari Penawaran {q.ReferenceNumber}" + (string.IsNullOrEmpty(q.Note) ? "" : $" — {q.Note}"),
            HeaderDiscountPercent = q.HeaderDiscountPercent,
            HeaderDiscountAmount = q.HeaderDiscountAmount,
            WithholdingTaxId = q.WithholdingTaxId,
            WithholdingRate = q.WithholdingRate,
            WithholdingAmount = q.WithholdingAmount,
            CreatedBy = User.Identity?.Name,
            Items = q.Items.Select(i => new SalesOrderItem
            {
                ProductId = i.ProductId, Quantity = i.Quantity, UnitPrice = i.UnitPrice,
                DiscountPercent = i.DiscountPercent, TaxId = i.TaxId, TaxRate = i.TaxRate, TaxAmount = i.TaxAmount
            }).ToList()
        };
        _db.SalesOrders.Add(so);
        await _db.SaveChangesAsync();
        q.ConvertedSalesOrderId = so.Id;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Penawaran {q.ReferenceNumber} dikonversi → Sales Order {so.ReferenceNumber}.";
        return RedirectToAction("Details", "SalesOrders", new { id = so.Id });
    }

    // ---- helpers ----
    private async Task<IActionResult> Transition(int id, SalesQuotationStatus from, SalesQuotationStatus to, string msg)
    {
        var q = await _db.SalesQuotations.FindAsync(id);
        if (q is null) return NotFound();
        if (q.Status != from)
        {
            TempData["Error"] = "Status penawaran tidak sesuai untuk aksi ini.";
            return RedirectToAction(nameof(Details), new { id });
        }
        q.Status = to;
        await _db.SaveChangesAsync();
        TempData["Success"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }

    private Task<SalesQuotation?> LoadAsync(int id) =>
        _db.SalesQuotations.Include(q => q.Items).ThenInclude(i => i.Product).FirstOrDefaultAsync(q => q.Id == id);

    private async Task<(List<SalesQuotationItem> Items, decimal HdrPct, decimal HdrAmt, int? WhtId, decimal WhtRate, decimal WhtAmount)>
        BuildTaxedItemsAsync(List<SalesQuotationLineInput> inputs, decimal headerDiscountPercent, int? withholdingTaxId)
    {
        var taxes = await _tax.GetByIdsAsync(inputs.Select(i => i.TaxId).Append(withholdingTaxId));
        var items = inputs.Select(i => new SalesQuotationItem
        {
            ProductId = i.ProductId, Quantity = i.Quantity, UnitPrice = i.UnitPrice, DiscountPercent = i.DiscountPercent
        }).ToList();

        var nets = items.Select(x => x.LineNet).ToList();
        var s = nets.Sum();
        var hdrPct = headerDiscountPercent;
        var hdrAmt = TaxMath.R2(s * hdrPct / 100m);

        for (int k = 0; k < items.Count; k++)
        {
            var alloc = s > 0 ? TaxMath.R2(hdrAmt * nets[k] / s) : 0m;
            var taxable = nets[k] - alloc;
            if (inputs[k].TaxId is int tid && taxes.TryGetValue(tid, out var tx) && tx.Kind == TaxKind.ValueAdded)
            {
                items[k].TaxId = tx.Id; items[k].TaxRate = tx.Rate;
                items[k].TaxAmount = TaxMath.R2(taxable * tx.Rate / 100m);
            }
        }

        var dpp = s - hdrAmt;
        int? whtId = null; decimal whtRate = 0, whtAmount = 0;
        if (withholdingTaxId is int wid && taxes.TryGetValue(wid, out var wtx) && wtx.Kind == TaxKind.Withholding)
        {
            whtId = wtx.Id; whtRate = wtx.Rate; whtAmount = TaxMath.R2(dpp * wtx.Rate / 100m);
        }
        return (items, hdrPct, hdrAmt, whtId, whtRate, whtAmount);
    }

    private async Task PopulateAsync()
    {
        ViewBag.Customers = new SelectList(await _db.Customers.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
        ViewBag.Warehouses = new SelectList(await _db.Warehouses.OrderBy(w => w.Name).ToListAsync(), "Id", "Name");
        ViewBag.Currencies = new SelectList(await _db.Currencies.Where(c => c.IsActive).OrderByDescending(c => c.IsBaseCurrency).ThenBy(c => c.Code)
            .Select(c => new { c.Id, Display = c.Code + " — " + c.Name }).ToListAsync(), "Id", "Display");
        ViewBag.Products = await _db.Products.OrderBy(p => p.Name)
            .Select(p => new { p.Id, Display = p.Sku + " — " + p.Name }).ToListAsync();
        ViewBag.VatTaxes = await _tax.GetVatTaxesAsync(TaxApplicability.Sales);
        ViewBag.WhtTaxes = await _tax.GetWithholdingTaxesAsync(TaxApplicability.Sales);

        // Auto-isi harga dari daftar harga pelanggan (sama seperti Sales Order).
        ViewBag.DefaultPrices = await _db.Products.ToDictionaryAsync(p => p.Id, p => p.SellingPrice);
        ViewBag.CustomerPriceList = await _db.Customers.Where(c => c.PriceListId != null)
            .ToDictionaryAsync(c => c.Id, c => c.PriceListId!.Value);
        ViewBag.PriceListPrices = (await _db.PriceListItems.ToListAsync())
            .GroupBy(i => i.PriceListId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.ProductId, x => x.Price));
    }
}
