using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpMetronic.Domain.Common;

namespace ErpMetronic.Domain.Entities;

/// <summary>
/// Dokumen Penerimaan Barang (dari pemasok). Saat dibuat, setiap baris langsung
/// diposting sebagai pergerakan Stok Masuk ke gudang tujuan.
/// </summary>
public class GoodsReceipt : BaseEntity
{
    [Required, StringLength(40)]
    public string ReferenceNumber { get; set; } = string.Empty;

    public DateTime ReceiptDate { get; set; } = DateTime.Today;

    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    /// <summary>Purchase Order sumber (kosong bila penerimaan langsung tanpa PO).</summary>
    public int? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    [StringLength(300)]
    public string? Note { get; set; }

    public ICollection<GoodsReceiptLine> Lines { get; set; } = new List<GoodsReceiptLine>();
}

public class GoodsReceiptLine : BaseEntity
{
    public int GoodsReceiptId { get; set; }
    public GoodsReceipt? GoodsReceipt { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitCost { get; set; }
}
