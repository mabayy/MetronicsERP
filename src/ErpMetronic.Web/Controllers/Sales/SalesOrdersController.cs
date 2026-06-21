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

/// <summary>Penjualan: Sales Order → Pengiriman barang (auto Stok Keluar).</summary>
[Authorize]
public class SalesOrdersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IStockService _stock;
    private readonly IDocumentNumberService _docNumber;
    private readonly ITaxService _tax;
    private readonly IJournalService _journal;

    public SalesOrdersController(ApplicationDbContext db, IStockService stock, IDocumentNumberService docNumber, ITaxService tax, IJournalService journal)
    {
        _db = db;
        _stock = stock;
        _docNumber = docNumber;
        _tax = tax;
        _journal = journal;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.SalesOrders
            .Include(p => p.Customer).Include(p => p.Currency).Include(p => p.Items)
            .OrderByDescending(p => p.OrderDate).ThenByDescending(p => p.Id)
            .Take(300).ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateAsync();
        return View(new SalesOrderCreateViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SalesOrderCreateViewModel model)
    {
        var items = model.Items.Where(i => i.ProductId > 0 && i.Quantity > 0).ToList();
        if (items.Count == 0) ModelState.AddModelError(string.Empty, "Minimal satu baris produk dengan jumlah > 0.");
        if (!ModelState.IsValid) { await PopulateAsync(); return View(model); }

        var (taxedItems, hdrPct, hdrAmt, whtId, whtRate, whtAmount) = await BuildTaxedItemsAsync(items, model.HeaderDiscountPercent, model.WithholdingTaxId);
        var so = new SalesOrder
        {
            ReferenceNumber = await _docNumber.NextAsync(DocumentCodes.SalesOrder, model.OrderDate),
            OrderDate = model.OrderDate,
            CustomerId = model.CustomerId,
            WarehouseId = model.WarehouseId,
            CurrencyId = model.CurrencyId,
            Status = SalesOrderStatus.Draft,
            Note = model.Note,
            HeaderDiscountPercent = hdrPct,
            HeaderDiscountAmount = hdrAmt,
            WithholdingTaxId = whtId,
            WithholdingRate = whtRate,
            WithholdingAmount = whtAmount,
            CreatedBy = User.Identity?.Name,
            Items = taxedItems
        };
        _db.SalesOrders.Add(so);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Sales Order {so.ReferenceNumber} dibuat (Draft).";
        return RedirectToAction(nameof(Details), new { id = so.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var so = await LoadAsync(id);
        if (so is null) return NotFound();
        if (so.Status != SalesOrderStatus.Draft)
        {
            TempData["Error"] = "Hanya SO berstatus Draft yang dapat diubah.";
            return RedirectToAction(nameof(Details), new { id });
        }
        await PopulateAsync();
        ViewBag.SalesOrderId = so.Id;
        ViewBag.ReferenceNumber = so.ReferenceNumber;
        return View(new SalesOrderCreateViewModel
        {
            CustomerId = so.CustomerId,
            WarehouseId = so.WarehouseId,
            CurrencyId = so.CurrencyId,
            OrderDate = so.OrderDate,
            Note = so.Note,
            HeaderDiscountPercent = so.HeaderDiscountPercent,
            WithholdingTaxId = so.WithholdingTaxId,
            Items = so.Items.Select(i => new SalesLineInput { ProductId = i.ProductId, Quantity = i.Quantity, UnitPrice = i.UnitPrice, DiscountPercent = i.DiscountPercent, TaxId = i.TaxId }).ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SalesOrderCreateViewModel model)
    {
        var so = await _db.SalesOrders.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id);
        if (so is null) return NotFound();
        if (so.Status != SalesOrderStatus.Draft)
        {
            TempData["Error"] = "Hanya SO berstatus Draft yang dapat diubah.";
            return RedirectToAction(nameof(Details), new { id });
        }
        var items = model.Items.Where(i => i.ProductId > 0 && i.Quantity > 0).ToList();
        if (items.Count == 0) ModelState.AddModelError(string.Empty, "Minimal satu baris produk dengan jumlah > 0.");
        if (!ModelState.IsValid)
        {
            await PopulateAsync();
            ViewBag.SalesOrderId = so.Id; ViewBag.ReferenceNumber = so.ReferenceNumber;
            return View(model);
        }

        so.CustomerId = model.CustomerId;
        so.WarehouseId = model.WarehouseId;
        so.CurrencyId = model.CurrencyId;
        so.OrderDate = model.OrderDate;
        so.Note = model.Note;
        so.UpdatedAt = DateTime.UtcNow;
        so.UpdatedBy = User.Identity?.Name;
        _db.SalesOrderItems.RemoveRange(so.Items);
        var (taxedItems, hdrPct, hdrAmt, whtId, whtRate, whtAmount) = await BuildTaxedItemsAsync(items, model.HeaderDiscountPercent, model.WithholdingTaxId);
        so.Items = taxedItems;
        so.HeaderDiscountPercent = hdrPct; so.HeaderDiscountAmount = hdrAmt;
        so.WithholdingTaxId = whtId; so.WithholdingRate = whtRate; so.WithholdingAmount = whtAmount;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Sales Order {so.ReferenceNumber} diperbarui.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var so = await LoadAsync(id);
        if (so is null) return NotFound();
        ViewBag.Deliveries = await _db.DeliveryOrders
            .Where(d => d.SalesOrderId == id).Include(d => d.Warehouse).Include(d => d.Lines)
            .OrderBy(d => d.Id).ToListAsync();
        return View(so);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        var so = await _db.SalesOrders.FindAsync(id);
        if (so is null) return NotFound();
        if (so.Status != SalesOrderStatus.Draft)
        {
            TempData["Error"] = "Hanya SO berstatus Draft yang dapat dikonfirmasi.";
            return RedirectToAction(nameof(Details), new { id });
        }
        so.Status = SalesOrderStatus.Confirmed;
        await _db.SaveChangesAsync();
        TempData["Success"] = "SO dikonfirmasi — siap dikirim.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var so = await _db.SalesOrders.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id);
        if (so is null) return NotFound();
        if (so.Status is SalesOrderStatus.Delivered or SalesOrderStatus.Cancelled || so.Items.Any(i => i.DeliveredQuantity > 0))
        {
            TempData["Error"] = "SO yang sudah mengirim barang atau selesai/batal tidak dapat dibatalkan.";
            return RedirectToAction(nameof(Details), new { id });
        }
        so.Status = SalesOrderStatus.Cancelled;
        await _db.SaveChangesAsync();
        TempData["Success"] = "SO dibatalkan.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Deliver(int id)
    {
        var so = await LoadAsync(id);
        if (so is null) return NotFound();
        if (so.Status is not (SalesOrderStatus.Confirmed or SalesOrderStatus.PartiallyDelivered))
        {
            TempData["Error"] = "Pengiriman hanya untuk SO berstatus Confirmed / Terkirim Sebagian.";
            return RedirectToAction(nameof(Details), new { id });
        }
        return View(so);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Deliver(DeliverSoViewModel model)
    {
        var so = await LoadAsync(model.SalesOrderId);
        if (so is null) return NotFound();
        if (so.Status is not (SalesOrderStatus.Confirmed or SalesOrderStatus.PartiallyDelivered))
        {
            TempData["Error"] = "SO tidak dalam status yang dapat mengirim barang.";
            return RedirectToAction(nameof(Details), new { id = so.Id });
        }

        var toDeliver = new List<(SalesOrderItem Item, int Qty)>();
        foreach (var line in model.Lines)
        {
            var item = so.Items.FirstOrDefault(i => i.Id == line.ItemId);
            if (item is null || line.DeliverQuantity <= 0) continue;
            if (line.DeliverQuantity > item.OutstandingQuantity)
                ModelState.AddModelError(string.Empty, $"Jumlah kirim '{item.Product?.Name}' melebihi sisa ({item.OutstandingQuantity}).");
            else
                toDeliver.Add((item, line.DeliverQuantity));
        }
        if (toDeliver.Count == 0 && ModelState.ErrorCount == 0)
            ModelState.AddModelError(string.Empty, "Tidak ada jumlah yang dikirim.");
        if (await _journal.IsPeriodClosedAsync(model.DeliveryDate))
            ModelState.AddModelError(string.Empty, "Periode sudah ditutup (tutup buku). Gunakan tanggal setelah periode terkunci.");
        if (!ModelState.IsValid) return View(so);

        await using var tx = await _db.Database.BeginTransactionAsync();

        var delivery = new DeliveryOrder
        {
            ReferenceNumber = await _docNumber.NextAsync(DocumentCodes.DeliveryOrder, model.DeliveryDate),
            DeliveryDate = model.DeliveryDate,
            CustomerId = so.CustomerId,
            WarehouseId = so.WarehouseId,
            SalesOrderId = so.Id,
            Note = $"Pengiriman SO {so.ReferenceNumber}",
            CreatedBy = User.Identity?.Name,
            Lines = toDeliver.Select(t => new DeliveryOrderLine { ProductId = t.Item.ProductId, Quantity = t.Qty, UnitPrice = t.Item.UnitPrice }).ToList()
        };
        _db.DeliveryOrders.Add(delivery);
        await _db.SaveChangesAsync();

        decimal cogs = 0;
        foreach (var (item, qty) in toDeliver)
        {
            var result = await _stock.StockOutAsync(item.ProductId, so.WarehouseId, qty, model.DeliveryDate,
                $"Pengiriman {delivery.ReferenceNumber} (SO {so.ReferenceNumber})", User.Identity?.Name);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Gagal memposting stok keluar.");
                return View(so); // transaksi rollback otomatis (belum commit)
            }
            cogs += qty * result.UnitCost;
            item.DeliveredQuantity += qty;
        }

        so.Status = so.Items.All(i => i.DeliveredQuantity >= i.Quantity)
            ? SalesOrderStatus.Delivered : SalesOrderStatus.PartiallyDelivered;

        await _db.SaveChangesAsync();
        // HPP otomatis (perpetual): Dr HPP / Cr Persediaan senilai biaya rata-rata yang keluar.
        await _journal.PostDeliveryCogsAsync(delivery.Id, model.DeliveryDate, delivery.ReferenceNumber, cogs, User.Identity?.Name);
        await tx.CommitAsync();
        TempData["Success"] = $"Barang dikirim ({delivery.ReferenceNumber}) & stok berkurang. Status SO: {so.Status}.";
        return RedirectToAction(nameof(Details), new { id = so.Id });
    }

    // Hitung snapshot diskon (baris + header), PPN per baris (atas neto), & PPh per dokumen.
    private async Task<(List<SalesOrderItem> Items, decimal HdrPct, decimal HdrAmt, int? WhtId, decimal WhtRate, decimal WhtAmount)>
        BuildTaxedItemsAsync(List<SalesLineInput> inputs, decimal headerDiscountPercent, int? withholdingTaxId)
    {
        var taxes = await _tax.GetByIdsAsync(inputs.Select(i => i.TaxId).Append(withholdingTaxId));
        var items = inputs.Select(i => new SalesOrderItem
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

    private Task<SalesOrder?> LoadAsync(int id) =>
        _db.SalesOrders
            .Include(p => p.Customer).Include(p => p.Warehouse).Include(p => p.Currency).Include(p => p.WithholdingTax)
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .Include(p => p.Items).ThenInclude(i => i.Tax)
            .FirstOrDefaultAsync(p => p.Id == id);

    private async Task PopulateAsync()
    {
        ViewBag.Customers = new SelectList(await _db.Customers.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
        ViewBag.Warehouses = new SelectList(await _db.Warehouses.OrderBy(w => w.Name).ToListAsync(), "Id", "Name");
        ViewBag.Currencies = new SelectList(await _db.Currencies.Where(c => c.IsActive).OrderByDescending(c => c.IsBaseCurrency).ThenBy(c => c.Code)
            .Select(c => new { c.Id, Display = c.Code + " — " + c.Name }).ToListAsync(), "Id", "Display");
        ViewBag.Products = await _db.Products.OrderBy(p => p.Name)
            .Select(p => new { p.Id, Display = p.Sku + " — " + p.Name }).ToListAsync();
        ViewBag.VatTaxes = await _tax.GetVatTaxesAsync(Domain.Enums.TaxApplicability.Sales);
        ViewBag.WhtTaxes = await _tax.GetWithholdingTaxesAsync(Domain.Enums.TaxApplicability.Sales);

        // Data harga untuk auto-isi: harga default produk, daftar harga pelanggan, & harga per daftar.
        ViewBag.DefaultPrices = await _db.Products.ToDictionaryAsync(p => p.Id, p => p.SellingPrice);
        ViewBag.CustomerPriceList = await _db.Customers.Where(c => c.PriceListId != null)
            .ToDictionaryAsync(c => c.Id, c => c.PriceListId!.Value);
        ViewBag.PriceListPrices = (await _db.PriceListItems.ToListAsync())
            .GroupBy(i => i.PriceListId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.ProductId, x => x.Price));
    }
}
