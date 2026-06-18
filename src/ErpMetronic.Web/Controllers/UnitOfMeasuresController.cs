using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

[Authorize]
public class UnitOfMeasuresController : Controller
{
    private readonly ApplicationDbContext _db;
    public UnitOfMeasuresController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
        => View(await _db.UnitOfMeasures.OrderBy(c => c.Code).ToListAsync());

    public IActionResult Create() => View(new UnitOfMeasure());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UnitOfMeasure model)
    {
        if (!ModelState.IsValid) return View(model);
        model.CreatedBy = User.Identity?.Name;
        _db.UnitOfMeasures.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Satuan berhasil ditambahkan.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.UnitOfMeasures.FindAsync(id);
        if (item is null) return NotFound();
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UnitOfMeasure model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);
        model.UpdatedAt = DateTime.UtcNow;
        model.UpdatedBy = User.Identity?.Name;
        _db.UnitOfMeasures.Update(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Satuan berhasil diperbarui.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.UnitOfMeasures.FindAsync(id);
        if (item is null) return NotFound();
        _db.UnitOfMeasures.Remove(item);
        try
        {
            await _db.SaveChangesAsync();
            TempData["Success"] = "Satuan berhasil dihapus.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Satuan tidak dapat dihapus karena masih dipakai produk.";
        }
        return RedirectToAction(nameof(Index));
    }
}
