using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

[Authorize]
public class SuppliersController : Controller
{
    private readonly ApplicationDbContext _db;
    public SuppliersController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
        => View(await _db.Suppliers.OrderBy(c => c.Code).ToListAsync());

    public async Task<IActionResult> Create() { await PopulateAsync(); return View(new Supplier()); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Supplier model)
    {
        if (!ModelState.IsValid) { await PopulateAsync(); return View(model); }
        model.CreatedBy = User.Identity?.Name;
        _db.Suppliers.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Pemasok berhasil ditambahkan.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Suppliers.FindAsync(id);
        if (item is null) return NotFound();
        await PopulateAsync();
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Supplier model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) { await PopulateAsync(); return View(model); }
        model.UpdatedAt = DateTime.UtcNow;
        model.UpdatedBy = User.Identity?.Name;
        _db.Suppliers.Update(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Pemasok berhasil diperbarui.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Suppliers.FindAsync(id);
        if (item is null) return NotFound();
        _db.Suppliers.Remove(item);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Pemasok berhasil dihapus.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateAsync()
        => ViewBag.PaymentTerms = new SelectList(await _db.PaymentTerms.Where(t => t.IsActive).OrderBy(t => t.NetDays)
            .Select(t => new { t.Id, Display = t.Name }).ToListAsync(), "Id", "Display");
}
