namespace ErpMetronic.Domain.Enums;

/// <summary>Jenis akun pada Chart of Accounts. Menentukan saldo normal (debit/kredit).</summary>
public enum AccountType
{
    Asset = 1,      // Aset — saldo normal debit
    Liability = 2,  // Kewajiban — saldo normal kredit
    Equity = 3,     // Ekuitas — saldo normal kredit
    Revenue = 4,    // Pendapatan — saldo normal kredit
    Expense = 5     // Beban — saldo normal debit
}
