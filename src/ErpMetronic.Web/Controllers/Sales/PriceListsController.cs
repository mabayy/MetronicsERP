using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Identity;
using ErpMetronic.Infrastructure.Persistence;
using ErpMetronic.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

/// <summary>Master Daftar Harga (price list) + pengelolaan harga per produk.</summary>
[Authorize(Roles = AppRoles.Administrator)]
public class PriceListsController : Controller
{
    private readonly ApplicationDbContext _db;
    public PriceListsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
        => View(await _db.PriceLists.Include(p => p.Currency).Include(p => p.Items)
            .OrderBy(p => p.Code).ToListAsync());

    public async Task<IActionResult> Create() { await PopulateAsync(); return View(new PriceListCreateViewModel()); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PriceListCreateViewModel model)
    {
        model.Code = (model.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (ModelState.IsValid && await _db.PriceLists.AnyAsync(p => p.Code == model.Code))
            ModelState.AddModelError(nameof(model.Code), "Kode daftar harga sudah ada.");
        if (!ModelState.IsValid) { await PopulateAsync(); return View(model); }

        _db.PriceLists.Add(new PriceList { Code = model.Code, Name = model.Name, CurrencyId = model.CurrencyId, IsActive = model.IsActive, CreatedBy = User.Identity?.Name });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Daftar harga ditambahkan. Kelola harga produk lewat tombol Harga.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var p = await _db.PriceLists.FindAsync(id);
        if (p is null) return NotFound();
        await PopulateAsync();
        ViewBag.PriceListId = p.Id;
        return View(new PriceListCreateViewModel { Code = p.Code, Name = p.Name, CurrencyId = p.CurrencyId, IsActive = p.IsActive });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PriceListCreateViewModel model)
    {
        var p = await _db.PriceLists.FindAsync(id);
        if (p is null) return NotFound();
        model.Code = (model.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (ModelState.IsValid && await _db.PriceLists.AnyAsync(x => x.Code == model.Code && x.Id != id))
            ModelState.AddModelError(nameof(model.Code), "Kode daftar harga sudah ada.");
        if (!ModelState.IsValid) { await PopulateAsync(); ViewBag.PriceListId = p.Id; return View(model); }

        if (!p.IsSystem) p.Code = model.Code;
        p.Name = model.Name; p.CurrencyId = model.CurrencyId; p.IsActive = model.IsActive;
        p.UpdatedAt = DateTime.UtcNow; p.UpdatedBy = User.Identity?.Name;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Daftar harga diperbarui.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.PriceLists.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        if (p.IsSystem) { TempData["Error"] = "Daftar harga sistem tidak dapat dihapus."; return RedirectToAction(nameof(Index)); }
        if (await _db.Customers.AnyAsync(c => c.PriceListId == id))
        {
            TempData["Error"] = "Tidak dapat dihapus karena dipakai pelanggan. Nonaktifkan saja.";
            return RedirectToAction(nameof(Index));
        }
        _db.PriceListItems.RemoveRange(p.Items);
        _db.PriceLists.Remove(p);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Daftar harga dihapus.";
        return RedirectToAction(nameof(Index));
    }

    // Kelola harga produk untuk satu daftar harga.
    public async Task<IActionResult> Manage(int id)
    {
        var pl = await _db.PriceLists.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id);
        if (pl is null) return NotFound();
        var prices = pl.Items.ToDictionary(i => i.ProductId, i => i.Price);
        var products = await _db.Products.OrderBy(p => p.Name).ToListAsync();
        var vm = new PriceListManageViewModel
        {
            PriceListId = pl.Id,
            PriceListName = pl.Name,
            Items = products.Select(p => new PriceListItemInput
            {
                ProductId = p.Id,
                ProductName = p.Sku + " — " + p.Name,
                Price = prices.TryGetValue(p.Id, out var pr) ? pr : (decimal?)null
            }).ToList()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Manage(PriceListManageViewModel model)
    {
        var pl = await _db.PriceLists.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == model.PriceListId);
        if (pl is null) return NotFound();

        foreach (var input in model.Items)
        {
            var existing = pl.Items.FirstOrDefault(i => i.ProductId == input.ProductId);
            if (input.Price is decimal price && price > 0)
            {
                if (existing is null) pl.Items.Add(new PriceListItem { ProductId = input.ProductId, Price = price });
                else existing.Price = price;
            }
            else if (existing is not null)
            {
                _db.PriceListItems.Remove(existing); // kosong/0 = hapus override
            }
        }
        await _db.SaveChangesAsync();
        TempData["Success"] = "Harga daftar harga disimpan.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateAsync()
        => ViewBag.Currencies = new SelectList(await _db.Currencies.Where(c => c.IsActive).OrderByDescending(c => c.IsBaseCurrency).ThenBy(c => c.Code)
            .Select(c => new { c.Id, Display = c.Code + " — " + c.Name }).ToListAsync(), "Id", "Display");
}
