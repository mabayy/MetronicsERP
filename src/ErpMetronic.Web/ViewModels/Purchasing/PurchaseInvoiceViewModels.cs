using System.ComponentModel.DataAnnotations;

namespace ErpMetronic.Web.ViewModels;

public class PurchaseInvoiceCreateViewModel
{
    public int PurchaseOrderId { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Tanggal Faktur")]
    public DateTime InvoiceDate { get; set; } = DateTime.Today;

    [Display(Name = "Catatan"), StringLength(300)]
    public string? Note { get; set; }

    public List<InvoiceLineInput> Lines { get; set; } = new();
}

public class InvoiceLineInput
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class PurchasePaymentViewModel
{
    public int PurchaseInvoiceId { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Tanggal Bayar")]
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [Range(0.01, double.MaxValue, ErrorMessage = "Jumlah harus lebih dari 0"), Display(Name = "Jumlah Bayar")]
    public decimal Amount { get; set; }

    [Display(Name = "Metode"), StringLength(40)]
    public string? Method { get; set; }

    [Display(Name = "Catatan"), StringLength(300)]
    public string? Note { get; set; }
}
