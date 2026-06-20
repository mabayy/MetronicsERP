using ErpMetronic.Domain.Constants;
using ErpMetronic.Domain.Entities;
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
        var amount = await ToBaseAsync(invoice.Total, invoice.CurrencyId, invoice.InvoiceDate);
        // Dr Persediaan, Cr Hutang Usaha
        await PostAsync(invoice.InvoiceDate, $"Faktur Pembelian {invoice.ReferenceNumber}",
            "PurchaseInvoice", invoice.Id, new[]
            {
                (AccountCodes.Inventory, amount, 0m),
                (AccountCodes.AccountsPayable, 0m, amount)
            }, user);
    }

    public async Task PostPurchasePaymentAsync(PurchasePayment payment, int? currencyId, string? user)
    {
        var amount = await ToBaseAsync(payment.Amount, currencyId, payment.PaymentDate);
        // Dr Hutang Usaha, Cr Kas/Bank
        await PostAsync(payment.PaymentDate, $"Pembayaran Pembelian {payment.ReferenceNumber}",
            "PurchasePayment", payment.Id, new[]
            {
                (AccountCodes.AccountsPayable, amount, 0m),
                (AccountCodes.Cash, 0m, amount)
            }, user);
    }

    public async Task PostSalesInvoiceAsync(SalesInvoice invoice, string? user)
    {
        var amount = await ToBaseAsync(invoice.Total, invoice.CurrencyId, invoice.InvoiceDate);
        // Dr Piutang Usaha, Cr Pendapatan Penjualan
        await PostAsync(invoice.InvoiceDate, $"Faktur Penjualan {invoice.ReferenceNumber}",
            "SalesInvoice", invoice.Id, new[]
            {
                (AccountCodes.AccountsReceivable, amount, 0m),
                (AccountCodes.SalesRevenue, 0m, amount)
            }, user);
    }

    public async Task PostSalesPaymentAsync(SalesPayment payment, int? currencyId, string? user)
    {
        var amount = await ToBaseAsync(payment.Amount, currencyId, payment.PaymentDate);
        // Dr Kas/Bank, Cr Piutang Usaha
        await PostAsync(payment.PaymentDate, $"Penerimaan Penjualan {payment.ReferenceNumber}",
            "SalesPayment", payment.Id, new[]
            {
                (AccountCodes.Cash, amount, 0m),
                (AccountCodes.AccountsReceivable, 0m, amount)
            }, user);
    }

    private async Task<decimal> ToBaseAsync(decimal amount, int? currencyId, DateTime date)
    {
        var baseCurrency = await _currency.GetBaseCurrencyAsync();
        if (baseCurrency is null || currencyId is null || currencyId == baseCurrency.Id) return amount;
        var converted = await _currency.ConvertAsync(amount, currencyId.Value, baseCurrency.Id, date);
        return converted ?? amount;
    }
}
