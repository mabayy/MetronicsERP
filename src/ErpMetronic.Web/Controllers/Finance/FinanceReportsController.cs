using ErpMetronic.Domain.Enums;
using ErpMetronic.Infrastructure.Identity;
using ErpMetronic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

/// <summary>Laporan akuntansi: Buku Besar, Neraca Saldo, Laba Rugi, Neraca.</summary>
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

    // Laba Rugi (Income Statement): Pendapatan − Beban dalam periode.
    public async Task<IActionResult> IncomeStatement(DateTime? from, DateTime? to)
    {
        var today = DateTime.Today;
        var fromDate = from ?? new DateTime(today.Year, 1, 1);
        var toDate = to ?? today;

        var data = await _db.JournalLines.Include(l => l.Account).Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry!.EntryDate >= fromDate && l.JournalEntry.EntryDate <= toDate
                && (l.Account!.Type == AccountType.Revenue || l.Account.Type == AccountType.Expense))
            .GroupBy(l => new { l.Account!.Code, l.Account.Name, l.Account.Type })
            .Select(g => new { g.Key.Code, g.Key.Name, g.Key.Type, Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .OrderBy(x => x.Code).ToListAsync();

        var vm = new IncomeStatementVm
        {
            From = fromDate,
            To = toDate,
            Revenue = data.Where(d => d.Type == AccountType.Revenue)
                .Select(d => new ReportLine { Code = d.Code, Name = d.Name, Amount = d.Credit - d.Debit })
                .Where(r => r.Amount != 0).ToList(),
            Expense = data.Where(d => d.Type == AccountType.Expense)
                .Select(d => new ReportLine { Code = d.Code, Name = d.Name, Amount = d.Debit - d.Credit })
                .Where(r => r.Amount != 0).ToList()
        };
        return View(vm);
    }

    // Neraca (Balance Sheet): Aset = Liabilitas + Ekuitas (termasuk laba berjalan) s.d. tanggal.
    public async Task<IActionResult> BalanceSheet(DateTime? asOf)
    {
        var date = asOf ?? DateTime.Today;

        var data = await _db.JournalLines.Include(l => l.Account).Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry!.EntryDate <= date)
            .GroupBy(l => new { l.Account!.Code, l.Account.Name, l.Account.Type })
            .Select(g => new { g.Key.Code, g.Key.Name, g.Key.Type, Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .OrderBy(x => x.Code).ToListAsync();

        var revenue = data.Where(d => d.Type == AccountType.Revenue).Sum(d => d.Credit - d.Debit);
        var expense = data.Where(d => d.Type == AccountType.Expense).Sum(d => d.Debit - d.Credit);

        var vm = new BalanceSheetVm
        {
            AsOf = date,
            Assets = data.Where(d => d.Type == AccountType.Asset)
                .Select(d => new ReportLine { Code = d.Code, Name = d.Name, Amount = d.Debit - d.Credit })
                .Where(r => r.Amount != 0).ToList(),
            Liabilities = data.Where(d => d.Type == AccountType.Liability)
                .Select(d => new ReportLine { Code = d.Code, Name = d.Name, Amount = d.Credit - d.Debit })
                .Where(r => r.Amount != 0).ToList(),
            Equity = data.Where(d => d.Type == AccountType.Equity)
                .Select(d => new ReportLine { Code = d.Code, Name = d.Name, Amount = d.Credit - d.Debit })
                .Where(r => r.Amount != 0).ToList(),
            NetIncome = revenue - expense
        };
        return View(vm);
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

/// <summary>Baris laporan akun (kode, nama, nilai) untuk Laba Rugi & Neraca.</summary>
public class ReportLine
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class IncomeStatementVm
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<ReportLine> Revenue { get; set; } = new();
    public List<ReportLine> Expense { get; set; } = new();
    public decimal TotalRevenue => Revenue.Sum(r => r.Amount);
    public decimal TotalExpense => Expense.Sum(r => r.Amount);
    public decimal NetIncome => TotalRevenue - TotalExpense;
}

public class BalanceSheetVm
{
    public DateTime AsOf { get; set; }
    public List<ReportLine> Assets { get; set; } = new();
    public List<ReportLine> Liabilities { get; set; } = new();
    public List<ReportLine> Equity { get; set; } = new();
    public decimal NetIncome { get; set; }
    public decimal TotalAssets => Assets.Sum(r => r.Amount);
    public decimal TotalLiabilities => Liabilities.Sum(r => r.Amount);
    public decimal EquityPosted => Equity.Sum(r => r.Amount);
    public decimal TotalEquity => EquityPosted + NetIncome;
    public decimal TotalLiabilitiesEquity => TotalLiabilities + TotalEquity;
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
