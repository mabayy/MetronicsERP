using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpMetronic.Domain.Common;

namespace ErpMetronic.Domain.Entities;

/// <summary>
/// Jurnal (journal voucher) — kumpulan baris debit/kredit yang seimbang. Dapat dibuat manual
/// atau otomatis dari dokumen (faktur/pembayaran). Nilai dalam mata uang dasar.
/// </summary>
public class JournalEntry : BaseEntity
{
    [Required, StringLength(40)]
    public string ReferenceNumber { get; set; } = string.Empty;

    public DateTime EntryDate { get; set; } = DateTime.Today;

    [StringLength(250)]
    public string? Description { get; set; }

    /// <summary>Sumber otomatis (mis. "PurchaseInvoice"); kosong untuk jurnal manual.</summary>
    [StringLength(40)]
    public string? SourceType { get; set; }
    public int? SourceId { get; set; }

    public ICollection<JournalLine> Lines { get; set; } = new List<JournalLine>();

    [NotMapped]
    public decimal TotalDebit => Lines?.Sum(l => l.Debit) ?? 0;

    [NotMapped]
    public decimal TotalCredit => Lines?.Sum(l => l.Credit) ?? 0;
}

public class JournalLine : BaseEntity
{
    public int JournalEntryId { get; set; }
    public JournalEntry? JournalEntry { get; set; }

    public int AccountId { get; set; }
    public ChartOfAccount? Account { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Debit { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Credit { get; set; }

    [StringLength(250)]
    public string? Description { get; set; }
}
