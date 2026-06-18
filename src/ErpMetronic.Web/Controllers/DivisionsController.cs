using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Identity;
using ErpMetronic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

[Authorize(Roles = AppRoles.Administrator)]
public class DivisionsController : Controller
{
    private readonly ApplicationDbContext _db;
    public DivisionsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
        => View(await _db.Divisions.OrderBy(c => c.Code).ToListAsync());

    public IActionResult Create() => View(new Division());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Division model)
    {
        if (!ModelState.IsValid) return View(model);
        model.CreatedBy = User.Identity?.Name;
        _db.Divisions.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Divisi berhasil ditambahkan.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Divisions.FindAsync(id);
        if (item is null) return NotFound();
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Division model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);
        model.UpdatedAt = DateTime.UtcNow;
        model.UpdatedBy = User.Identity?.Name;
        _db.Divisions.Update(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Divisi berhasil diperbarui.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Divisions.FindAsync(id);
        if (item is null) return NotFound();
        _db.Divisions.Remove(item);
        try
        {
            await _db.SaveChangesAsync();
            TempData["Success"] = "Divisi berhasil dihapus.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Divisi tidak dapat dihapus karena masih dipakai pengguna atau menu.";
        }
        return RedirectToAction(nameof(Index));
    }
}
