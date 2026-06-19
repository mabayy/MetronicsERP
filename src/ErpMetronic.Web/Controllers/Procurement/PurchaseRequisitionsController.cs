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

/// <summary>Purchase Requisition (permintaan pembelian internal): Draft → Submitted → Approved/Rejected.</summary>
[Authorize]
public class PurchaseRequisitionsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IDocumentNumberService _docNumber;

    public PurchaseRequisitionsController(ApplicationDbContext db, IDocumentNumberService docNumber)
    {
        _db = db;
        _docNumber = docNumber;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.PurchaseRequisitions.Include(p => p.Lines)
            .OrderByDescending(p => p.RequestDate).ThenByDescending(p => p.Id).Take(300).ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateAsync();
        return View(new PurchaseRequisitionCreateViewModel { RequestedBy = User.Identity?.Name });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseRequisitionCreateViewModel model)
    {
        var lines = model.Lines.Where(l => l.ProductId > 0 && l.Quantity > 0).ToList();
        if (lines.Count == 0) ModelState.AddModelError(string.Empty, "Minimal satu baris produk dengan jumlah > 0.");
        if (!ModelState.IsValid) { await PopulateAsync(); return View(model); }

        var pr = new PurchaseRequisition
        {
            ReferenceNumber = await _docNumber.NextAsync(DocumentCodes.PurchaseRequisition, model.RequestDate),
            RequestDate = model.RequestDate,
            RequestedBy = model.RequestedBy,
            Department = model.Department,
            Status = PurchaseRequisitionStatus.Draft,
            Note = model.Note,
            CreatedBy = User.Identity?.Name,
            Lines = lines.Select(l => new PurchaseRequisitionLine { ProductId = l.ProductId, Quantity = l.Quantity, EstimatedPrice = l.EstimatedPrice, Note = l.Note }).ToList()
        };
        _db.PurchaseRequisitions.Add(pr);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Purchase Requisition {pr.ReferenceNumber} dibuat (Draft).";
        return RedirectToAction(nameof(Details), new { id = pr.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var pr = await Load(id);
        if (pr is null) return NotFound();
        if (pr.Status != PurchaseRequisitionStatus.Draft)
        {
            TempData["Error"] = "Hanya PR berstatus Draft yang dapat diubah.";
            return RedirectToAction(nameof(Details), new { id });
        }
        await PopulateAsync();
        ViewBag.PrId = pr.Id; ViewBag.ReferenceNumber = pr.ReferenceNumber;
        return View(new PurchaseRequisitionCreateViewModel
        {
            RequestDate = pr.RequestDate, RequestedBy = pr.RequestedBy, Department = pr.Department, Note = pr.Note,
            Lines = pr.Lines.Select(l => new PrLineInput { ProductId = l.ProductId, Quantity = l.Quantity, EstimatedPrice = l.EstimatedPrice, Note = l.Note }).ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PurchaseRequisitionCreateViewModel model)
    {
        var pr = await _db.PurchaseRequisitions.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == id);
        if (pr is null) return NotFound();
        if (pr.Status != PurchaseRequisitionStatus.Draft)
        {
            TempData["Error"] = "Hanya PR berstatus Draft yang dapat diubah.";
            return RedirectToAction(nameof(Details), new { id });
        }
        var lines = model.Lines.Where(l => l.ProductId > 0 && l.Quantity > 0).ToList();
        if (lines.Count == 0) ModelState.AddModelError(string.Empty, "Minimal satu baris produk dengan jumlah > 0.");
        if (!ModelState.IsValid) { await PopulateAsync(); ViewBag.PrId = pr.Id; ViewBag.ReferenceNumber = pr.ReferenceNumber; return View(model); }

        pr.RequestDate = model.RequestDate; pr.RequestedBy = model.RequestedBy; pr.Department = model.Department; pr.Note = model.Note;
        pr.UpdatedAt = DateTime.UtcNow; pr.UpdatedBy = User.Identity?.Name;
        _db.PurchaseRequisitionLines.RemoveRange(pr.Lines);
        pr.Lines = lines.Select(l => new PurchaseRequisitionLine { ProductId = l.ProductId, Quantity = l.Quantity, EstimatedPrice = l.EstimatedPrice, Note = l.Note }).ToList();
        await _db.SaveChangesAsync();
        TempData["Success"] = $"PR {pr.ReferenceNumber} diperbarui.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var pr = await Load(id);
        if (pr is null) return NotFound();
        ViewBag.Rfqs = await _db.RequestForQuotations.Where(r => r.PurchaseRequisitionId == id)
            .OrderBy(r => r.Id).ToListAsync();
        return View(pr);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id) => await Transition(id, PurchaseRequisitionStatus.Draft, PurchaseRequisitionStatus.Submitted, "PR diajukan (Submitted).");

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var pr = await _db.PurchaseRequisitions.FindAsync(id);
        if (pr is null) return NotFound();
        if (pr.Status != PurchaseRequisitionStatus.Submitted)
        {
            TempData["Error"] = "Hanya PR yang diajukan (Submitted) yang dapat disetujui.";
            return RedirectToAction(nameof(Details), new { id });
        }
        pr.Status = PurchaseRequisitionStatus.Approved;
        pr.ApprovedBy = User.Identity?.Name;
        pr.ApprovedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = "PR disetujui (Approved) — siap dibuatkan RFQ.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? reason)
    {
        var pr = await _db.PurchaseRequisitions.FindAsync(id);
        if (pr is null) return NotFound();
        if (pr.Status != PurchaseRequisitionStatus.Submitted)
        {
            TempData["Error"] = "Hanya PR yang diajukan yang dapat ditolak.";
            return RedirectToAction(nameof(Details), new { id });
        }
        pr.Status = PurchaseRequisitionStatus.Rejected;
        pr.RejectionReason = reason;
        await _db.SaveChangesAsync();
        TempData["Success"] = "PR ditolak.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var pr = await _db.PurchaseRequisitions.FindAsync(id);
        if (pr is null) return NotFound();
        if (pr.Status != PurchaseRequisitionStatus.Draft)
        {
            TempData["Error"] = "Hanya PR Draft yang dapat dihapus.";
            return RedirectToAction(nameof(Details), new { id });
        }
        _db.PurchaseRequisitions.Remove(pr);
        await _db.SaveChangesAsync();
        TempData["Success"] = "PR dihapus.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> Transition(int id, PurchaseRequisitionStatus from, PurchaseRequisitionStatus to, string msg)
    {
        var pr = await _db.PurchaseRequisitions.FindAsync(id);
        if (pr is null) return NotFound();
        if (pr.Status != from)
        {
            TempData["Error"] = "Status PR tidak sesuai untuk aksi ini.";
            return RedirectToAction(nameof(Details), new { id });
        }
        pr.Status = to;
        await _db.SaveChangesAsync();
        TempData["Success"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }

    private Task<PurchaseRequisition?> Load(int id) =>
        _db.PurchaseRequisitions.Include(p => p.Lines).ThenInclude(l => l.Product).FirstOrDefaultAsync(p => p.Id == id);

    private async Task PopulateAsync()
        => ViewBag.Products = await _db.Products.OrderBy(p => p.Name)
            .Select(p => new { p.Id, Display = p.Sku + " — " + p.Name }).ToListAsync();
}
