using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Identity;
using ErpMetronic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

/// <summary>Bagan Akun (Chart of Accounts).</summary>
[Authorize(Roles = AppRoles.Administrator)]
public class ChartOfAccountsController : Controller
{
    private readonly ApplicationDbContext _db;
    public ChartOfAccountsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
        => View(await _db.ChartOfAccounts.OrderBy(a => a.Code).ToListAsync());

    public IActionResult Create() => View(new ChartOfAccount());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ChartOfAccount model)
    {
        model.Code = (model.Code ?? string.Empty).Trim();
        if (!ModelState.IsValid) return View(model);
        if (await _db.ChartOfAccounts.AnyAsync(a => a.Code == model.Code))
        {
            ModelState.AddModelError(nameof(model.Code), "Kode akun sudah ada.");
            return View(model);
        }
        model.IsSystem = false;
        model.CreatedBy = User.Identity?.Name;
        _db.ChartOfAccounts.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Akun berhasil ditambahkan.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.ChartOfAccounts.FindAsync(id);
        if (item is null) return NotFound();
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ChartOfAccount model)
    {
        if (id != model.Id) return NotFound();
        var existing = await _db.ChartOfAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
        if (existing is null) return NotFound();
        model.Code = (model.Code ?? string.Empty).Trim();
        if (!ModelState.IsValid) return View(model);
        if (await _db.ChartOfAccounts.AnyAsync(a => a.Code == model.Code && a.Id != id))
        {
            ModelState.AddModelError(nameof(model.Code), "Kode akun sudah ada.");
            return View(model);
        }
        model.IsSystem = existing.IsSystem;
        model.CreatedAt = existing.CreatedAt;
        model.CreatedBy = existing.CreatedBy;
        model.UpdatedAt = DateTime.UtcNow;
        model.UpdatedBy = User.Identity?.Name;
        _db.ChartOfAccounts.Update(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Akun berhasil diperbarui.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.ChartOfAccounts.FindAsync(id);
        if (item is null) return NotFound();
        if (item.IsSystem)
        {
            TempData["Error"] = "Akun bawaan sistem tidak dapat dihapus.";
            return RedirectToAction(nameof(Index));
        }
        if (await _db.JournalLines.AnyAsync(l => l.AccountId == id))
        {
            TempData["Error"] = "Akun tidak dapat dihapus karena sudah dipakai jurnal.";
            return RedirectToAction(nameof(Index));
        }
        _db.ChartOfAccounts.Remove(item);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Akun berhasil dihapus.";
        return RedirectToAction(nameof(Index));
    }
}
