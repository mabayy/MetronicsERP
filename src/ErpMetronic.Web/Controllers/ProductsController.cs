using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

[Authorize]
public class ProductsController : Controller
{
    private readonly ApplicationDbContext _db;
    public ProductsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? search)
    {
        var query = _db.Products.Include(p => p.Category).Include(p => p.UnitOfMeasure).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || p.Sku.Contains(search));

        ViewBag.Search = search;
        return View(await query.OrderBy(p => p.Name).ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();
        return View(new Product());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            return View(model);
        }
        model.CreatedBy = User.Identity?.Name;
        _db.Products.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Produk berhasil ditambahkan.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Products.FindAsync(id);
        if (item is null) return NotFound();
        await PopulateDropdownsAsync(item);
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            return View(model);
        }
        model.UpdatedAt = DateTime.UtcNow;
        model.UpdatedBy = User.Identity?.Name;
        _db.Products.Update(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Produk berhasil diperbarui.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var item = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.UnitOfMeasure)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (item is null) return NotFound();
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Products.FindAsync(id);
        if (item is null) return NotFound();
        _db.Products.Remove(item);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Produk berhasil dihapus.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync(Product? model = null)
    {
        ViewBag.Categories = new SelectList(await _db.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", model?.CategoryId);
        ViewBag.Units = new SelectList(await _db.UnitOfMeasures.OrderBy(u => u.Name).ToListAsync(), "Id", "Name", model?.UnitOfMeasureId);
    }
}
