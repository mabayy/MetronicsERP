using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Identity;
using ErpMetronic.Infrastructure.Persistence;
using ErpMetronic.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

/// <summary>Master Akun Kas/Bank — tiap akun terhubung ke akun GL untuk posting pembayaran.</summary>
[Authorize(Roles = AppRoles.Administrator)]
public class CashBankAccountsController : Controller
{
    private readonly ApplicationDbContext _db;
    public CashBankAccountsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
        => View(await _db.CashBankAccounts.OrderBy(a => a.Kind).ThenBy(a => a.Code).ToListAsync());

    public async Task<IActionResult> Create()
    {
        await PopulateAsync();
        return View(new CashBankAccountCreateViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CashBankAccountCreateViewModel model)
    {
        model.Code = (model.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (ModelState.IsValid && await _db.CashBankAccounts.AnyAsync(a => a.Code == model.Code))
            ModelState.AddModelError(nameof(model.Code), "Kode akun sudah ada.");
        if (ModelState.IsValid && !await _db.ChartOfAccounts.AnyAsync(a => a.Code == model.AccountCode))
            ModelState.AddModelError(nameof(model.AccountCode), "Akun GL tidak ditemukan.");
        if (!ModelState.IsValid) { await PopulateAsync(); return View(model); }

        _db.CashBankAccounts.Add(new CashBankAccount
        {
            Code = model.Code, Name = model.Name, Kind = model.Kind, AccountCode = model.AccountCode,
            BankName = model.BankName, AccountNumber = model.AccountNumber, IsActive = model.IsActive,
            IsSystem = false, CreatedBy = User.Identity?.Name
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Akun kas/bank ditambahkan.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var a = await _db.CashBankAccounts.FindAsync(id);
        if (a is null) return NotFound();
        await PopulateAsync();
        ViewBag.AccountId = a.Id; ViewBag.IsSystem = a.IsSystem;
        return View(new CashBankAccountCreateViewModel
        {
            Code = a.Code, Name = a.Name, Kind = a.Kind, AccountCode = a.AccountCode,
            BankName = a.BankName, AccountNumber = a.AccountNumber, IsActive = a.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CashBankAccountCreateViewModel model)
    {
        var a = await _db.CashBankAccounts.FindAsync(id);
        if (a is null) return NotFound();
        model.Code = (model.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (ModelState.IsValid && await _db.CashBankAccounts.AnyAsync(x => x.Code == model.Code && x.Id != id))
            ModelState.AddModelError(nameof(model.Code), "Kode akun sudah ada.");
        if (ModelState.IsValid && !await _db.ChartOfAccounts.AnyAsync(x => x.Code == model.AccountCode))
            ModelState.AddModelError(nameof(model.AccountCode), "Akun GL tidak ditemukan.");
        if (!ModelState.IsValid) { await PopulateAsync(); ViewBag.AccountId = a.Id; ViewBag.IsSystem = a.IsSystem; return View(model); }

        if (!a.IsSystem) { a.Code = model.Code; a.AccountCode = model.AccountCode; a.Kind = model.Kind; }
        a.Name = model.Name; a.BankName = model.BankName; a.AccountNumber = model.AccountNumber; a.IsActive = model.IsActive;
        a.UpdatedAt = DateTime.UtcNow; a.UpdatedBy = User.Identity?.Name;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Akun kas/bank diperbarui.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var a = await _db.CashBankAccounts.FindAsync(id);
        if (a is null) return NotFound();
        if (a.IsSystem)
        {
            TempData["Error"] = "Akun bawaan sistem tidak dapat dihapus.";
            return RedirectToAction(nameof(Index));
        }
        var used = await _db.SalesPayments.AnyAsync(p => p.CashBankAccountId == id)
            || await _db.PurchasePayments.AnyAsync(p => p.CashBankAccountId == id);
        if (used)
        {
            TempData["Error"] = "Akun tidak dapat dihapus karena sudah dipakai pembayaran. Nonaktifkan saja.";
            return RedirectToAction(nameof(Index));
        }
        _db.CashBankAccounts.Remove(a);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Akun kas/bank dihapus.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateAsync()
        => ViewBag.Accounts = new SelectList(await _db.ChartOfAccounts.OrderBy(a => a.Code)
            .Select(a => new { a.Code, Display = a.Code + " — " + a.Name }).ToListAsync(), "Code", "Display");
}
