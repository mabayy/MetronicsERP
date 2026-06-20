using ErpMetronic.Infrastructure.Identity;
using ErpMetronic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

/// <summary>Laporan akuntansi: Buku Besar (General Ledger) & Neraca Saldo (Trial Balance).</summary>
[Authorize(Roles = AppRoles.Administrator)]
public class FinanceReportsController : Controller
{
    private readonly ApplicationDbContext _db;
    public FinanceReportsController(ApplicationDbContext db) => _db = db;

    // Buku Besar per akun dengan saldo berjalan (debit − kredit).
    public async Task<IActionResult> GeneralLedger(int? accountId, DateTime? from, DateTime? to)
    {
        ViewBag.Accounts = new SelectList(await _db.ChartOfAccounts.OrderBy(a => a.Code)
            .Select(a => new { a.Id, Display = a.Code + " — " + a.Name }).ToListAsync(), "Id", "Display", accountId);
        ViewBag.AccountId = accountId; ViewBag.From = from; ViewBag.To = to;

        var rows = new List<LedgerRow>();
        if (accountId is null) { ViewBag.Opening = 0m; ViewBag.Closing = 0m; return View(rows); }

        var q = _db.JournalLines.Include(l => l.JournalEntry).Where(l => l.AccountId == accountId);
        var opening = from.HasValue
            ? await q.Where(l => l.JournalEntry!.EntryDate < from.Value).SumAsync(l => (decimal?)(l.Debit - l.Credit)) ?? 0
            : 0m;

        var inRange = await q
            .Where(l => (!from.HasValue || l.JournalEntry!.EntryDate >= from.Value) && (!to.HasValue || l.JournalEntry!.EntryDate <= to.Value))
            .OrderBy(l => l.JournalEntry!.EntryDate).ThenBy(l => l.Id).ToListAsync();

        var running = opening;
        foreach (var l in inRange)
        {
            running += l.Debit - l.Credit;
            rows.Add(new LedgerRow
            {
                Date = l.JournalEntry!.EntryDate,
                Reference = l.JournalEntry.ReferenceNumber,
                Description = l.Description ?? l.JournalEntry.Description,
                Debit = l.Debit,
                Credit = l.Credit,
                Balance = running
            });
        }
        ViewBag.Opening = opening; ViewBag.Closing = running;
        return View(rows);
    }

    // Neraca Saldo: saldo tiap akun s.d. tanggal tertentu.
    public async Task<IActionResult> TrialBalance(DateTime? asOf)
    {
        var date = asOf ?? DateTime.Today;
        var data = await _db.JournalLines.Include(l => l.Account).Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry!.EntryDate <= date)
            .GroupBy(l => new { l.AccountId, l.Account!.Code, l.Account.Name })
            .Select(g => new { g.Key.Code, g.Key.Name, Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .OrderBy(x => x.Code).ToListAsync();

        var rows = data.Select(d =>
        {
            var net = d.Debit - d.Credit;
            return new TrialBalanceRow
            {
                Code = d.Code,
                Name = d.Name,
                Debit = net > 0 ? net : 0,
                Credit = net < 0 ? -net : 0
            };
        }).Where(r => r.Debit != 0 || r.Credit != 0).ToList();

        ViewBag.AsOf = date;
        ViewBag.TotalDebit = rows.Sum(r => r.Debit);
        ViewBag.TotalCredit = rows.Sum(r => r.Credit);
        return View(rows);
    }
}

public class LedgerRow
{
    public DateTime Date { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}

public class TrialBalanceRow
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

/// <summary>Baris laporan umur piutang/hutang per mitra (pelanggan/pemasok).</summary>
public class AgingRow
{
    public string Partner { get; set; } = string.Empty;
    public decimal Current { get; set; }   // 0–30 hari
    public decimal Bucket31 { get; set; }   // 31–60
    public decimal Bucket61 { get; set; }   // 61–90
    public decimal Over90 { get; set; }     // > 90
    public decimal Total => Current + Bucket31 + Bucket61 + Over90;
}
