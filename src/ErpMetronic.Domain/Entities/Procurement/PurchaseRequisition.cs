using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpMetronic.Domain.Common;
using ErpMetronic.Domain.Enums;

namespace ErpMetronic.Domain.Entities;

/// <summary>
/// Purchase Requisition (permintaan pembelian internal). Alur: Draft → Submitted →
/// Approved/Rejected. PR yang disetujui dapat dijadikan dasar RFQ.
/// </summary>
public class PurchaseRequisition : BaseEntity
{
    [Required, StringLength(40)]
    public string ReferenceNumber { get; set; } = string.Empty;

    public DateTime RequestDate { get; set; } = DateTime.Today;

    [StringLength(100)]
    public string? RequestedBy { get; set; }

    [StringLength(100)]
    public string? Department { get; set; }

    public PurchaseRequisitionStatus Status { get; set; } = PurchaseRequisitionStatus.Draft;

    [StringLength(300)]
    public string? Note { get; set; }

    [StringLength(100)]
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    [StringLength(300)]
    public string? RejectionReason { get; set; }

    public ICollection<PurchaseRequisitionLine> Lines { get; set; } = new List<PurchaseRequisitionLine>();
}

public class PurchaseRequisitionLine : BaseEntity
{
    public int PurchaseRequisitionId { get; set; }
    public PurchaseRequisition? PurchaseRequisition { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal EstimatedPrice { get; set; }

    [StringLength(200)]
    public string? Note { get; set; }
}
