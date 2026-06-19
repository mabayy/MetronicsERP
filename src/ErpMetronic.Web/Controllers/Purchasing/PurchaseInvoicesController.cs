using ErpMetronic.Domain.Constants;
using ErpMetronic.Domain.Entities;
using ErpMetronic.Domain.Enums;
using ErpMetronic.Infrastructure.Persistence;
using ErpMetronic.Infrastructure.Services;
using ErpMetronic.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

/// <summary>
/// Faktur Pembelian (hutang) + Pembayaran, dengan 3-way matching terhadap PO & penerimaan.
/// </summary>
[Authorize]
public class PurchaseInvoicesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IDocumentNumberService _docNumber;

    public PurchaseInvoicesController(ApplicationDbContext db, IDocumentNumberService docNumber)
    {
        _db = db;
        _docNumber = docNumber;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.PurchaseInvoices
            .Include(i => i.Supplier).Include(i => i.PurchaseOrder).Include(i => i.Currency).Include(i => i.Lines)
            .OrderByDescending(i => i.InvoiceDate).ThenByDescending(i => i.Id)
            .Take(300).ToListAsync();
        return View(list);
    }

    // Pilih PO yang masih ada barang diterima namun belum difaktur penuh
    public async Task<IActionResult> SelectPo()
    {
        var pos = await _db.PurchaseOrders
            .Include(p => p.Supplier).Include(p => p.Items)
            .Where(p => p.Status == PurchaseOrderStatus.PartiallyReceived || p.Status == PurchaseOrderStatus.Received)
            .OrderByDescending(p => p.OrderDate).ToListAsync();

        var eligible = new List<PurchaseOrder>();
        foreach (var po in pos)
            if ((await InvoiceableAsync(po)).Values.Any(q => q > 0))
                eligible.Add(po);
        return View(eligible);
    }

    public async Task<IActionResult> Create(int poId)
    {
        var po = await LoadPoAsync(poId);
        if (po is null) return NotFound();

        var invoiceable = await InvoiceableAsync(po);
        if (!invoiceable.Values.Any(q => q > 0))
        {
            TempData["Error"] = "Tidak ada barang yang dapat difaktur untuk PO ini (semua sudah difaktur atau belum diterima).";
            return RedirectToAction(nameof(SelectPo));
        }

        ViewBag.PurchaseOrder = po;
        ViewBag.Invoiceable = invoiceable;
        var model = new PurchaseInvoiceCreateViewModel
        {
            PurchaseOrderId = po.Id,
            Lines = po.Items.Where(i => invoiceable.TryGetValue(i.ProductId, out var q) && q > 0)
                .Select(i => new InvoiceLineInput { ProductId = i.ProductId, Quantity = invoiceable[i.ProductId], UnitPrice = i.UnitPrice }).ToList()
        };
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseInvoiceCreateViewModel model)
    {
        var po = await LoadPoAsync(model.PurchaseOrderId);
        if (po is null) return NotFound();

        var invoiceable = await InvoiceableAsync(po);
        var lines = model.Lines.Where(l => l.ProductId > 0 && l.Quantity > 0).ToList();
        if (lines.Count == 0)
            ModelState.AddModelError(string.Empty, "Tidak ada baris untuk difaktur.");

        // 3-way matching: jumlah difaktur ≤ (diterima − sudah difaktur)
        foreach (var l in lines)
        {
            var max = invoiceable.TryGetValue(l.ProductId, out var q) ? q : 0;
            if (l.Quantity > max)
            {
                var name = po.Items.FirstOrDefault(i => i.ProductId == l.ProductId)?.Product?.Name ?? "produk";
                ModelState.AddModelError(string.Empty, $"Jumlah faktur '{name}' melebihi yang dapat difaktur (maks {max}).");
            }
        }

        if (!ModelState.IsValid)
        {
            ViewBag.PurchaseOrder = po;
            ViewBag.Invoiceable = invoiceable;
            return View(model);
        }

        var invoice = new PurchaseInvoice
        {
            ReferenceNumber = await _docNumber.NextAsync(DocumentCodes.PurchaseInvoice, model.InvoiceDate),
            InvoiceDate = model.InvoiceDate,
            SupplierId = po.SupplierId,
            PurchaseOrderId = po.Id,
            CurrencyId = po.CurrencyId,
            Status = PurchaseInvoiceStatus.Unpaid,
            Note = model.Note,
            CreatedBy = User.Identity?.Name,
            Lines = lines.Select(l => new PurchaseInvoiceLine { ProductId = l.ProductId, Quantity = l.Quantity, UnitPrice = l.UnitPrice }).ToList()
        };
        _db.PurchaseInvoices.Add(invoice);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Faktur {invoice.ReferenceNumber} dibuat.";
        return RedirectToAction(nameof(Details), new { id = invoice.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var inv = await _db.PurchaseInvoices
            .Include(i => i.Supplier).Include(i => i.PurchaseOrder).Include(i => i.Currency)
            .Include(i => i.Lines).ThenInclude(l => l.Product)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (inv is null) return NotFound();
        return View(inv);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(PurchasePaymentViewModel model)
    {
        var inv = await _db.PurchaseInvoices.Include(i => i.Lines).Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == model.PurchaseInvoiceId);
        if (inv is null) return NotFound();

        if (inv.Status == PurchaseInvoiceStatus.Paid)
            TempData["Error"] = "Faktur sudah lunas.";
        else if (model.Amount <= 0)
            TempData["Error"] = "Jumlah bayar harus lebih dari 0.";
        else if (model.Amount > inv.Outstanding)
            TempData["Error"] = $"Jumlah bayar melebihi sisa tagihan ({inv.Outstanding:N2}).";
        else
        {
            _db.PurchasePayments.Add(new PurchasePayment
            {
                ReferenceNumber = await _docNumber.NextAsync(DocumentCodes.PurchasePayment, model.PaymentDate),
                PurchaseInvoiceId = inv.Id,
                PaymentDate = model.PaymentDate,
                Amount = model.Amount,
                Method = model.Method,
                Note = model.Note,
                CreatedBy = User.Identity?.Name
            });
            inv.PaidAmount += model.Amount;
            inv.Status = inv.PaidAmount >= inv.Total ? PurchaseInvoiceStatus.Paid : PurchaseInvoiceStatus.PartiallyPaid;
            inv.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Pembayaran dicatat.";
        }
        return RedirectToAction(nameof(Details), new { id = model.PurchaseInvoiceId });
    }

    // ---- 3-way matching helper: diterima per produk − sudah difaktur per produk ----
    private async Task<Dictionary<int, int>> InvoiceableAsync(PurchaseOrder po)
    {
        var received = await _db.GoodsReceiptLines
            .Where(l => l.GoodsReceipt!.PurchaseOrderId == po.Id)
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty);

        var invoiced = await _db.PurchaseInvoiceLines
            .Where(l => l.PurchaseInvoice!.PurchaseOrderId == po.Id)
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty);

        var result = new Dictionary<int, int>();
        foreach (var kv in received)
            result[kv.Key] = kv.Value - (invoiced.TryGetValue(kv.Key, out var inv) ? inv : 0);
        return result;
    }

    private Task<PurchaseOrder?> LoadPoAsync(int id) =>
        _db.PurchaseOrders
            .Include(p => p.Supplier).Include(p => p.Currency)
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == id);
}
