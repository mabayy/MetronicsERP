using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpMetronic.Domain.Common;
using ErpMetronic.Domain.Enums;

namespace ErpMetronic.Domain.Entities;

/// <summary>
/// Sales Order (pesanan penjualan) dari pelanggan. Alur: Draft → Confirmed →
/// (Partially)Delivered. Pengiriman mengurangi stok & memperbarui jumlah terkirim.
/// </summary>
public class SalesOrder : BaseEntity
{
    [Required, StringLength(40)]
    public string ReferenceNumber { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; } = DateTime.Today;

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Gudang sumber pengiriman.</summary>
    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public int? CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Draft;

    [StringLength(300)]
    public string? Note { get; set; }

    // ----- PPh dipotong pelanggan (estimasi) tingkat dokumen -----
    public int? WithholdingTaxId { get; set; }
    public Tax? WithholdingTax { get; set; }

    [Column(TypeName = "decimal(9,4)")]
    public decimal WithholdingRate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal WithholdingAmount { get; set; }

    public ICollection<SalesOrderItem> Items { get; set; } = new List<SalesOrderItem>();

    [NotMapped]
    public decimal Subtotal => Items?.Sum(i => i.Quantity * i.UnitPrice) ?? 0;

    [NotMapped]
    public decimal TaxTotal => Items?.Sum(i => i.TaxAmount) ?? 0;

    [NotMapped]
    public decimal GrandTotal => Subtotal + TaxTotal - WithholdingAmount;
}

public class SalesOrderItem : BaseEntity
{
    public int SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    /// <summary>Akumulasi jumlah yang sudah dikirim (≤ Quantity).</summary>
    public int DeliveredQuantity { get; set; }

    // ----- PPN per baris (snapshot) -----
    public int? TaxId { get; set; }
    public Tax? Tax { get; set; }

    [Column(TypeName = "decimal(9,4)")]
    public decimal TaxRate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    [NotMapped]
    public int OutstandingQuantity => Quantity - DeliveredQuantity;

    [NotMapped]
    public decimal LineSubtotal => Quantity * UnitPrice;
}
