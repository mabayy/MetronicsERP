using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpMetronic.Domain.Common;

namespace ErpMetronic.Domain.Entities;

/// <summary>
/// Retur Penjualan (barang dikembalikan pelanggan). Saat dibuat: stok masuk kembali ke gudang
/// dan jurnal otomatis (Dr Pendapatan/retur, Cr Piutang).
/// </summary>
public class SalesReturn : BaseEntity
{
    [Required, StringLength(40)]
    public string ReferenceNumber { get; set; } = string.Empty;

    public DateTime ReturnDate { get; set; } = DateTime.Today;

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    [StringLength(300)]
    public string? Note { get; set; }

    public ICollection<SalesReturnLine> Lines { get; set; } = new List<SalesReturnLine>();

    [NotMapped]
    public decimal Total => Lines?.Sum(l => l.Quantity * l.UnitPrice) ?? 0;
}

public class SalesReturnLine : BaseEntity
{
    public int SalesReturnId { get; set; }
    public SalesReturn? SalesReturn { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }
}
