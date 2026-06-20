using System.ComponentModel.DataAnnotations;
using ErpMetronic.Domain.Common;
using ErpMetronic.Domain.Enums;

namespace ErpMetronic.Domain.Entities;

/// <summary>Akun pada Bagan Akun (Chart of Accounts).</summary>
public class ChartOfAccount : BaseEntity
{
    [Required, StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    public AccountType Type { get; set; }

    [StringLength(250)]
    public string? Description { get; set; }

    /// <summary>Akun bawaan sistem (dipakai posting otomatis) — tidak dapat dihapus.</summary>
    public bool IsSystem { get; set; }

    /// <summary>Saldo normal debit untuk Asset & Expense; selain itu kredit.</summary>
    public bool IsDebitNormal => Type is AccountType.Asset or AccountType.Expense;
}
