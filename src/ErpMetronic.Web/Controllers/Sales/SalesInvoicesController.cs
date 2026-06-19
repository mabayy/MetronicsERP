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

/// <summary>Faktur Penjualan (piutang) + Penerimaan pembayaran, matching terhadap pengiriman.</summary>
[Authorize]
public class SalesInvoicesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IDocumentNumberService _docNumber;

    public SalesInvoicesController(ApplicationDbContext db, IDocumentNumberService docNumber)
    {
        _db = db;
        _docNumber = docNumber;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.SalesInvoices
            .Include(i => i.Customer).Include(i => i.SalesOrder).Include(i => i.Currency).Include(i => i.Lines)
            .OrderByDescending(i => i.InvoiceDate).ThenByDescending(i => i.Id).Take(300).ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> SelectSo()
    {
        var sos = await _db.SalesOrders
            .Include(p => p.Customer).Include(p => p.Items)
            .Where(p => p.Status == SalesOrderStatus.PartiallyDelivered || p.Status == SalesOrderStatus.Delivered)
            .OrderByDescending(p => p.OrderDate).ToListAsync();

        var eligible = new List<SalesOrder>();
        foreach (var so in sos)
            if ((await InvoiceableAsync(so)).Values.Any(q => q > 0))
                eligible.Add(so);
        return View(eligible);
    }

    public async Task<IActionResult> Create(int soId)
    {
        var so = await LoadSoAsync(soId);
        if (so is null) return NotFound();

        var invoiceable = await InvoiceableAsync(so);
        if (!invoiceable.Values.Any(q => q > 0))
        {
            TempData["Error"] = "Tidak ada barang yang dapat difaktur untuk SO ini (semua sudah difaktur atau belum dikirim).";
            return RedirectToAction(nameof(SelectSo));
        }

        ViewBag.SalesOrder = so;
        ViewBag.Invoiceable = invoiceable;
        var model = new SalesInvoiceCreateViewModel
        {
            SalesOrderId = so.Id,
            Lines = so.Items.Where(i => invoiceable.TryGetValue(i.ProductId, out var q) && q > 0)
                .Select(i => new SalesInvLineInput { ProductId = i.ProductId, Quantity = invoiceable[i.ProductId], UnitPrice = i.UnitPrice }).ToList()
        };
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SalesInvoiceCreateViewModel model)
    {
        var so = await LoadSoAsync(model.SalesOrderId);
        if (so is null) return NotFound();

        var invoiceable = await InvoiceableAsync(so);
        var lines = model.Lines.Where(l => l.ProductId > 0 && l.Quantity > 0).ToList();
        if (lines.Count == 0) ModelState.AddModelError(string.Empty, "Tidak ada baris untuk difaktur.");

        foreach (var l in lines)
        {
            var max = invoiceable.TryGetValue(l.ProductId, out var q) ? q : 0;
            if (l.Quantity > max)
            {
                var name = so.Items.FirstOrDefault(i => i.ProductId == l.ProductId)?.Product?.Name ?? "produk";
                ModelState.AddModelError(string.Empty, $"Jumlah faktur '{name}' melebihi yang dapat difaktur (maks {max}).");
            }
        }

        if (!ModelState.IsValid)
        {
            ViewBag.SalesOrder = so;
            ViewBag.Invoiceable = invoiceable;
            return View(model);
        }

        var invoice = new SalesInvoice
        {
            ReferenceNumber = await _docNumber.NextAsync(DocumentCodes.SalesInvoice, model.InvoiceDate),
            InvoiceDate = model.InvoiceDate,
            CustomerId = so.CustomerId,
            SalesOrderId = so.Id,
            CurrencyId = so.CurrencyId,
            Status = SalesInvoiceStatus.Unpaid,
            Note = model.Note,
            CreatedBy = User.Identity?.Name,
            Lines = lines.Select(l => new SalesInvoiceLine { ProductId = l.ProductId, Quantity = l.Quantity, UnitPrice = l.UnitPrice }).ToList()
        };
        _db.SalesInvoices.Add(invoice);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Faktur {invoice.ReferenceNumber} dibuat.";
        return RedirectToAction(nameof(Details), new { id = invoice.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var inv = await _db.SalesInvoices
            .Include(i => i.Customer).Include(i => i.SalesOrder).Include(i => i.Currency)
            .Include(i => i.Lines).ThenInclude(l => l.Product)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (inv is null) return NotFound();
        return View(inv);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(SalesPaymentViewModel model)
    {
        var inv = await _db.SalesInvoices.Include(i => i.Lines).Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == model.SalesInvoiceId);
        if (inv is null) return NotFound();

        if (inv.Status == SalesInvoiceStatus.Paid)
            TempData["Error"] = "Faktur sudah lunas.";
        else if (model.Amount <= 0)
            TempData["Error"] = "Jumlah harus lebih dari 0.";
        else if (model.Amount > inv.Outstanding)
            TempData["Error"] = $"Jumlah melebihi sisa tagihan ({inv.Outstanding:N2}).";
        else
        {
            _db.SalesPayments.Add(new SalesPayment
            {
                ReferenceNumber = await _docNumber.NextAsync(DocumentCodes.SalesPayment, model.PaymentDate),
                SalesInvoiceId = inv.Id,
                PaymentDate = model.PaymentDate,
                Amount = model.Amount,
                Method = model.Method,
                Note = model.Note,
                CreatedBy = User.Identity?.Name
            });
            inv.PaidAmount += model.Amount;
            inv.Status = inv.PaidAmount >= inv.Total ? SalesInvoiceStatus.Paid : SalesInvoiceStatus.PartiallyPaid;
            inv.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Penerimaan pembayaran dicatat.";
        }
        return RedirectToAction(nameof(Details), new { id = model.SalesInvoiceId });
    }

    // matching: dikirim per produk − sudah difaktur per produk
    private async Task<Dictionary<int, int>> InvoiceableAsync(SalesOrder so)
    {
        var delivered = await _db.DeliveryOrderLines
            .Where(l => l.DeliveryOrder!.SalesOrderId == so.Id)
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty);

        var invoiced = await _db.SalesInvoiceLines
            .Where(l => l.SalesInvoice!.SalesOrderId == so.Id)
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty);

        var result = new Dictionary<int, int>();
        foreach (var kv in delivered)
            result[kv.Key] = kv.Value - (invoiced.TryGetValue(kv.Key, out var inv) ? inv : 0);
        return result;
    }

    private Task<SalesOrder?> LoadSoAsync(int id) =>
        _db.SalesOrders
            .Include(p => p.Customer).Include(p => p.Currency)
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == id);
}
