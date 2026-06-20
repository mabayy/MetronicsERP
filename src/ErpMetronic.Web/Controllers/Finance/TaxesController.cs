using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Identity;
using ErpMetronic.Infrastructure.Persistence;
using ErpMetronic.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

/// <summary>Master Pajak (PPN/PPh) — kode pajak yang dipakai ulang pada transaksi.</summary>
[Authorize(Roles = AppRoles.Administrator)]
public class TaxesController : Controller
{
    private readonly ApplicationDbContext _db;
    public TaxesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
        => View(await _db.Taxes.OrderBy(t => t.Code).ToListAsync());

    public async Task<IActionResult> Create()
    {
        await PopulateAsync();
        return View(new TaxCreateViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaxCreateViewModel model)
    {
        model.Code = (model.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (ModelState.IsValid && await _db.Taxes.AnyAsync(t => t.Code == model.Code))
            ModelState.AddModelError(nameof(model.Code), "Kode pajak sudah ada.");
        if (ModelState.IsValid && !await _db.ChartOfAccounts.AnyAsync(a => a.Code == model.AccountCode))
            ModelState.AddModelError(nameof(model.AccountCode), "Akun GL tidak ditemukan.");
        if (!ModelState.IsValid) { await PopulateAsync(); return View(model); }

        _db.Taxes.Add(new Tax
        {
            Code = model.Code, Name = model.Name, Rate = model.Rate, Kind = model.Kind,
            AppliesTo = model.AppliesTo, AccountCode = model.AccountCode, IsActive = model.IsActive,
            IsSystem = false, CreatedBy = User.Identity?.Name
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Pajak berhasil ditambahkan.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var t = await _db.Taxes.FindAsync(id);
        if (t is null) return NotFound();
        await PopulateAsync();
        ViewBag.TaxId = t.Id; ViewBag.IsSystem = t.IsSystem;
        return View(new TaxCreateViewModel
        {
            Code = t.Code, Name = t.Name, Rate = t.Rate, Kind = t.Kind,
            AppliesTo = t.AppliesTo, AccountCode = t.AccountCode, IsActive = t.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TaxCreateViewModel model)
    {
        var t = await _db.Taxes.FindAsync(id);
        if (t is null) return NotFound();
        model.Code = (model.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (ModelState.IsValid && await _db.Taxes.AnyAsync(x => x.Code == model.Code && x.Id != id))
            ModelState.AddModelError(nameof(model.Code), "Kode pajak sudah ada.");
        if (ModelState.IsValid && !await _db.ChartOfAccounts.AnyAsync(a => a.Code == model.AccountCode))
            ModelState.AddModelError(nameof(model.AccountCode), "Akun GL tidak ditemukan.");
        if (!ModelState.IsValid) { await PopulateAsync(); ViewBag.TaxId = t.Id; ViewBag.IsSystem = t.IsSystem; return View(model); }

        // Kode & jenis pajak sistem dikunci; sisanya boleh diubah.
        if (!t.IsSystem) { t.Code = model.Code; t.Kind = model.Kind; t.AppliesTo = model.AppliesTo; }
        t.Name = model.Name; t.Rate = model.Rate; t.AccountCode = model.AccountCode; t.IsActive = model.IsActive;
        t.UpdatedAt = DateTime.UtcNow; t.UpdatedBy = User.Identity?.Name;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Pajak berhasil diperbarui.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var t = await _db.Taxes.FindAsync(id);
        if (t is null) return NotFound();
        if (t.IsSystem)
        {
            TempData["Error"] = "Pajak bawaan sistem tidak dapat dihapus.";
            return RedirectToAction(nameof(Index));
        }
        var used = await _db.PurchaseInvoiceLines.AnyAsync(l => l.TaxId == id)
            || await _db.SalesInvoiceLines.AnyAsync(l => l.TaxId == id)
            || await _db.PurchaseOrderItems.AnyAsync(l => l.TaxId == id)
            || await _db.SalesOrderItems.AnyAsync(l => l.TaxId == id)
            || await _db.PurchaseInvoices.AnyAsync(h => h.WithholdingTaxId == id)
            || await _db.SalesInvoices.AnyAsync(h => h.WithholdingTaxId == id)
            || await _db.PurchaseOrders.AnyAsync(h => h.WithholdingTaxId == id)
            || await _db.SalesOrders.AnyAsync(h => h.WithholdingTaxId == id);
        if (used)
        {
            TempData["Error"] = "Pajak tidak dapat dihapus karena sudah dipakai dokumen. Nonaktifkan saja.";
            return RedirectToAction(nameof(Index));
        }
        _db.Taxes.Remove(t);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Pajak berhasil dihapus.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateAsync()
        => ViewBag.Accounts = new SelectList(await _db.ChartOfAccounts.OrderBy(a => a.Code)
            .Select(a => new { a.Code, Display = a.Code + " — " + a.Name }).ToListAsync(), "Code", "Display");
}
