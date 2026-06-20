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

/// <summary>Pembelian: Purchase Order → Penerimaan barang (auto Stok Masuk).</summary>
[Authorize]
public class PurchaseOrdersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IStockService _stock;
    private readonly IDocumentNumberService _docNumber;
    private readonly ITaxService _tax;

    public PurchaseOrdersController(ApplicationDbContext db, IStockService stock, IDocumentNumberService docNumber, ITaxService tax)
    {
        _db = db;
        _stock = stock;
        _docNumber = docNumber;
        _tax = tax;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.PurchaseOrders
            .Include(p => p.Supplier).Include(p => p.Currency).Include(p => p.Items)
            .OrderByDescending(p => p.OrderDate).ThenByDescending(p => p.Id)
            .Take(300).ToListAsync();
        return View(list);
    }

    // Create mendukung "Copy From": salin dari PR yang disetujui atau RFQ yang sudah ditutup.
    public async Task<IActionResult> Create(int? fromPr, int? fromRfq)
    {
        await PopulateAsync();
        var model = new PurchaseOrderCreateViewModel();

        if (fromPr is int prId)
        {
            var pr = await _db.PurchaseRequisitions.Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.Id == prId && p.Status == PurchaseRequisitionStatus.Approved);
            if (pr is not null)
            {
                model.Note = $"Berdasarkan PR {pr.ReferenceNumber}";
                model.Items = pr.Lines.Select(l => new PurchaseLineInput { ProductId = l.ProductId, Quantity = l.Quantity, UnitPrice = l.EstimatedPrice }).ToList();
                ViewBag.CopiedFrom = $"PR {pr.ReferenceNumber} — lengkapi pemasok & gudang.";
            }
        }
        else if (fromRfq is int rfqId)
        {
            var rfq = await _db.RequestForQuotations.Include(r => r.Lines).Include(r => r.Quotes)
                .FirstOrDefaultAsync(r => r.Id == rfqId && r.Status == RequestForQuotationStatus.Closed);
            if (rfq is not null)
            {
                var winner = rfq.Quotes.FirstOrDefault(q => q.IsSelected);
                if (winner is not null) model.SupplierId = winner.SupplierId;
                model.Note = $"Berdasarkan RFQ {rfq.ReferenceNumber}";
                model.Items = rfq.Lines.Select(l => new PurchaseLineInput { ProductId = l.ProductId, Quantity = l.Quantity, UnitPrice = 0 }).ToList();
                ViewBag.CopiedFrom = $"RFQ {rfq.ReferenceNumber} — lengkapi harga satuan & gudang.";
            }
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseOrderCreateViewModel model)
    {
        var items = model.Items.Where(i => i.ProductId > 0 && i.Quantity > 0).ToList();
        if (items.Count == 0)
            ModelState.AddModelError(string.Empty, "Minimal satu baris produk dengan jumlah > 0.");

        if (!ModelState.IsValid)
        {
            await PopulateAsync();
            return View(model);
        }

        var (taxedItems, hdrPct, hdrAmt, whtId, whtRate, whtAmount) = await BuildTaxedItemsAsync(items, model.HeaderDiscountPercent, model.WithholdingTaxId);
        var po = new PurchaseOrder
        {
            ReferenceNumber = await _docNumber.NextAsync(Domain.Constants.DocumentCodes.PurchaseOrder, model.OrderDate),
            OrderDate = model.OrderDate,
            SupplierId = model.SupplierId,
            WarehouseId = model.WarehouseId,
            CurrencyId = model.CurrencyId,
            Status = PurchaseOrderStatus.Draft,
            Note = model.Note,
            HeaderDiscountPercent = hdrPct,
            HeaderDiscountAmount = hdrAmt,
            WithholdingTaxId = whtId,
            WithholdingRate = whtRate,
            WithholdingAmount = whtAmount,
            CreatedBy = User.Identity?.Name,
            Items = taxedItems
        };
        _db.PurchaseOrders.Add(po);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Purchase Order {po.ReferenceNumber} dibuat (Draft).";
        return RedirectToAction(nameof(Details), new { id = po.Id });
    }

    // Edit hanya saat Draft
    public async Task<IActionResult> Edit(int id)
    {
        var po = await LoadAsync(id);
        if (po is null) return NotFound();
        if (po.Status != PurchaseOrderStatus.Draft)
        {
            TempData["Error"] = "Hanya PO berstatus Draft yang dapat diubah.";
            return RedirectToAction(nameof(Details), new { id });
        }
        await PopulateAsync();
        ViewBag.PurchaseOrderId = po.Id;
        ViewBag.ReferenceNumber = po.ReferenceNumber;
        return View(new PurchaseOrderCreateViewModel
        {
            SupplierId = po.SupplierId,
            WarehouseId = po.WarehouseId,
            CurrencyId = po.CurrencyId,
            OrderDate = po.OrderDate,
            Note = po.Note,
            HeaderDiscountPercent = po.HeaderDiscountPercent,
            WithholdingTaxId = po.WithholdingTaxId,
            Items = po.Items.Select(i => new PurchaseLineInput { ProductId = i.ProductId, Quantity = i.Quantity, UnitPrice = i.UnitPrice, DiscountPercent = i.DiscountPercent, TaxId = i.TaxId }).ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PurchaseOrderCreateViewModel model)
    {
        var po = await _db.PurchaseOrders.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id);
        if (po is null) return NotFound();
        if (po.Status != PurchaseOrderStatus.Draft)
        {
            TempData["Error"] = "Hanya PO berstatus Draft yang dapat diubah.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var items = model.Items.Where(i => i.ProductId > 0 && i.Quantity > 0).ToList();
        if (items.Count == 0)
            ModelState.AddModelError(string.Empty, "Minimal satu baris produk dengan jumlah > 0.");

        if (!ModelState.IsValid)
        {
            await PopulateAsync();
            ViewBag.PurchaseOrderId = po.Id;
            ViewBag.ReferenceNumber = po.ReferenceNumber;
            return View(model);
        }

        po.SupplierId = model.SupplierId;
        po.WarehouseId = model.WarehouseId;
        po.CurrencyId = model.CurrencyId;
        po.OrderDate = model.OrderDate;
        po.Note = model.Note;
        po.UpdatedAt = DateTime.UtcNow;
        po.UpdatedBy = User.Identity?.Name;

        // Ganti seluruh item (PO masih Draft, belum ada penerimaan)
        _db.PurchaseOrderItems.RemoveRange(po.Items);
        var (taxedItems, hdrPct, hdrAmt, whtId, whtRate, whtAmount) = await BuildTaxedItemsAsync(items, model.HeaderDiscountPercent, model.WithholdingTaxId);
        po.Items = taxedItems;
        po.HeaderDiscountPercent = hdrPct; po.HeaderDiscountAmount = hdrAmt;
        po.WithholdingTaxId = whtId; po.WithholdingRate = whtRate; po.WithholdingAmount = whtAmount;

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Purchase Order {po.ReferenceNumber} diperbarui.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var po = await LoadAsync(id);
        if (po is null) return NotFound();
        ViewBag.Receipts = await _db.GoodsReceipts
            .Where(g => g.PurchaseOrderId == id).Include(g => g.Warehouse)
            .OrderBy(g => g.Id).ToListAsync();
        return View(po);
    }

    // Draft → Ordered
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        var po = await _db.PurchaseOrders.FindAsync(id);
        if (po is null) return NotFound();
        if (po.Status != PurchaseOrderStatus.Draft)
        {
            TempData["Error"] = "Hanya PO berstatus Draft yang dapat dikonfirmasi.";
            return RedirectToAction(nameof(Details), new { id });
        }
        po.Status = PurchaseOrderStatus.Ordered;
        await _db.SaveChangesAsync();
        TempData["Success"] = "PO dikonfirmasi (Ordered) — siap menerima barang.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // Batal (selama belum ada penerimaan)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var po = await _db.PurchaseOrders.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id);
        if (po is null) return NotFound();

        if (po.Status is PurchaseOrderStatus.Received or PurchaseOrderStatus.Cancelled
            || po.Items.Any(i => i.ReceivedQuantity > 0))
        {
            TempData["Error"] = "PO yang sudah menerima barang atau selesai/batal tidak dapat dibatalkan.";
            return RedirectToAction(nameof(Details), new { id });
        }
        po.Status = PurchaseOrderStatus.Cancelled;
        await _db.SaveChangesAsync();
        TempData["Success"] = "PO dibatalkan.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // Form penerimaan: tampilkan baris yang masih outstanding
    public async Task<IActionResult> Receive(int id)
    {
        var po = await LoadAsync(id);
        if (po is null) return NotFound();
        if (po.Status is not (PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartiallyReceived))
        {
            TempData["Error"] = "Penerimaan hanya untuk PO berstatus Ordered / Diterima Sebagian.";
            return RedirectToAction(nameof(Details), new { id });
        }
        return View(po);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Receive(ReceivePoViewModel model)
    {
        var po = await LoadAsync(model.PurchaseOrderId);
        if (po is null) return NotFound();
        if (po.Status is not (PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartiallyReceived))
        {
            TempData["Error"] = "PO tidak dalam status yang dapat menerima barang.";
            return RedirectToAction(nameof(Details), new { id = po.Id });
        }

        // Validasi: jumlah terima per item tidak melebihi outstanding
        var toReceive = new List<(PurchaseOrderItem Item, int Qty)>();
        foreach (var line in model.Lines)
        {
            var item = po.Items.FirstOrDefault(i => i.Id == line.ItemId);
            if (item is null || line.ReceiveQuantity <= 0) continue;
            if (line.ReceiveQuantity > item.OutstandingQuantity)
            {
                ModelState.AddModelError(string.Empty, $"Jumlah terima '{item.Product?.Name}' melebihi sisa ({item.OutstandingQuantity}).");
                continue;
            }
            toReceive.Add((item, line.ReceiveQuantity));
        }

        if (toReceive.Count == 0 && ModelState.ErrorCount == 0)
            ModelState.AddModelError(string.Empty, "Tidak ada jumlah yang diterima.");

        if (!ModelState.IsValid)
            return View(po);

        await using var tx = await _db.Database.BeginTransactionAsync();

        var receipt = new GoodsReceipt
        {
            ReferenceNumber = await _docNumber.NextAsync(Domain.Constants.DocumentCodes.GoodsReceipt, model.ReceiptDate),
            ReceiptDate = model.ReceiptDate,
            SupplierId = po.SupplierId,
            WarehouseId = po.WarehouseId,
            PurchaseOrderId = po.Id,
            Note = $"Penerimaan PO {po.ReferenceNumber}",
            CreatedBy = User.Identity?.Name,
            Lines = toReceive.Select(t => new GoodsReceiptLine { ProductId = t.Item.ProductId, Quantity = t.Qty, UnitCost = t.Item.UnitPrice }).ToList()
        };
        _db.GoodsReceipts.Add(receipt);
        await _db.SaveChangesAsync();

        foreach (var (item, qty) in toReceive)
        {
            await _stock.StockInAsync(item.ProductId, po.WarehouseId, qty, model.ReceiptDate,
                $"Penerimaan {receipt.ReferenceNumber} (PO {po.ReferenceNumber})", User.Identity?.Name);
            item.ReceivedQuantity += qty;
        }

        // Perbarui status PO
        po.Status = po.Items.All(i => i.ReceivedQuantity >= i.Quantity)
            ? PurchaseOrderStatus.Received
            : PurchaseOrderStatus.PartiallyReceived;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        TempData["Success"] = $"Barang diterima ({receipt.ReferenceNumber}) & stok bertambah. Status PO: {po.Status}.";
        return RedirectToAction(nameof(Details), new { id = po.Id });
    }

    // Hitung snapshot diskon (baris + header), PPN per baris (atas neto setelah diskon),
    // & PPh (withholding) per dokumen. Diskon header dialokasikan proporsional sebelum PPN.
    private async Task<(List<PurchaseOrderItem> Items, decimal HdrPct, decimal HdrAmt, int? WhtId, decimal WhtRate, decimal WhtAmount)>
        BuildTaxedItemsAsync(List<PurchaseLineInput> inputs, decimal headerDiscountPercent, int? withholdingTaxId)
    {
        var taxes = await _tax.GetByIdsAsync(inputs.Select(i => i.TaxId).Append(withholdingTaxId));
        var items = inputs.Select(i => new PurchaseOrderItem
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
            if (inputs[k].TaxId is int tid && taxes.TryGetValue(tid, out var tx) && tx.Kind == Domain.Enums.TaxKind.ValueAdded)
            {
                items[k].TaxId = tx.Id; items[k].TaxRate = tx.Rate;
                items[k].TaxAmount = TaxMath.R2(taxable * tx.Rate / 100m);
            }
        }

        var dpp = s - hdrAmt;
        int? whtId = null; decimal whtRate = 0, whtAmount = 0;
        if (withholdingTaxId is int wid && taxes.TryGetValue(wid, out var wtx) && wtx.Kind == Domain.Enums.TaxKind.Withholding)
        {
            whtId = wtx.Id; whtRate = wtx.Rate; whtAmount = TaxMath.R2(dpp * wtx.Rate / 100m);
        }
        return (items, hdrPct, hdrAmt, whtId, whtRate, whtAmount);
    }

    // ---- helpers ----
    private Task<PurchaseOrder?> LoadAsync(int id) =>
        _db.PurchaseOrders
            .Include(p => p.Supplier).Include(p => p.Warehouse).Include(p => p.Currency).Include(p => p.WithholdingTax)
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .Include(p => p.Items).ThenInclude(i => i.Tax)
            .FirstOrDefaultAsync(p => p.Id == id);

    private async Task PopulateAsync()
    {
        ViewBag.Suppliers = new SelectList(await _db.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync(), "Id", "Name");
        ViewBag.Warehouses = new SelectList(await _db.Warehouses.OrderBy(w => w.Name).ToListAsync(), "Id", "Name");
        ViewBag.Currencies = new SelectList(await _db.Currencies.Where(c => c.IsActive).OrderByDescending(c => c.IsBaseCurrency).ThenBy(c => c.Code)
            .Select(c => new { c.Id, Display = c.Code + " — " + c.Name }).ToListAsync(), "Id", "Display");
        ViewBag.Products = await _db.Products.OrderBy(p => p.Name)
            .Select(p => new { p.Id, Display = p.Sku + " — " + p.Name }).ToListAsync();

        // Sumber "Copy From": PR disetujui & RFQ ditutup
        ViewBag.SourcePrs = await _db.PurchaseRequisitions.Where(p => p.Status == PurchaseRequisitionStatus.Approved)
            .OrderByDescending(p => p.Id).Select(p => new { p.Id, p.ReferenceNumber }).Take(100).ToListAsync();
        ViewBag.SourceRfqs = await _db.RequestForQuotations.Where(r => r.Status == RequestForQuotationStatus.Closed)
            .OrderByDescending(r => r.Id).Select(r => new { r.Id, r.ReferenceNumber }).Take(100).ToListAsync();

        // Pajak: PPN per baris & PPh (withholding) per dokumen — konteks Pembelian
        ViewBag.VatTaxes = await _tax.GetVatTaxesAsync(Domain.Enums.TaxApplicability.Purchase);
        ViewBag.WhtTaxes = await _tax.GetWithholdingTaxesAsync(Domain.Enums.TaxApplicability.Purchase);
    }
}
