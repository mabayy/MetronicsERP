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

    public SalesOrdersController(ApplicationDbContext db, IStockService stock, IDocumentNumberService docNumber, ITaxService tax)
    {
        _db = db;
        _stock = stock;
        _docNumber = docNumber;
        _tax = tax;
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

        var (taxedItems, whtId, whtRate, whtAmount) = await BuildTaxedItemsAsync(items, model.WithholdingTaxId);
        var so = new SalesOrder
        {
            ReferenceNumber = await _docNumber.NextAsync(DocumentCodes.SalesOrder, model.OrderDate),
            OrderDate = model.OrderDate,
            CustomerId = model.CustomerId,
            WarehouseId = model.WarehouseId,
            CurrencyId = model.CurrencyId,
            Status = SalesOrderStatus.Draft,
            Note = model.Note,
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
            WithholdingTaxId = so.WithholdingTaxId,
            Items = so.Items.Select(i => new SalesLineInput { ProductId = i.ProductId, Quantity = i.Quantity, UnitPrice = i.UnitPrice, TaxId = i.TaxId }).ToList()
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
        var (taxedItems, whtId, whtRate, whtAmount) = await BuildTaxedItemsAsync(items, model.WithholdingTaxId);
        so.Items = taxedItems;
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

        foreach (var (item, qty) in toDeliver)
        {
            var result = await _stock.StockOutAsync(item.ProductId, so.WarehouseId, qty, model.DeliveryDate,
                $"Pengiriman {delivery.ReferenceNumber} (SO {so.ReferenceNumber})", User.Identity?.Name);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Gagal memposting stok keluar.");
                return View(so); // transaksi rollback otomatis (belum commit)
            }
            item.DeliveredQuantity += qty;
        }

        so.Status = so.Items.All(i => i.DeliveredQuantity >= i.Quantity)
            ? SalesOrderStatus.Delivered : SalesOrderStatus.PartiallyDelivered;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        TempData["Success"] = $"Barang dikirim ({delivery.ReferenceNumber}) & stok berkurang. Status SO: {so.Status}.";
        return RedirectToAction(nameof(Details), new { id = so.Id });
    }

    // Hitung snapshot PPN per baris & PPh (withholding) per dokumen.
    private async Task<(List<SalesOrderItem> Items, int? WhtId, decimal WhtRate, decimal WhtAmount)>
        BuildTaxedItemsAsync(List<SalesLineInput> inputs, int? withholdingTaxId)
    {
        var taxes = await _tax.GetByIdsAsync(inputs.Select(i => i.TaxId).Append(withholdingTaxId));
        var items = inputs.Select(i =>
        {
            var soi = new SalesOrderItem { ProductId = i.ProductId, Quantity = i.Quantity, UnitPrice = i.UnitPrice };
            if (i.TaxId is int tid && taxes.TryGetValue(tid, out var tx) && tx.Kind == TaxKind.ValueAdded)
            {
                soi.TaxId = tx.Id; soi.TaxRate = tx.Rate;
                soi.TaxAmount = Math.Round(soi.Quantity * soi.UnitPrice * tx.Rate / 100m, 2);
            }
            return soi;
        }).ToList();

        var dpp = items.Sum(x => x.Quantity * x.UnitPrice);
        int? whtId = null; decimal whtRate = 0, whtAmount = 0;
        if (withholdingTaxId is int wid && taxes.TryGetValue(wid, out var wtx) && wtx.Kind == TaxKind.Withholding)
        {
            whtId = wtx.Id; whtRate = wtx.Rate; whtAmount = Math.Round(dpp * wtx.Rate / 100m, 2);
        }
        return (items, whtId, whtRate, whtAmount);
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
    }
}
