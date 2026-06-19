using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Identity;
using ErpMetronic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

[Authorize(Roles = AppRoles.Administrator)]
public class PositionsController : Controller
{
    private readonly ApplicationDbContext _db;
    public PositionsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
        => View(await _db.Positions.OrderBy(c => c.Code).ToListAsync());

    public IActionResult Create() => View(new Position());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Position model)
    {
        if (!ModelState.IsValid) return View(model);
        model.CreatedBy = User.Identity?.Name;
        _db.Positions.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Posisi berhasil ditambahkan.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Positions.FindAsync(id);
        if (item is null) return NotFound();
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Position model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);
        model.UpdatedAt = DateTime.UtcNow;
        model.UpdatedBy = User.Identity?.Name;
        _db.Positions.Update(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Posisi berhasil diperbarui.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Positions.FindAsync(id);
        if (item is null) return NotFound();
        _db.Positions.Remove(item);
        try
        {
            await _db.SaveChangesAsync();
            TempData["Success"] = "Posisi berhasil dihapus.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Posisi tidak dapat dihapus karena masih dipakai pengguna atau menu.";
        }
        return RedirectToAction(nameof(Index));
    }
}
