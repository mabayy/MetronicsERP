using System.ComponentModel.DataAnnotations;

namespace ErpMetronic.Web.ViewModels;

public class SalesOrderCreateViewModel
{
    [Required(ErrorMessage = "Pelanggan wajib dipilih"), Display(Name = "Pelanggan")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Gudang wajib dipilih"), Display(Name = "Gudang Sumber")]
    public int WarehouseId { get; set; }

    [Display(Name = "Mata Uang")]
    public int? CurrencyId { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Tanggal SO")]
    public DateTime OrderDate { get; set; } = DateTime.Today;

    [Display(Name = "Catatan"), StringLength(300)]
    public string? Note { get; set; }

    [Display(Name = "PPh dipotong")]
    public int? WithholdingTaxId { get; set; }

    [Display(Name = "Diskon Header (%)")]
    public decimal HeaderDiscountPercent { get; set; }

    public List<SalesLineInput> Items { get; set; } = new();
}

public class SalesLineInput
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public int? TaxId { get; set; }
}

public class DeliverSoViewModel
{
    public int SalesOrderId { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Tanggal Kirim")]
    public DateTime DeliveryDate { get; set; } = DateTime.Today;

    public List<DeliverLineInput> Lines { get; set; } = new();
}

public class DeliverLineInput
{
    public int ItemId { get; set; }
    public int DeliverQuantity { get; set; }
}

public class SalesInvoiceCreateViewModel
{
    public int SalesOrderId { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Tanggal Faktur")]
    public DateTime InvoiceDate { get; set; } = DateTime.Today;

    [Display(Name = "Termin Pembayaran")]
    public int? PaymentTermId { get; set; }

    [Display(Name = "Catatan"), StringLength(300)]
    public string? Note { get; set; }

    [Display(Name = "PPh dipotong")]
    public int? WithholdingTaxId { get; set; }

    [Display(Name = "Diskon Header (%)")]
    public decimal HeaderDiscountPercent { get; set; }

    public List<SalesInvLineInput> Lines { get; set; } = new();
}

public class SalesInvLineInput
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public int? TaxId { get; set; }
}

public class SalesPaymentViewModel
{
    public int SalesInvoiceId { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Tanggal Terima")]
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [Range(0.01, double.MaxValue, ErrorMessage = "Jumlah harus lebih dari 0"), Display(Name = "Jumlah Diterima")]
    public decimal Amount { get; set; }

    [Display(Name = "Akun Kas/Bank")]
    public int? CashBankAccountId { get; set; }

    [Display(Name = "Metode"), StringLength(40)]
    public string? Method { get; set; }

    [Display(Name = "Catatan"), StringLength(300)]
    public string? Note { get; set; }
}
