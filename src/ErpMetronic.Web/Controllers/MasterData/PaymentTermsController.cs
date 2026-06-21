using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Identity;
using ErpMetronic.Infrastructure.Persistence;
using ErpMetronic.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

/// <summary>Master Termin Pembayaran (menentukan jatuh tempo faktur).</summary>
[Authorize(Roles = AppRoles.Administrator)]
public class PaymentTermsController : Controller
{
    private readonly ApplicationDbContext _db;
    public PaymentTermsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
        => View(await _db.PaymentTerms.OrderBy(t => t.NetDays).ThenBy(t => t.Code).ToListAsync());

    public IActionResult Create() => View(new PaymentTermCreateViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PaymentTermCreateViewModel model)
    {
        model.Code = (model.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (ModelState.IsValid && await _db.PaymentTerms.AnyAsync(t => t.Code == model.Code))
            ModelState.AddModelError(nameof(model.Code), "Kode termin sudah ada.");
        if (!ModelState.IsValid) return View(model);

        _db.PaymentTerms.Add(new PaymentTerm
        {
            Code = model.Code, Name = model.Name, NetDays = model.NetDays, IsActive = model.IsActive,
            IsSystem = false, CreatedBy = User.Identity?.Name
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Termin pembayaran ditambahkan.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var t = await _db.PaymentTerms.FindAsync(id);
        if (t is null) return NotFound();
        ViewBag.TermId = t.Id; ViewBag.IsSystem = t.IsSystem;
        return View(new PaymentTermCreateViewModel { Code = t.Code, Name = t.Name, NetDays = t.NetDays, IsActive = t.IsActive });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PaymentTermCreateViewModel model)
    {
        var t = await _db.PaymentTerms.FindAsync(id);
        if (t is null) return NotFound();
        model.Code = (model.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (ModelState.IsValid && await _db.PaymentTerms.AnyAsync(x => x.Code == model.Code && x.Id != id))
            ModelState.AddModelError(nameof(model.Code), "Kode termin sudah ada.");
        if (!ModelState.IsValid) { ViewBag.TermId = t.Id; ViewBag.IsSystem = t.IsSystem; return View(model); }

        if (!t.IsSystem) t.Code = model.Code;
        t.Name = model.Name; t.NetDays = model.NetDays; t.IsActive = model.IsActive;
        t.UpdatedAt = DateTime.UtcNow; t.UpdatedBy = User.Identity?.Name;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Termin pembayaran diperbarui.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var t = await _db.PaymentTerms.FindAsync(id);
        if (t is null) return NotFound();
        if (t.IsSystem)
        {
            TempData["Error"] = "Termin bawaan sistem tidak dapat dihapus.";
            return RedirectToAction(nameof(Index));
        }
        var used = await _db.Customers.AnyAsync(c => c.PaymentTermId == id)
            || await _db.Suppliers.AnyAsync(c => c.PaymentTermId == id)
            || await _db.SalesInvoices.AnyAsync(i => i.PaymentTermId == id)
            || await _db.PurchaseInvoices.AnyAsync(i => i.PaymentTermId == id);
        if (used)
        {
            TempData["Error"] = "Termin tidak dapat dihapus karena sudah dipakai. Nonaktifkan saja.";
            return RedirectToAction(nameof(Index));
        }
        _db.PaymentTerms.Remove(t);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Termin pembayaran dihapus.";
        return RedirectToAction(nameof(Index));
    }
}
