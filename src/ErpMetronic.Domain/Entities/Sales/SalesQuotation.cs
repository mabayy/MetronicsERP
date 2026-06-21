using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpMetronic.Domain.Common;
using ErpMetronic.Domain.Enums;

namespace ErpMetronic.Domain.Entities;

/// <summary>
/// Penawaran Penjualan (Sales Quotation) ala SAP B1/Odoo: dokumen pra-pesanan.
/// Alur: Draft → Terkirim → Diterima/Ditolak; yang diterima dikonversi menjadi Sales Order.
/// </summary>
public class SalesQuotation : BaseEntity
{
    [Required, StringLength(40)]
    public string ReferenceNumber { get; set; } = string.Empty;

    public DateTime QuotationDate { get; set; } = DateTime.Today;

    /// <summary>Berlaku sampai tanggal.</summary>
    public DateTime ValidUntil { get; set; } = DateTime.Today.AddDays(14);

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public int? CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    public SalesQuotationStatus Status { get; set; } = SalesQuotationStatus.Draft;

    [StringLength(300)]
    public string? Note { get; set; }

    // ----- Diskon header & PPh (mirror Sales Order) -----
    [Column(TypeName = "decimal(9,4)")]
    public decimal HeaderDiscountPercent { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal HeaderDiscountAmount { get; set; }

    public int? WithholdingTaxId { get; set; }
    public Tax? WithholdingTax { get; set; }
    [Column(TypeName = "decimal(9,4)")]
    public decimal WithholdingRate { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal WithholdingAmount { get; set; }

    /// <summary>SO hasil konversi (bila sudah dikonversi).</summary>
    public int? ConvertedSalesOrderId { get; set; }

    public ICollection<SalesQuotationItem> Items { get; set; } = new List<SalesQuotationItem>();

    [NotMapped]
    public decimal NetBeforeHeaderDiscount => Items?.Sum(i => i.LineNet) ?? 0;
    [NotMapped]
    public decimal Subtotal => NetBeforeHeaderDiscount - HeaderDiscountAmount;
    [NotMapped]
    public decimal TaxTotal => Items?.Sum(i => i.TaxAmount) ?? 0;
    [NotMapped]
    public decimal GrandTotal => Subtotal + TaxTotal - WithholdingAmount;
}

public class SalesQuotationItem : BaseEntity
{
    public int SalesQuotationId { get; set; }
    public SalesQuotation? SalesQuotation { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(9,4)")]
    public decimal DiscountPercent { get; set; }

    public int? TaxId { get; set; }
    public Tax? Tax { get; set; }
    [Column(TypeName = "decimal(9,4)")]
    public decimal TaxRate { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    [NotMapped]
    public decimal LineGross => Quantity * UnitPrice;
    [NotMapped]
    public decimal LineDiscountAmount => Math.Round(LineGross * DiscountPercent / 100m, 2, MidpointRounding.AwayFromZero);
    [NotMapped]
    public decimal LineNet => LineGross - LineDiscountAmount;
}
