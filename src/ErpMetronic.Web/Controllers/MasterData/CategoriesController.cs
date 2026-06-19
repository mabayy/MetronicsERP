using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

[Authorize]
public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _db;
    public CategoriesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
        => View(await _db.Categories.OrderBy(c => c.Code).ToListAsync());

    public IActionResult Create() => View(new Category());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category model)
    {
        if (!ModelState.IsValid) return View(model);
        model.CreatedBy = User.Identity?.Name;
        _db.Categories.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Kategori berhasil ditambahkan.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Categories.FindAsync(id);
        if (item is null) return NotFound();
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);
        model.UpdatedAt = DateTime.UtcNow;
        model.UpdatedBy = User.Identity?.Name;
        _db.Categories.Update(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Kategori berhasil diperbarui.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Categories.FindAsync(id);
        if (item is null) return NotFound();
        _db.Categories.Remove(item);
        try
        {
            await _db.SaveChangesAsync();
            TempData["Success"] = "Kategori berhasil dihapus.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Kategori tidak dapat dihapus karena masih dipakai produk.";
        }
        return RedirectToAction(nameof(Index));
    }
}
