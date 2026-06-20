using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpMetronic.Domain.Common;
using ErpMetronic.Domain.Enums;

namespace ErpMetronic.Domain.Entities;

/// <summary>
/// Faktur Pembelian (hutang) yang dibuat terhadap sebuah Purchase Order.
/// Jumlah yang difakturkan dibatasi oleh jumlah yang sudah diterima (3-way matching:
/// PO dipesan ↔ barang diterima ↔ jumlah difaktur).
/// </summary>
public class PurchaseInvoice : BaseEntity
{
    [Required, StringLength(40)]
    public string ReferenceNumber { get; set; } = string.Empty;

    public DateTime InvoiceDate { get; set; } = DateTime.Today;

    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public int PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public int? CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    public PurchaseInvoiceStatus Status { get; set; } = PurchaseInvoiceStatus.Unpaid;

    /// <summary>Akumulasi nilai yang sudah dibayar.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; }

    [StringLength(300)]
    public string? Note { get; set; }

    // ----- Diskon header (tingkat dokumen) -----
    [Column(TypeName = "decimal(9,4)")]
    public decimal HeaderDiscountPercent { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal HeaderDiscountAmount { get; set; }

    // ----- PPh dipotong (withholding) tingkat dokumen -----
    public int? WithholdingTaxId { get; set; }
    public Tax? WithholdingTax { get; set; }

    /// <summary>Tarif PPh (snapshot %) saat dokumen dibuat.</summary>
    [Column(TypeName = "decimal(9,4)")]
    public decimal WithholdingRate { get; set; }

    /// <summary>Nilai PPh dipotong (snapshot) = DPP × tarif.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal WithholdingAmount { get; set; }

    public ICollection<PurchaseInvoiceLine> Lines { get; set; } = new List<PurchaseInvoiceLine>();
    public ICollection<PurchasePayment> Payments { get; set; } = new List<PurchasePayment>();

    /// <summary>Jumlah neto baris (setelah diskon baris) sebelum diskon header.</summary>
    [NotMapped]
    public decimal NetBeforeHeaderDiscount => Lines?.Sum(l => l.LineNet) ?? 0;

    /// <summary>DPP (dasar pengenaan pajak) = neto baris − diskon header.</summary>
    [NotMapped]
    public decimal Subtotal => NetBeforeHeaderDiscount - HeaderDiscountAmount;

    /// <summary>Total PPN dari seluruh baris.</summary>
    [NotMapped]
    public decimal TaxTotal => Lines?.Sum(l => l.TaxAmount) ?? 0;

    /// <summary>Nilai yang terutang/dibayar = DPP + PPN − PPh dipotong.</summary>
    [NotMapped]
    public decimal Total => Subtotal + TaxTotal - WithholdingAmount;

    [NotMapped]
    public decimal Outstanding => Total - PaidAmount;
}

public class PurchaseInvoiceLine : BaseEntity
{
    public int PurchaseInvoiceId { get; set; }
    public PurchaseInvoice? PurchaseInvoice { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

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

    /// <summary>Bruto baris = jumlah × harga.</summary>
    [NotMapped]
    public decimal LineGross => Quantity * UnitPrice;

    [NotMapped]
    public decimal LineDiscountAmount => Math.Round(LineGross * DiscountPercent / 100m, 2, MidpointRounding.AwayFromZero);

    /// <summary>Neto baris setelah diskon baris (sebelum diskon header).</summary>
    [NotMapped]
    public decimal LineNet => LineGross - LineDiscountAmount;

    [NotMapped]
    public decimal LineTotal => LineNet + TaxAmount;
}

public class PurchasePayment : BaseEntity
{
    [Required, StringLength(40)]
    public string ReferenceNumber { get; set; } = string.Empty;

    public int PurchaseInvoiceId { get; set; }
    public PurchaseInvoice? PurchaseInvoice { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [StringLength(40)]
    public string? Method { get; set; }

    [StringLength(300)]
    public string? Note { get; set; }
}
