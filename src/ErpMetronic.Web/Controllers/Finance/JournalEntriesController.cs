using ErpMetronic.Domain.Constants;
using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Identity;
using ErpMetronic.Infrastructure.Persistence;
using ErpMetronic.Infrastructure.Services;
using ErpMetronic.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

/// <summary>Jurnal (manual & otomatis). Manual harus seimbang (total debit = total kredit).</summary>
[Authorize(Roles = AppRoles.Administrator)]
public class JournalEntriesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IDocumentNumberService _docNumber;
    private readonly IJournalService _journal;

    public JournalEntriesController(ApplicationDbContext db, IDocumentNumberService docNumber, IJournalService journal)
    {
        _db = db;
        _docNumber = docNumber;
        _journal = journal;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.JournalEntries.Include(j => j.Lines)
            .OrderByDescending(j => j.EntryDate).ThenByDescending(j => j.Id).Take(300).ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateAsync();
        return View(new JournalEntryCreateViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(JournalEntryCreateViewModel model)
    {
        var lines = model.Lines.Where(l => l.AccountId > 0 && (l.Debit != 0 || l.Credit != 0)).ToList();
        if (lines.Count < 2) ModelState.AddModelError(string.Empty, "Minimal dua baris jurnal.");

        var totalDebit = lines.Sum(l => l.Debit);
        var totalCredit = lines.Sum(l => l.Credit);
        if (lines.Any(l => l.Debit < 0 || l.Credit < 0 || (l.Debit > 0 && l.Credit > 0)))
            ModelState.AddModelError(string.Empty, "Setiap baris hanya boleh debit ATAU kredit (tidak negatif).");
        if (totalDebit <= 0 || Math.Round(totalDebit, 2) != Math.Round(totalCredit, 2))
            ModelState.AddModelError(string.Empty, $"Jurnal tidak seimbang (Debit {totalDebit:N2} ≠ Kredit {totalCredit:N2}).");
        if (await _journal.IsPeriodClosedAsync(model.EntryDate))
            ModelState.AddModelError(string.Empty, "Periode sudah ditutup (tutup buku). Pilih tanggal setelah periode terkunci.");

        if (!ModelState.IsValid) { await PopulateAsync(); return View(model); }

        var entry = new JournalEntry
        {
            ReferenceNumber = await _docNumber.NextAsync(DocumentCodes.JournalVoucher, model.EntryDate),
            EntryDate = model.EntryDate,
            Description = model.Description,
            CreatedBy = User.Identity?.Name,
            Lines = lines.Select(l => new JournalLine { AccountId = l.AccountId, Debit = l.Debit, Credit = l.Credit, Description = l.Description }).ToList()
        };
        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Jurnal {entry.ReferenceNumber} dibuat.";
        return RedirectToAction(nameof(Details), new { id = entry.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var entry = await _db.JournalEntries.Include(j => j.Lines).ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(j => j.Id == id);
        if (entry is null) return NotFound();
        return View(entry);
    }

    private async Task PopulateAsync()
        => ViewBag.Accounts = await _db.ChartOfAccounts.Where(a => a.IsActive).OrderBy(a => a.Code)
            .Select(a => new { a.Id, Display = a.Code + " — " + a.Name }).ToListAsync();
}
