using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

[Authorize]
public class CustomersController : Controller
{
    private readonly ApplicationDbContext _db;
    public CustomersController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
        => View(await _db.Customers.OrderBy(c => c.Code).ToListAsync());

    public IActionResult Create() => View(new Customer());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer model)
    {
        if (!ModelState.IsValid) return View(model);
        model.CreatedBy = User.Identity?.Name;
        _db.Customers.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Pelanggan berhasil ditambahkan.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Customers.FindAsync(id);
        if (item is null) return NotFound();
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Customer model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);
        model.UpdatedAt = DateTime.UtcNow;
        model.UpdatedBy = User.Identity?.Name;
        _db.Customers.Update(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Pelanggan berhasil diperbarui.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Customers.FindAsync(id);
        if (item is null) return NotFound();
        _db.Customers.Remove(item);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Pelanggan berhasil dihapus.";
        return RedirectToAction(nameof(Index));
    }
}
