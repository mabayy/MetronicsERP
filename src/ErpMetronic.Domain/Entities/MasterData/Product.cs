using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpMetronic.Domain.Common;

namespace ErpMetronic.Domain.Entities;

/// <summary>Produk / barang (master data inti).</summary>
public class Product : BaseEntity
{
    [Required, StringLength(40)]
    public string Sku { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PurchasePrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SellingPrice { get; set; }

    /// <summary>Biaya rata-rata bergerak (moving average cost) dalam mata uang dasar.</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal AverageCost { get; set; }

    public int StockQuantity { get; set; }

    /// <summary>Titik pemesanan ulang (stok minimum). Bila stok ≤ nilai ini, muncul di saran pembelian.</summary>
    public int ReorderLevel { get; set; }

    /// <summary>Jumlah pemesanan ulang yang disarankan (lot size). 0 = isi sampai titik reorder.</summary>
    public int ReorderQuantity { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int UnitOfMeasureId { get; set; }
    public UnitOfMeasure? UnitOfMeasure { get; set; }

    /// <summary>Mata uang harga produk. Bila kosong, dianggap memakai mata uang dasar.</summary>
    public int? CurrencyId { get; set; }
    public Currency? Currency { get; set; }
}
