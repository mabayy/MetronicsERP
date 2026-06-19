using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Persistence;
using ErpMetronic.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

[Authorize]
public class ProductsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrencyService _currency;

    public ProductsController(ApplicationDbContext db, ICurrencyService currency)
    {
        _db = db;
        _currency = currency;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _db.Products.Include(p => p.Category).Include(p => p.UnitOfMeasure).Include(p => p.Currency).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || p.Sku.Contains(search));

        var products = await query.OrderBy(p => p.Name).ToListAsync();

        // Setara mata uang dasar (untuk perbandingan lintas mata uang)
        var baseCurrency = await _currency.GetBaseCurrencyAsync();
        ViewBag.BaseCurrency = baseCurrency;
        var equivalents = new Dictionary<int, decimal?>();
        if (baseCurrency is not null)
        {
            foreach (var p in products)
            {
                if (p.CurrencyId is int cid && cid != baseCurrency.Id)
                    equivalents[p.Id] = await _currency.ConvertAsync(p.SellingPrice, cid, baseCurrency.Id, DateTime.Today);
            }
        }
        ViewBag.BaseEquivalent = equivalents;
        ViewBag.Search = search;
        return View(products);
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
            .Include(p => p.Currency)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (item is null) return NotFound();

        var baseCurrency = await _currency.GetBaseCurrencyAsync();
        ViewBag.BaseCurrency = baseCurrency;
        if (baseCurrency is not null && item.CurrencyId is int cid && cid != baseCurrency.Id)
        {
            ViewBag.SellingInBase = await _currency.ConvertAsync(item.SellingPrice, cid, baseCurrency.Id, DateTime.Today);
            ViewBag.PurchaseInBase = await _currency.ConvertAsync(item.PurchasePrice, cid, baseCurrency.Id, DateTime.Today);
        }
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

        var currencies = await _db.Currencies.Where(c => c.IsActive).OrderByDescending(c => c.IsBaseCurrency).ThenBy(c => c.Code)
            .Select(c => new { c.Id, Display = c.Code + " — " + c.Name }).ToListAsync();
        var selectedCurrency = model?.CurrencyId ?? (await _currency.GetBaseCurrencyAsync())?.Id;
        ViewBag.Currencies = new SelectList(currencies, "Id", "Display", selectedCurrency);
    }
}
