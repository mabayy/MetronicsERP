using System.ComponentModel.DataAnnotations;
using ErpMetronic.Domain.Common;
using ErpMetronic.Domain.Enums;

namespace ErpMetronic.Domain.Entities;

/// <summary>
/// Konfigurasi penomoran dokumen yang dapat dikustomisasi per <see cref="Code"/> (object code).
/// Bawaan sistem ditandai <see cref="IsSystem"/>; pengguna dapat menambah kode sendiri.
/// Format memakai token: {PREFIX} {YYYY} {YY} {MM} {DD} {SEQ}.
/// Contoh: "{PREFIX}-{YYYY}{MM}-{SEQ}" → "PO-202606-0001".
/// </summary>
public class DocumentNumberSequence : BaseEntity
{
    /// <summary>Kode unik jenis dokumen, mis. PO, GR, MEMO. Menjadi kunci rujukan aplikasi.</summary>
    [Required, StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string Name { get; set; } = string.Empty;

    [StringLength(15)]
    public string? Prefix { get; set; }

    [Required, StringLength(60)]
    public string Format { get; set; } = "{PREFIX}-{YYYY}{MM}-{SEQ}";

    /// <summary>Jumlah digit nomor urut (mis. 4 → 0001).</summary>
    [Range(1, 10)]
    public int Padding { get; set; } = 4;

    /// <summary>Nomor urut berikutnya yang akan dipakai.</summary>
    public int NextNumber { get; set; } = 1;

    public NumberResetPeriod ResetPeriod { get; set; } = NumberResetPeriod.Monthly;

    /// <summary>Penanda kapan terakhir nomor di-reset (deteksi pergantian periode).</summary>
    public int? LastResetYear { get; set; }
    public int? LastResetMonth { get; set; }

    /// <summary>Kode bawaan sistem—boleh diedit, tidak boleh dihapus, kode tidak dapat diubah.</summary>
    public bool IsSystem { get; set; }
}
