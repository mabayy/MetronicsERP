using System.ComponentModel.DataAnnotations;

namespace ErpMetronic.Web.ViewModels;

public class PurchaseOrderCreateViewModel
{
    [Required(ErrorMessage = "Pemasok wajib dipilih"), Display(Name = "Pemasok")]
    public int SupplierId { get; set; }

    [Required(ErrorMessage = "Gudang wajib dipilih"), Display(Name = "Gudang Tujuan")]
    public int WarehouseId { get; set; }

    [Display(Name = "Mata Uang")]
    public int? CurrencyId { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Tanggal PO")]
    public DateTime OrderDate { get; set; } = DateTime.Today;

    [Display(Name = "Catatan"), StringLength(300)]
    public string? Note { get; set; }

    [Display(Name = "PPh dipotong")]
    public int? WithholdingTaxId { get; set; }

    public List<PurchaseLineInput> Items { get; set; } = new();
}

public class PurchaseLineInput
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int? TaxId { get; set; }
}

public class ReceivePoViewModel
{
    public int PurchaseOrderId { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Tanggal Terima")]
    public DateTime ReceiptDate { get; set; } = DateTime.Today;

    public List<ReceiveLineInput> Lines { get; set; } = new();
}

public class ReceiveLineInput
{
    public int ItemId { get; set; }
    public int ReceiveQuantity { get; set; }
}
