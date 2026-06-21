using System.ComponentModel.DataAnnotations;

namespace ErpMetronic.Web.ViewModels;

public class SalesQuotationCreateViewModel
{
    [Required(ErrorMessage = "Pelanggan wajib dipilih"), Display(Name = "Pelanggan")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Gudang wajib dipilih"), Display(Name = "Gudang")]
    public int WarehouseId { get; set; }

    [Display(Name = "Mata Uang")]
    public int? CurrencyId { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Tanggal Penawaran")]
    public DateTime QuotationDate { get; set; } = DateTime.Today;

    [Required, DataType(DataType.Date), Display(Name = "Berlaku Sampai")]
    public DateTime ValidUntil { get; set; } = DateTime.Today.AddDays(14);

    [Display(Name = "Catatan"), StringLength(300)]
    public string? Note { get; set; }

    [Display(Name = "PPh dipotong")]
    public int? WithholdingTaxId { get; set; }

    [Display(Name = "Diskon Header (%)")]
    public decimal HeaderDiscountPercent { get; set; }

    public List<SalesQuotationLineInput> Items { get; set; } = new();
}

public class SalesQuotationLineInput
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public int? TaxId { get; set; }
}
