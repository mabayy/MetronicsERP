using ErpMetronic.Domain.Entities;

namespace ErpMetronic.Infrastructure.Services;

/// <summary>Posting jurnal akuntansi (manual & otomatis dari dokumen). Nilai dalam mata uang dasar.</summary>
public interface IJournalService
{
    /// <summary>Posting jurnal seimbang dari daftar (kode akun, debit, kredit). Null bila akun tak lengkap/tidak seimbang.</summary>
    Task<JournalEntry?> PostAsync(DateTime date, string description, string? sourceType, int? sourceId,
        IEnumerable<(string AccountCode, decimal Debit, decimal Credit)> lines, string? user);

    Task PostPurchaseInvoiceAsync(PurchaseInvoice invoice, string? user);
    Task PostPurchasePaymentAsync(PurchasePayment payment, int? currencyId, string cashAccountCode, string? user);
    Task PostSalesInvoiceAsync(SalesInvoice invoice, string? user);
    Task PostSalesPaymentAsync(SalesPayment payment, int? currencyId, string cashAccountCode, string? user);

    /// <summary>HPP saat pengiriman (perpetual): Dr Harga Pokok Penjualan / Cr Persediaan.</summary>
    Task PostDeliveryCogsAsync(int deliveryId, DateTime date, string reference, decimal cogsAmount, string? user);
}
