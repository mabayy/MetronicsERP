using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Identity;
using ErpMetronic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

[Authorize(Roles = AppRoles.Administrator)]
public class CurrenciesController : Controller
{
    private readonly ApplicationDbContext _db;
    public CurrenciesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var currencies = await _db.Currencies.OrderByDescending(c => c.IsBaseCurrency).ThenBy(c => c.Code).ToListAsync();

        // Kurs terbaru per mata uang (untuk ditampilkan)
        var latest = await _db.ExchangeRates
            .GroupBy(r => r.CurrencyId)
            .Select(g => g.OrderByDescending(x => x.EffectiveDate).First())
            .ToListAsync();
        ViewBag.LatestRates = latest.ToDictionary(r => r.CurrencyId, r => r.Rate);
        return View(currencies);
    }

    public IActionResult Create() => View(new Currency());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Currency model)
    {
        NormalizeCode(model);
        if (!ModelState.IsValid) return View(model);

        if (await _db.Currencies.AnyAsync(c => c.Code == model.Code))
        {
            ModelState.AddModelError(nameof(model.Code), "Kode mata uang sudah ada.");
            return View(model);
        }

        // Aturan: mata uang baru tidak otomatis jadi base, kecuali belum ada base sama sekali.
        model.IsBaseCurrency = !await _db.Currencies.AnyAsync(c => c.IsBaseCurrency);
        model.CreatedBy = User.Identity?.Name;
        _db.Currencies.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Mata uang berhasil ditambahkan.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Currencies.FindAsync(id);
        if (item is null) return NotFound();
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Currency model)
    {
        if (id != model.Id) return NotFound();
        NormalizeCode(model);

        var existing = await _db.Currencies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (existing is null) return NotFound();

        if (!ModelState.IsValid) return View(model);

        if (await _db.Currencies.AnyAsync(c => c.Code == model.Code && c.Id != id))
        {
            ModelState.AddModelError(nameof(model.Code), "Kode mata uang sudah ada.");
            return View(model);
        }

        // Aturan: status base hanya diubah lewat aksi "Jadikan Dasar". Mata uang dasar tak boleh dinonaktifkan.
        model.IsBaseCurrency = existing.IsBaseCurrency;
        if (existing.IsBaseCurrency && !model.IsActive)
        {
            ModelState.AddModelError(nameof(model.IsActive), "Mata uang dasar tidak dapat dinonaktifkan.");
            return View(model);
        }

        model.CreatedAt = existing.CreatedAt;
        model.CreatedBy = existing.CreatedBy;
        model.UpdatedAt = DateTime.UtcNow;
        model.UpdatedBy = User.Identity?.Name;
        _db.Currencies.Update(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Mata uang berhasil diperbarui.";
        return RedirectToAction(nameof(Index));
    }

    // Aturan: tepat satu mata uang dasar. Menjadikan satu sebagai dasar akan mencabut yang lain.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetBase(int id)
    {
        var target = await _db.Currencies.FindAsync(id);
        if (target is null) return NotFound();

        if (!target.IsActive)
        {
            TempData["Error"] = "Mata uang nonaktif tidak dapat dijadikan mata uang dasar.";
            return RedirectToAction(nameof(Index));
        }

        if (target.IsBaseCurrency)
        {
            TempData["Error"] = "Mata uang ini sudah menjadi mata uang dasar.";
            return RedirectToAction(nameof(Index));
        }

        // Dua tahap dalam satu transaksi: cabut base lama dulu, baru set base baru —
        // menghindari pelanggaran indeks unik terfilter (sempat ada dua base sekaligus).
        await using var tx = await _db.Database.BeginTransactionAsync();
        var currentBase = await _db.Currencies.Where(c => c.IsBaseCurrency).ToListAsync();
        foreach (var c in currentBase) c.IsBaseCurrency = false;
        await _db.SaveChangesAsync();

        target.IsBaseCurrency = true;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        TempData["Success"] = $"{target.Code} kini menjadi mata uang dasar. Pastikan kurs mata uang lain diperbarui.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Currencies.FindAsync(id);
        if (item is null) return NotFound();

        if (item.IsBaseCurrency)
        {
            TempData["Error"] = "Mata uang dasar tidak dapat dihapus.";
            return RedirectToAction(nameof(Index));
        }

        if (await _db.Products.AnyAsync(p => p.CurrencyId == id))
        {
            TempData["Error"] = "Mata uang tidak dapat dihapus karena masih dipakai produk.";
            return RedirectToAction(nameof(Index));
        }

        _db.Currencies.Remove(item); // kurs terkait ikut terhapus (cascade)
        await _db.SaveChangesAsync();
        TempData["Success"] = "Mata uang berhasil dihapus.";
        return RedirectToAction(nameof(Index));
    }

    private static void NormalizeCode(Currency model)
        => model.Code = (model.Code ?? string.Empty).Trim().ToUpperInvariant();
}
