using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpMetronic.Domain.Common;

namespace ErpMetronic.Domain.Entities;

/// <summary>
/// Retur Pembelian (barang dikembalikan ke pemasok). Saat dibuat: stok keluar dari gudang
/// (divalidasi saldo) dan jurnal otomatis (Dr Hutang, Cr Persediaan).
/// </summary>
public class PurchaseReturn : BaseEntity
{
    [Required, StringLength(40)]
    public string ReferenceNumber { get; set; } = string.Empty;

    public DateTime ReturnDate { get; set; } = DateTime.Today;

    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    [StringLength(300)]
    public string? Note { get; set; }

    public ICollection<PurchaseReturnLine> Lines { get; set; } = new List<PurchaseReturnLine>();

    [NotMapped]
    public decimal Total => Lines?.Sum(l => l.Quantity * l.UnitPrice) ?? 0;
}

public class PurchaseReturnLine : BaseEntity
{
    public int PurchaseReturnId { get; set; }
    public PurchaseReturn? PurchaseReturn { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }
}
