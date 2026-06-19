using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Identity;
using ErpMetronic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

[Authorize(Roles = AppRoles.Administrator)]
public class ExchangeRatesController : Controller
{
    private readonly ApplicationDbContext _db;
    public ExchangeRatesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? currencyId)
    {
        var query = _db.ExchangeRates.Include(r => r.Currency).AsQueryable();
        if (currencyId.HasValue) query = query.Where(r => r.CurrencyId == currencyId);

        ViewBag.CurrencyId = currencyId;
        ViewBag.Currencies = await CurrencySelectAsync(currencyId, includeBase: true);
        ViewBag.BaseCurrency = await _db.Currencies.FirstOrDefaultAsync(c => c.IsBaseCurrency);
        return View(await query
            .OrderByDescending(r => r.EffectiveDate).ThenBy(r => r.Currency!.Code)
            .Take(300).ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        await PopulateAsync();
        return View(new ExchangeRate { EffectiveDate = DateTime.Today });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExchangeRate model)
    {
        await ValidateAsync(model);
        if (!ModelState.IsValid) { await PopulateAsync(model.CurrencyId); return View(model); }

        model.CreatedBy = User.Identity?.Name;
        _db.ExchangeRates.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Kurs berhasil ditambahkan.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.ExchangeRates.FindAsync(id);
        if (item is null) return NotFound();
        await PopulateAsync(item.CurrencyId);
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ExchangeRate model)
    {
        if (id != model.Id) return NotFound();
        await ValidateAsync(model, id);
        if (!ModelState.IsValid) { await PopulateAsync(model.CurrencyId); return View(model); }

        model.UpdatedAt = DateTime.UtcNow;
        model.UpdatedBy = User.Identity?.Name;
        _db.ExchangeRates.Update(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Kurs berhasil diperbarui.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.ExchangeRates.FindAsync(id);
        if (item is null) return NotFound();
        _db.ExchangeRates.Remove(item);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Kurs berhasil dihapus.";
        return RedirectToAction(nameof(Index));
    }

    // ---- business rules ----
    private async Task ValidateAsync(ExchangeRate model, int? ignoreId = null)
    {
        var currency = await _db.Currencies.FindAsync(model.CurrencyId);
        if (currency is null)
            ModelState.AddModelError(nameof(model.CurrencyId), "Mata uang tidak valid.");
        else if (currency.IsBaseCurrency)
            ModelState.AddModelError(nameof(model.CurrencyId), "Mata uang dasar selalu berkurs 1 — tidak perlu kurs.");

        if (model.Rate <= 0)
            ModelState.AddModelError(nameof(model.Rate), "Kurs harus lebih dari 0.");

        var duplicate = await _db.ExchangeRates.AnyAsync(r =>
            r.CurrencyId == model.CurrencyId && r.EffectiveDate == model.EffectiveDate &&
            (ignoreId == null || r.Id != ignoreId));
        if (duplicate)
            ModelState.AddModelError(nameof(model.EffectiveDate), "Sudah ada kurs untuk mata uang & tanggal ini.");
    }

    private async Task PopulateAsync(int? selected = null)
        => ViewBag.Currencies = await CurrencySelectAsync(selected, includeBase: false);

    private async Task<SelectList> CurrencySelectAsync(int? selected, bool includeBase)
    {
        var query = _db.Currencies.Where(c => c.IsActive);
        if (!includeBase) query = query.Where(c => !c.IsBaseCurrency);
        var items = await query.OrderBy(c => c.Code)
            .Select(c => new { c.Id, Display = c.Code + " — " + c.Name })
            .ToListAsync();
        return new SelectList(items, "Id", "Display", selected);
    }
}
