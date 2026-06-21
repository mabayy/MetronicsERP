using System.ComponentModel.DataAnnotations;
using ErpMetronic.Domain.Common;

namespace ErpMetronic.Domain.Entities;

/// <summary>
/// Termin pembayaran (mengikuti SAP B1/Odoo): menentukan jatuh tempo faktur =
/// tanggal faktur + <see cref="NetDays"/> hari.
/// </summary>
public class PaymentTerm : BaseEntity
{
    [Required, StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Jumlah hari hingga jatuh tempo (0 = tunai/jatuh tempo saat itu juga).</summary>
    public int NetDays { get; set; }

    /// <summary>Termin bawaan sistem (tidak dapat dihapus).</summary>
    public bool IsSystem { get; set; }
}
