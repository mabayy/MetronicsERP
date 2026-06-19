using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpMetronic.Domain.Common;
using ErpMetronic.Domain.Enums;

namespace ErpMetronic.Domain.Entities;

/// <summary>
/// Faktur Penjualan (piutang) dibuat terhadap Sales Order. Jumlah difaktur dibatasi oleh
/// jumlah yang sudah dikirim (matching: SO dipesan ↔ barang dikirim ↔ jumlah difaktur).
/// </summary>
public class SalesInvoice : BaseEntity
{
    [Required, StringLength(40)]
    public string ReferenceNumber { get; set; } = string.Empty;

    public DateTime InvoiceDate { get; set; } = DateTime.Today;

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    public int? CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    public SalesInvoiceStatus Status { get; set; } = SalesInvoiceStatus.Unpaid;

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; }

    [StringLength(300)]
    public string? Note { get; set; }

    public ICollection<SalesInvoiceLine> Lines { get; set; } = new List<SalesInvoiceLine>();
    public ICollection<SalesPayment> Payments { get; set; } = new List<SalesPayment>();

    [NotMapped]
    public decimal Total => Lines?.Sum(l => l.Quantity * l.UnitPrice) ?? 0;

    [NotMapped]
    public decimal Outstanding => Total - PaidAmount;
}

public class SalesInvoiceLine : BaseEntity
{
    public int SalesInvoiceId { get; set; }
    public SalesInvoice? SalesInvoice { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }
}

public class SalesPayment : BaseEntity
{
    [Required, StringLength(40)]
    public string ReferenceNumber { get; set; } = string.Empty;

    public int SalesInvoiceId { get; set; }
    public SalesInvoice? SalesInvoice { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [StringLength(40)]
    public string? Method { get; set; }

    [StringLength(300)]
    public string? Note { get; set; }
}
