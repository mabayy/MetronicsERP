using ErpMetronic.Domain.Enums;

namespace ErpMetronic.Domain.Constants;

/// <summary>
/// Kode akun bawaan yang dipakai posting otomatis. Akun dengan kode ini di-seed sebagai
/// akun sistem dan menjadi rujukan JournalService.
/// </summary>
public static class AccountCodes
{
    public const string Cash = "1100";              // Kas/Bank
    public const string AccountsReceivable = "1200"; // Piutang Usaha
    public const string Inventory = "1300";          // Persediaan
    public const string AccountsPayable = "2100";    // Hutang Usaha
    public const string Capital = "3100";            // Modal
    public const string SalesRevenue = "4100";       // Pendapatan Penjualan
    public const string PurchaseExpense = "5100";    // Pembelian/HPP

    /// <summary>Bagan akun bawaan (kode, nama, jenis) untuk seeding.</summary>
    public static readonly (string Code, string Name, AccountType Type)[] Defaults =
    {
        (Cash, "Kas/Bank", AccountType.Asset),
        (AccountsReceivable, "Piutang Usaha", AccountType.Asset),
        (Inventory, "Persediaan", AccountType.Asset),
        (AccountsPayable, "Hutang Usaha", AccountType.Liability),
        (Capital, "Modal", AccountType.Equity),
        (SalesRevenue, "Pendapatan Penjualan", AccountType.Revenue),
        (PurchaseExpense, "Pembelian", AccountType.Expense)
    };
}
