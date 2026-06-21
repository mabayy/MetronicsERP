using ErpMetronic.Domain.Constants;
using ErpMetronic.Domain.Entities;
using ErpMetronic.Domain.Enums;
using ErpMetronic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Infrastructure.Services;

public class JournalService : IJournalService
{
    private readonly ApplicationDbContext _db;
    private readonly IDocumentNumberService _docNumber;
    private readonly ICurrencyService _currency;

    public JournalService(ApplicationDbContext db, IDocumentNumberService docNumber, ICurrencyService currency)
    {
        _db = db;
        _docNumber = docNumber;
        _currency = currency;
    }

    public async Task<JournalEntry?> PostAsync(DateTime date, string description, string? sourceType, int? sourceId,
        IEnumerable<(string AccountCode, decimal Debit, decimal Credit)> lines, string? user)
    {
        // Idempoten: jangan posting ganda untuk dokumen sumber yang sama.
        if (sourceType is not null && sourceId is not null &&
            await _db.JournalEntries.AnyAsync(j => j.SourceType == sourceType && j.SourceId == sourceId))
            return null;

        // Periode terkunci: tolak posting (kecuali jurnal penutup itu sendiri).
        if (sourceType != "YearEndClosing" && await IsPeriodClosedAsync(date))
            return null;

        var lineList = lines.Where(l => l.Debit != 0 || l.Credit != 0).ToList();
        if (lineList.Count == 0) return null;

        var codes = lineList.Select(l => l.AccountCode).Distinct().ToList();
        var accounts = await _db.ChartOfAccounts.Where(a => codes.Contains(a.Code)).ToDictionaryAsync(a => a.Code, a => a.Id);
        if (codes.Any(c => !accounts.ContainsKey(c))) return null; // akun belum lengkap → lewati

        var totalDebit = lineList.Sum(l => l.Debit);
        var totalCredit = lineList.Sum(l => l.Credit);
        if (totalDebit <= 0 || Math.Round(totalDebit, 2) != Math.Round(totalCredit, 2)) return null; // tidak seimbang

        var entry = new JournalEntry
        {
            ReferenceNumber = await _docNumber.NextAsync(DocumentCodes.JournalVoucher, date),
            EntryDate = date,
            Description = description,
            SourceType = sourceType,
            SourceId = sourceId,
            CreatedBy = user,
            Lines = lineList.Select(l => new JournalLine
            {
                AccountId = accounts[l.AccountCode],
                Debit = l.Debit,
                Credit = l.Credit,
                Description = description
            }).ToList()
        };
        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();
        return entry;
    }

    public async Task PostPurchaseInvoiceAsync(PurchaseInvoice invoice, string? user)
    {
        var dpp = await ToBaseAsync(invoice.Subtotal, invoice.CurrencyId, invoice.InvoiceDate);
        var vat = await ToBaseAsync(invoice.TaxTotal, invoice.CurrencyId, invoice.InvoiceDate);
        var wht = await ToBaseAsync(invoice.WithholdingAmount, invoice.CurrencyId, invoice.InvoiceDate);
        var payable = dpp + vat - wht;
        // Dr Persediaan (DPP), Dr PPN Masukan, Cr Hutang PPh, Cr Hutang Usaha (neto)
        await PostAsync(invoice.InvoiceDate, $"Faktur Pembelian {invoice.ReferenceNumber}",
            "PurchaseInvoice", invoice.Id, new[]
            {
                (AccountCodes.Inventory, dpp, 0m),
                (AccountCodes.InputVat, vat, 0m),
                (AccountCodes.WhtPayable, 0m, wht),
                (AccountCodes.AccountsPayable, 0m, payable)
            }, user);
    }

    public async Task PostPurchasePaymentAsync(PurchasePayment payment, int? currencyId, string cashAccountCode, string? user)
    {
        var amount = await ToBaseAsync(payment.Amount, currencyId, payment.PaymentDate);
        // Dr Hutang Usaha, Cr Kas/Bank (akun terpilih)
        await PostAsync(payment.PaymentDate, $"Pembayaran Pembelian {payment.ReferenceNumber}",
            "PurchasePayment", payment.Id, new[]
            {
                (AccountCodes.AccountsPayable, amount, 0m),
                (cashAccountCode, 0m, amount)
            }, user);
    }

    public async Task PostSalesInvoiceAsync(SalesInvoice invoice, string? user)
    {
        var dpp = await ToBaseAsync(invoice.Subtotal, invoice.CurrencyId, invoice.InvoiceDate);
        var vat = await ToBaseAsync(invoice.TaxTotal, invoice.CurrencyId, invoice.InvoiceDate);
        var wht = await ToBaseAsync(invoice.WithholdingAmount, invoice.CurrencyId, invoice.InvoiceDate);
        var receivable = dpp + vat - wht;
        // Dr Piutang Usaha (neto), Dr PPh Dibayar Dimuka, Cr Pendapatan, Cr PPN Keluaran
        await PostAsync(invoice.InvoiceDate, $"Faktur Penjualan {invoice.ReferenceNumber}",
            "SalesInvoice", invoice.Id, new[]
            {
                (AccountCodes.AccountsReceivable, receivable, 0m),
                (AccountCodes.PrepaidWht, wht, 0m),
                (AccountCodes.SalesRevenue, 0m, dpp),
                (AccountCodes.OutputVat, 0m, vat)
            }, user);
    }

