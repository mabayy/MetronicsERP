using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpMetronic.Domain.Common;
using ErpMetronic.Domain.Enums;

namespace ErpMetronic.Domain.Entities;

/// <summary>
/// Request for Quotation (permintaan penawaran ke pemasok). Dapat dibuat dari PR yang disetujui.
/// Alur: Draft → Sent → Closed (setelah satu penawaran dipilih/diberikan).
/// </summary>
public class RequestForQuotation : BaseEntity
{
    [Required, StringLength(40)]
    public string ReferenceNumber { get; set; } = string.Empty;

    public DateTime RfqDate { get; set; } = DateTime.Today;

    /// <summary>PR sumber (opsional).</summary>
    public int? PurchaseRequisitionId { get; set; }
    public PurchaseRequisition? PurchaseRequisition { get; set; }

    public RequestForQuotationStatus Status { get; set; } = RequestForQuotationStatus.Draft;

    [StringLength(300)]
    public string? Note { get; set; }

    public ICollection<RfqLine> Lines { get; set; } = new List<RfqLine>();
    public ICollection<RfqQuote> Quotes { get; set; } = new List<RfqQuote>();
}

public class RfqLine : BaseEntity
{
    public int RequestForQuotationId { get; set; }
    public RequestForQuotation? RequestForQuotation { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }
}

/// <summary>Penawaran dari satu pemasok atas sebuah RFQ.</summary>
public class RfqQuote : BaseEntity
{
    public int RequestForQuotationId { get; set; }
    public RequestForQuotation? RequestForQuotation { get; set; }

    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal QuotedAmount { get; set; }

    public int? LeadTimeDays { get; set; }

    [StringLength(200)]
    public string? Note { get; set; }

    /// <summary>Penawaran terpilih (pemenang).</summary>
    public bool IsSelected { get; set; }
}
