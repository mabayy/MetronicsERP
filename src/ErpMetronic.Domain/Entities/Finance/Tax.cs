using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpMetronic.Domain.Common;
using ErpMetronic.Domain.Enums;

namespace ErpMetronic.Domain.Entities;

/// <summary>
/// Master pajak (mengikuti pola SAP B1 / Odoo): kode pajak yang dapat dipakai ulang pada
/// baris/dokumen transaksi. PPN (VAT) menambah nilai dokumen; PPh (withholding) dipotong
/// dari nilai yang dibayar. Setiap pajak menunjuk akun GL untuk posting otomatis.
/// </summary>
public class Tax : BaseEntity
{
    [Required, StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Tarif dalam persen, mis. 11.00 untuk PPN 11%.</summary>
    [Column(TypeName = "decimal(9,4)")]
    public decimal Rate { get; set; }

    public TaxKind Kind { get; set; } = TaxKind.ValueAdded;

    public TaxApplicability AppliesTo { get; set; } = TaxApplicability.Both;

    /// <summary>Kode akun GL tujuan posting (mis. PPN Masukan/Keluaran).</summary>
    [Required, StringLength(20)]
    public string AccountCode { get; set; } = string.Empty;

    // IsActive diwarisi dari BaseEntity.

    /// <summary>Pajak bawaan sistem (tidak dapat dihapus).</summary>
    public bool IsSystem { get; set; }

    [NotMapped]
    public bool IsWithholding => Kind == TaxKind.Withholding;
}