    public async Task PostSalesPaymentAsync(SalesPayment payment, int? currencyId, string cashAccountCode, string? user)
    {
        var amount = await ToBaseAsync(payment.Amount, currencyId, payment.PaymentDate);
        // Dr Kas/Bank (akun terpilih), Cr Piutang Usaha
        await PostAsync(payment.PaymentDate, $"Penerimaan Penjualan {payment.ReferenceNumber}",
            "SalesPayment", payment.Id, new[]
            {
                (cashAccountCode, amount, 0m),
                (AccountCodes.AccountsReceivable, 0m, amount)
            }, user);
    }

    public async Task PostDeliveryCogsAsync(int deliveryId, DateTime date, string reference, decimal cogsAmount, string? user)
    {
        var amount = Math.Round(cogsAmount, 2, MidpointRounding.AwayFromZero);
        if (amount <= 0) return; // tanpa biaya rata-rata, tidak ada HPP yang diposting
        // Dr Harga Pokok Penjualan, Cr Persediaan
        await PostAsync(date, $"HPP Pengiriman {reference}", "DeliveryOrder", deliveryId, new[]
        {
            (AccountCodes.Cogs, amount, 0m),
            (AccountCodes.Inventory, 0m, amount)
        }, user);
    }

    public async Task<DateTime?> GetLockDateAsync()
    {
        var lastClosed = await _db.FiscalYears.Where(f => f.Status == FiscalYearStatus.Closed)
            .OrderByDescending(f => f.Year).Select(f => (int?)f.Year).FirstOrDefaultAsync();
        return lastClosed.HasValue ? new DateTime(lastClosed.Value, 12, 31) : null;
    }

    public async Task<bool> IsPeriodClosedAsync(DateTime date)
    {
        var lockDate = await GetLockDateAsync();
        return lockDate.HasValue && date.Date <= lockDate.Value.Date;
    }

    public async Task<(bool Ok, string? Error)> CloseFiscalYearAsync(int year, string? user)
    {
        var fy = await _db.FiscalYears.FirstOrDefaultAsync(f => f.Year == year);
        if (fy?.Status == FiscalYearStatus.Closed) return (false, $"Tahun {year} sudah ditutup.");

        // Tutup berurutan: tahun sebelumnya yang memiliki data harus sudah ditutup.
        var lockDate = await GetLockDateAsync();
        if (lockDate.HasValue && year <= lockDate.Value.Year) return (false, "Tahun ini sudah berada dalam periode terkunci.");

        if (fy is null)
        {
            fy = new FiscalYear { Year = year, Status = FiscalYearStatus.Open, CreatedBy = user };
            _db.FiscalYears.Add(fy);
            await _db.SaveChangesAsync();
        }

        var start = new DateTime(year, 1, 1);
        var end = new DateTime(year, 12, 31);
        var data = await _db.JournalLines.Include(l => l.Account).Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry!.EntryDate >= start && l.JournalEntry.EntryDate <= end
                && (l.Account!.Type == AccountType.Revenue || l.Account.Type == AccountType.Expense))
            .GroupBy(l => new { l.Account!.Code, l.Account.Type })
            .Select(g => new { g.Key.Code, g.Key.Type, Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .ToListAsync();

        var lines = new List<(string, decimal, decimal)>();
        decimal totalRev = 0, totalExp = 0;
        foreach (var d in data)
        {
            if (d.Type == AccountType.Revenue)
            {
                var bal = d.Credit - d.Debit; // saldo normal kredit
                if (bal != 0) { lines.Add((d.Code, bal, 0m)); totalRev += bal; } // Dr untuk menutup
            }
            else
            {
                var bal = d.Debit - d.Credit; // saldo normal debit
                if (bal != 0) { lines.Add((d.Code, 0m, bal)); totalExp += bal; } // Cr untuk menutup
            }
        }
        var net = totalRev - totalExp;
        if (net > 0) lines.Add((AccountCodes.RetainedEarnings, 0m, net));
        else if (net < 0) lines.Add((AccountCodes.RetainedEarnings, -net, 0m));

        if (lines.Count > 0)
            await PostAsync(end, $"Jurnal Penutup Tahun {year}", "YearEndClosing", fy.Id, lines, user);

        fy.Status = FiscalYearStatus.Closed;
        fy.ClosedAt = DateTime.UtcNow;
        fy.ClosedBy = user;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> ReopenFiscalYearAsync(int year, string? user)
    {
        var fy = await _db.FiscalYears.FirstOrDefaultAsync(f => f.Year == year);
        if (fy is null || fy.Status != FiscalYearStatus.Closed) return (false, $"Tahun {year} tidak dalam keadaan ditutup.");

        // Hanya tahun terkunci paling akhir yang boleh dibuka (jaga urutan).
        var latestClosed = await _db.FiscalYears.Where(f => f.Status == FiscalYearStatus.Closed).MaxAsync(f => (int?)f.Year);
        if (year != latestClosed) return (false, "Hanya tahun terkunci terakhir yang dapat dibuka kembali.");

        var entry = await _db.JournalEntries.Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.SourceType == "YearEndClosing" && j.SourceId == fy.Id);
        if (entry is not null)
        {
            _db.JournalLines.RemoveRange(entry.Lines);
            _db.JournalEntries.Remove(entry);
        }
        fy.Status = FiscalYearStatus.Open;
        fy.ClosedAt = null;
        fy.ClosedBy = null;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    private async Task<decimal> ToBaseAsync(decimal amount, int? currencyId, DateTime date)
    {
        var baseCurrency = await _currency.GetBaseCurrencyAsync();
        if (baseCurrency is null || currencyId is null || currencyId == baseCurrency.Id) return amount;
        var converted = await _currency.ConvertAsync(amount, currencyId.Value, baseCurrency.Id, date);
        return converted ?? amount;
    }
}
