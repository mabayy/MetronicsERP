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

/// <summary>Request for Quotation: kumpulkan penawaran pemasok atas RFQ, lalu pilih pemenang.</summary>
[Authorize]
public class RequestForQuotationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IDocumentNumberService _docNumber;

    public RequestForQuotationsController(ApplicationDbContext db, IDocumentNumberService docNumber)
    {
        _db = db;
        _docNumber = docNumber;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.RequestForQuotations.Include(r => r.Lines).Include(r => r.Quotes)
            .Include(r => r.PurchaseRequisition)
            .OrderByDescending(r => r.RfqDate).ThenByDescending(r => r.Id).Take(300).ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Create(int? prId)
    {
        await PopulateAsync();
        var model = new RfqCreateViewModel();
        if (prId is int pid)
        {
            var pr = await _db.PurchaseRequisitions.Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.Id == pid && p.Status == PurchaseRequisitionStatus.Approved);
            if (pr is not null)
            {
                model.PurchaseRequisitionId = pr.Id;
                model.Note = $"Berdasarkan PR {pr.ReferenceNumber}";
                model.Lines = pr.Lines.Select(l => new RfqLineInput { ProductId = l.ProductId, Quantity = l.Quantity }).ToList();
                ViewBag.CopiedFrom = $"PR {pr.ReferenceNumber}";
            }
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RfqCreateViewModel model)
    {
        var lines = model.Lines.Where(l => l.ProductId > 0 && l.Quantity > 0).ToList();
        if (lines.Count == 0) ModelState.AddModelError(string.Empty, "Minimal satu baris produk dengan jumlah > 0.");
        if (!ModelState.IsValid) { await PopulateAsync(); return View(model); }

        var rfq = new RequestForQuotation
        {
            ReferenceNumber = await _docNumber.NextAsync(DocumentCodes.RequestForQuotation, model.RfqDate),
            RfqDate = model.RfqDate,
            PurchaseRequisitionId = model.PurchaseRequisitionId,
            Status = RequestForQuotationStatus.Draft,
            Note = model.Note,
            CreatedBy = User.Identity?.Name,
            Lines = lines.Select(l => new RfqLine { ProductId = l.ProductId, Quantity = l.Quantity }).ToList()
        };
        _db.RequestForQuotations.Add(rfq);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"RFQ {rfq.ReferenceNumber} dibuat (Draft).";
        return RedirectToAction(nameof(Details), new { id = rfq.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var rfq = await _db.RequestForQuotations
            .Include(r => r.PurchaseRequisition)
            .Include(r => r.Lines).ThenInclude(l => l.Product)
            .Include(r => r.Quotes).ThenInclude(q => q.Supplier)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (rfq is null) return NotFound();
        ViewBag.Suppliers = new SelectList(await _db.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync(), "Id", "Name");
        return View(rfq);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(int id)
    {
        var rfq = await _db.RequestForQuotations.FindAsync(id);
        if (rfq is null) return NotFound();
        if (rfq.Status != RequestForQuotationStatus.Draft)
        {
            TempData["Error"] = "Hanya RFQ Draft yang dapat dikirim.";
            return RedirectToAction(nameof(Details), new { id });
        }
        rfq.Status = RequestForQuotationStatus.Sent;
        await _db.SaveChangesAsync();
        TempData["Success"] = "RFQ ditandai terkirim (Sent).";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddQuote(RfqQuoteInput model)
    {
        var rfq = await _db.RequestForQuotations.Include(r => r.Quotes).FirstOrDefaultAsync(r => r.Id == model.RequestForQuotationId);
        if (rfq is null) return NotFound();
        if (rfq.Status == RequestForQuotationStatus.Closed)
            TempData["Error"] = "RFQ sudah ditutup.";
        else if (model.SupplierId <= 0 || model.QuotedAmount <= 0)
            TempData["Error"] = "Pemasok & nilai penawaran wajib diisi (> 0).";
        else if (rfq.Quotes.Any(q => q.SupplierId == model.SupplierId))
            TempData["Error"] = "Pemasok ini sudah memberikan penawaran.";
        else
        {
            _db.RfqQuotes.Add(new RfqQuote
            {
                RequestForQuotationId = rfq.Id,
                SupplierId = model.SupplierId,
                QuotedAmount = model.QuotedAmount,
                LeadTimeDays = model.LeadTimeDays,
                Note = model.Note,
                CreatedBy = User.Identity?.Name
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Penawaran ditambahkan.";
        }
        return RedirectToAction(nameof(Details), new { id = model.RequestForQuotationId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Award(int id, int quoteId)
    {
        var rfq = await _db.RequestForQuotations.Include(r => r.Quotes).FirstOrDefaultAsync(r => r.Id == id);
        if (rfq is null) return NotFound();
        var quote = rfq.Quotes.FirstOrDefault(q => q.Id == quoteId);
        if (quote is null) return NotFound();
        if (rfq.Status == RequestForQuotationStatus.Draft)
        {
            TempData["Error"] = "Kirim RFQ terlebih dahulu sebelum memilih pemenang.";
            return RedirectToAction(nameof(Details), new { id });
        }
        foreach (var q in rfq.Quotes) q.IsSelected = false;
        quote.IsSelected = true;
        rfq.Status = RequestForQuotationStatus.Closed;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Penawaran pemenang dipilih & RFQ ditutup.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var rfq = await _db.RequestForQuotations.FindAsync(id);
        if (rfq is null) return NotFound();
        if (rfq.Status != RequestForQuotationStatus.Draft)
        {
            TempData["Error"] = "Hanya RFQ Draft yang dapat dihapus.";
            return RedirectToAction(nameof(Details), new { id });
        }
        _db.RequestForQuotations.Remove(rfq);
        await _db.SaveChangesAsync();
        TempData["Success"] = "RFQ dihapus.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateAsync()
    {
        ViewBag.Products = await _db.Products.OrderBy(p => p.Name)
            .Select(p => new { p.Id, Display = p.Sku + " — " + p.Name }).ToListAsync();
        // Sumber "Copy From": PR yang sudah disetujui
        ViewBag.SourcePrs = await _db.PurchaseRequisitions.Where(p => p.Status == PurchaseRequisitionStatus.Approved)
            .OrderByDescending(p => p.Id).Select(p => new { p.Id, p.ReferenceNumber }).Take(100).ToListAsync();
    }
}
