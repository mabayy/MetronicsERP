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

    // ----- Diskon header (tingkat dokumen) -----
    [Column(TypeName = "decimal(9,4)")]
    public decimal HeaderDiscountPercent { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal HeaderDiscountAmount { get; set; }

    // ----- PPh dipotong (estimasi) tingkat dokumen -----
    public int? WithholdingTaxId { get; set; }
    public Tax? WithholdingTax { get; set; }

    [Column(TypeName = "decimal(9,4)")]
    public decimal WithholdingRate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal WithholdingAmount { get; set; }

    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();

    /// <summary>Neto baris (setelah diskon baris) sebelum diskon header.</summary>
    [NotMapped]
    public decimal NetBeforeHeaderDiscount => Items?.Sum(i => i.LineNet) ?? 0;

    /// <summary>DPP = neto baris − diskon header.</summary>
    [NotMapped]
    public decimal Subtotal => NetBeforeHeaderDiscount - HeaderDiscountAmount;

    [NotMapped]
    public decimal TaxTotal => Items?.Sum(i => i.TaxAmount) ?? 0;

    /// <summary>Estimasi nilai PO = DPP + PPN − PPh.</summary>
    [NotMapped]
    public decimal GrandTotal => Subtotal + TaxTotal - WithholdingAmount;
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

    /// <summary>Diskon baris dalam persen.</summary>
    [Column(TypeName = "decimal(9,4)")]
    public decimal DiscountPercent { get; set; }

    // ----- PPN per baris (snapshot) -----
    public int? TaxId { get; set; }
    public Tax? Tax { get; set; }

    [Column(TypeName = "decimal(9,4)")]
    public decimal TaxRate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    [NotMapped]
    public int OutstandingQuantity => Quantity - ReceivedQuantity;

    [NotMapped]
    public decimal LineGross => Quantity * UnitPrice;

    [NotMapped]
    public decimal LineDiscountAmount => Math.Round(LineGross * DiscountPercent / 100m, 2, MidpointRounding.AwayFromZero);

    [NotMapped]
    public decimal LineNet => LineGross - LineDiscountAmount;
}
