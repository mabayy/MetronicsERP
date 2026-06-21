using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Identity;
using ErpMetronic.Infrastructure.Persistence;
using ErpMetronic.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

/// <summary>Tutup Buku — menutup/membuka tahun fiskal & mengunci periode.</summary>
[Authorize(Roles = AppRoles.Administrator)]
public class FiscalYearsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IJournalService _journal;

    public FiscalYearsController(ApplicationDbContext db, IJournalService journal)
    {
        _db = db;
        _journal = journal;
    }

    public async Task<IActionResult> Index()
    {
        var dataYears = await _db.JournalEntries.Select(j => j.EntryDate.Year).Distinct().ToListAsync();
        var fyMap = await _db.FiscalYears.ToDictionaryAsync(f => f.Year);
        var years = dataYears.Union(fyMap.Keys).ToHashSet();
        if (years.Count == 0) years.Add(DateTime.Today.Year);

        var rows = years.OrderByDescending(y => y).Select(y => new FiscalYearRow
        {
            Year = y,
            Status = fyMap.TryGetValue(y, out var f) ? f.Status : FiscalYearStatus.Open,
            ClosedAt = fyMap.TryGetValue(y, out var f2) ? f2.ClosedAt : null,
            HasData = dataYears.Contains(y)
        }).ToList();

        ViewBag.LockDate = await _journal.GetLockDateAsync();
        return View(rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int year)
    {
        var (ok, error) = await _journal.CloseFiscalYearAsync(year, User.Identity?.Name);
        if (ok) TempData["Success"] = $"Tahun {year} ditutup. Jurnal penutup diposting & periode dikunci.";
        else TempData["Error"] = error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reopen(int year)
    {
        var (ok, error) = await _journal.ReopenFiscalYearAsync(year, User.Identity?.Name);
        if (ok) TempData["Success"] = $"Tahun {year} dibuka kembali. Jurnal penutup dihapus.";
        else TempData["Error"] = error;
        return RedirectToAction(nameof(Index));
    }
}

public class FiscalYearRow
{
    public int Year { get; set; }
    public FiscalYearStatus Status { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool HasData { get; set; }
}
