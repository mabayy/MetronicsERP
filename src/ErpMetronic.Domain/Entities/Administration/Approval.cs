using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpMetronic.Domain.Common;
using ErpMetronic.Domain.Enums;

namespace ErpMetronic.Domain.Entities;

/// <summary>
/// Aturan persetujuan berjenjang (mirip SAP B1 Approval Procedure): bila nilai dokumen ≥ ambang,
/// dokumen wajib disetujui melalui beberapa langkah (per jabatan) secara berurutan.
/// </summary>
public class ApprovalRule : BaseEntity
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Jenis dokumen, mis. "PurchaseOrder".</summary>
    [Required, StringLength(40)]
    public string DocumentType { get; set; } = "PurchaseOrder";

    /// <summary>Ambang nilai minimum yang memicu persetujuan.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal MinAmount { get; set; }

    public ICollection<ApprovalRuleStep> Steps { get; set; } = new List<ApprovalRuleStep>();
}

public class ApprovalRuleStep : BaseEntity
{
    public int ApprovalRuleId { get; set; }
    public ApprovalRule? ApprovalRule { get; set; }

    /// <summary>Urutan langkah (1, 2, 3, …).</summary>
    public int Level { get; set; }

    /// <summary>Jabatan yang berwenang menyetujui langkah ini.</summary>
    public int PositionId { get; set; }
    public Position? Position { get; set; }
}

/// <summary>Permintaan persetujuan atas sebuah dokumen, dibuat saat dokumen melewati ambang.</summary>
public class ApprovalRequest : BaseEntity
{
    [Required, StringLength(40)]
    public string DocumentType { get; set; } = string.Empty;
    public int DocumentId { get; set; }

    [StringLength(40)]
    public string? ReferenceNumber { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    /// <summary>Level langkah yang sedang menunggu keputusan.</summary>
    public int CurrentLevel { get; set; } = 1;

    public ICollection<ApprovalStep> Steps { get; set; } = new List<ApprovalStep>();
}

public class ApprovalStep : BaseEntity
{
    public int ApprovalRequestId { get; set; }
    public ApprovalRequest? ApprovalRequest { get; set; }

    public int Level { get; set; }

    public int PositionId { get; set; }
    public Position? Position { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    [StringLength(100)]
    public string? DecidedBy { get; set; }
    public DateTime? DecidedAt { get; set; }

    [StringLength(300)]
    public string? Note { get; set; }
}
