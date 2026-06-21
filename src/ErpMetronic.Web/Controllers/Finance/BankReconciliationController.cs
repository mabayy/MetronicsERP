using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Identity;
using ErpMetronic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

/// <summary>Rekonsiliasi Bank/Kas: cocokkan mutasi GL akun kas/bank dengan rekening koran.</summary>
[Authorize(Roles = AppRoles.Administrator)]
public class BankReconciliationController : Controller
{
    private readonly ApplicationDbContext _db;
    public BankReconciliationController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? accountId, DateTime? from, DateTime? to)
    {
        var today = DateTime.Today;
        var vm = new ReconcileVm
        {
            From = from ?? new DateTime(today.Year, today.Month, 1),
            To = to ?? today,
            Accounts = await _db.CashBankAccounts.Where(a => a.IsActive).OrderBy(a => a.Kind).ThenBy(a => a.Code).ToListAsync()
        };
        var acc = accountId is int aid ? vm.Accounts.FirstOrDefault(a => a.Id == aid) : vm.Accounts.FirstOrDefault();
        if (acc is null) return View(vm);
        vm.AccountId = acc.Id;
        vm.Account = acc;

        var gl = await _db.ChartOfAccounts.FirstOrDefaultAsync(a => a.Code == acc.AccountCode);
        if (gl is null) return View(vm);

        var lines = await _db.JournalLines.Include(l => l.JournalEntry)
            .Where(l => l.AccountId == gl.Id && l.JournalEntry!.EntryDate >= vm.From && l.JournalEntry.EntryDate <= vm.To)
            .OrderBy(l => l.JournalEntry!.EntryDate).ThenBy(l => l.Id).ToListAsync();
        vm.Lines = lines.Select(l => new ReconcileLine
        {
            JournalLineId = l.Id,
            Date = l.JournalEntry!.EntryDate,
            Reference = l.JournalEntry.ReferenceNumber,
            Description = l.Description ?? l.JournalEntry.Description,
            In = l.Debit,
            Out = l.Credit,
            IsReconciled = l.IsReconciled
        }).ToList();

        // Saldo buku & saldo terekonsiliasi s.d. tanggal akhir (lintas seluruh periode).
        var upTo = _db.JournalLines.Include(l => l.JournalEntry).Where(l => l.AccountId == gl.Id && l.JournalEntry!.EntryDate <= vm.To);
        vm.BookBalance = await upTo.SumAsync(l => (decimal?)(l.Debit - l.Credit)) ?? 0;
        vm.ReconciledBalance = await upTo.Where(l => l.IsReconciled).SumAsync(l => (decimal?)(l.Debit - l.Credit)) ?? 0;
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int accountId, DateTime from, DateTime to, int[] allIds, int[] reconciledIds)
    {
        var set = new HashSet<int>(reconciledIds ?? Array.Empty<int>());
        var lines = await _db.JournalLines.Where(l => allIds.Contains(l.Id)).ToListAsync();
        foreach (var l in lines)
        {
            var now = set.Contains(l.Id);
            if (now && !l.IsReconciled) { l.IsReconciled = true; l.ReconciledDate = DateTime.UtcNow; }
            else if (!now && l.IsReconciled) { l.IsReconciled = false; l.ReconciledDate = null; }
        }
        await _db.SaveChangesAsync();
        TempData["Success"] = "Rekonsiliasi disimpan.";
        return RedirectToAction(nameof(Index), new { accountId, from = from.ToString("yyyy-MM-dd"), to = to.ToString("yyyy-MM-dd") });
    }
}

public class ReconcileLine
{
    public int JournalLineId { get; set; }
    public DateTime Date { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal In { get; set; }
    public decimal Out { get; set; }
    public bool IsReconciled { get; set; }
}

public class ReconcileVm
{
    public List<CashBankAccount> Accounts { get; set; } = new();
    public int AccountId { get; set; }
    public CashBankAccount? Account { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<ReconcileLine> Lines { get; set; } = new();
    public decimal BookBalance { get; set; }
    public decimal ReconciledBalance { get; set; }
}
