using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

[Authorize]
public class WarehousesController : Controller
{
    private readonly ApplicationDbContext _db;
    public WarehousesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
        => View(await _db.Warehouses.OrderBy(c => c.Code).ToListAsync());

    public IActionResult Create() => View(new Warehouse());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Warehouse model)
    {
        if (!ModelState.IsValid) return View(model);
        model.CreatedBy = User.Identity?.Name;
        _db.Warehouses.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Gudang berhasil ditambahkan.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Warehouses.FindAsync(id);
        if (item is null) return NotFound();
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Warehouse model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);
        model.UpdatedAt = DateTime.UtcNow;
        model.UpdatedBy = User.Identity?.Name;
        _db.Warehouses.Update(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Gudang berhasil diperbarui.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Warehouses.FindAsync(id);
        if (item is null) return NotFound();
        _db.Warehouses.Remove(item);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Gudang berhasil dihapus.";
        return RedirectToAction(nameof(Index));
    }
}
