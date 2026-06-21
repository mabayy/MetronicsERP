using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpMetronic.Domain.Common;

namespace ErpMetronic.Domain.Entities;

/// <summary>
/// Daftar Harga (price list) ala SAP B1/Odoo: kumpulan harga jual per produk yang dapat dipakai
/// ulang & ditetapkan sebagai harga default pelanggan.
/// </summary>
public class PriceList : BaseEntity
{
    [Required, StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Mata uang daftar harga (opsional; kosong = mata uang dasar).</summary>
    public int? CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    public bool IsSystem { get; set; }

    public ICollection<PriceListItem> Items { get; set; } = new List<PriceListItem>();
}

public class PriceListItem : BaseEntity
{
    public int PriceListId { get; set; }
    public PriceList? PriceList { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
}
