using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpMetronic.Domain.Common;
using ErpMetronic.Domain.Enums;

namespace ErpMetronic.Domain.Entities;

/// <summary>
/// Purchase Order (pesanan pembelian) ke pemasok. Alur: Draft → Ordered → (Partially)Received.
/// Penerimaan barang memicu Stok Masuk dan memperbarui jumlah diterima per item.
/// </summary>
public class PurchaseOrder : BaseEntity
{
    [Required, StringLength(40)]
    public string ReferenceNumber { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; } = DateTime.Today;

    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    /// <summary>Gudang tujuan penerimaan default.</summary>
    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    /// <summary>Mata uang harga PO (multi-currency).</summary>
    public int? CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    [StringLength(300)]
    public string? Note { get; set; }

    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}

public class PurchaseOrderItem : BaseEntity
{
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    /// <summary>Akumulasi jumlah yang sudah diterima (≤ Quantity).</summary>
    public int ReceivedQuantity { get; set; }

    [NotMapped]
    public int OutstandingQuantity => Quantity - ReceivedQuantity;
}
