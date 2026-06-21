using System.ComponentModel.DataAnnotations;
using ErpMetronic.Domain.Common;
using ErpMetronic.Domain.Enums;

namespace ErpMetronic.Domain.Entities;

/// <summary>Jenis akun kas/bank.</summary>
public enum CashBankAccountKind
{
    Cash = 1,
    Bank = 2
}

/// <summary>
/// Akun Kas/Bank (mengikuti SAP B1/Odoo): tiap akun terhubung ke satu akun GL untuk posting.
/// Pembayaran & penerimaan diarahkan ke akun ini.
/// </summary>
public class CashBankAccount : BaseEntity
{
    [Required, StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public CashBankAccountKind Kind { get; set; } = CashBankAccountKind.Cash;

    /// <summary>Kode akun GL tujuan posting (mis. 1100 Kas, 1110 Bank).</summary>
    [Required, StringLength(20)]
    public string AccountCode { get; set; } = string.Empty;

    [StringLength(100)]
    public string? BankName { get; set; }

    [StringLength(50)]
    public string? AccountNumber { get; set; }

    public bool IsSystem { get; set; }
}
