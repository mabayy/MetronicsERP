using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpMetronic.Domain.Common;

namespace ErpMetronic.Domain.Entities;

/// <summary>Pelanggan (master data).</summary>
public class Customer : BaseEntity
{
    [Required, StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(150)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    /// <summary>Batas kredit (0 = tanpa batas). Faktur penjualan diblokir bila melebihi.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal CreditLimit { get; set; }

    /// <summary>Termin pembayaran default.</summary>
    public int? PaymentTermId { get; set; }
    public PaymentTerm? PaymentTerm { get; set; }

    /// <summary>Daftar harga default pelanggan.</summary>
    public int? PriceListId { get; set; }
    public PriceList? PriceList { get; set; }
}
