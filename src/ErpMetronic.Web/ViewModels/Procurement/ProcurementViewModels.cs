using System.ComponentModel.DataAnnotations;

namespace ErpMetronic.Web.ViewModels;

// ----- Purchase Requisition -----
public class PurchaseRequisitionCreateViewModel
{
    [Required, DataType(DataType.Date), Display(Name = "Tanggal")]
    public DateTime RequestDate { get; set; } = DateTime.Today;

    [Display(Name = "Diminta Oleh"), StringLength(100)]
    public string? RequestedBy { get; set; }

    [Display(Name = "Divisi/Departemen"), StringLength(100)]
    public string? Department { get; set; }

    [Display(Name = "Catatan"), StringLength(300)]
    public string? Note { get; set; }

    public List<PrLineInput> Lines { get; set; } = new();
}

public class PrLineInput
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal EstimatedPrice { get; set; }
    public string? Note { get; set; }
}

// ----- Request for Quotation -----
public class RfqCreateViewModel
{
    [Required, DataType(DataType.Date), Display(Name = "Tanggal")]
    public DateTime RfqDate { get; set; } = DateTime.Today;

    [Display(Name = "Dari PR")]
    public int? PurchaseRequisitionId { get; set; }

    [Display(Name = "Catatan"), StringLength(300)]
    public string? Note { get; set; }

    public List<RfqLineInput> Lines { get; set; } = new();
}

public class RfqLineInput
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class RfqQuoteInput
{
    public int RequestForQuotationId { get; set; }

    [Required(ErrorMessage = "Pemasok wajib dipilih"), Display(Name = "Pemasok")]
    public int SupplierId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Nilai penawaran harus > 0"), Display(Name = "Nilai Penawaran")]
    public decimal QuotedAmount { get; set; }

    [Display(Name = "Lead Time (hari)")]
    public int? LeadTimeDays { get; set; }

    [Display(Name = "Catatan"), StringLength(200)]
    public string? Note { get; set; }
}
