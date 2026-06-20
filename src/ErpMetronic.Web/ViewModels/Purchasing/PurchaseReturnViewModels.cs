using System.ComponentModel.DataAnnotations;

namespace ErpMetronic.Web.ViewModels;

public class PurchaseReturnCreateViewModel
{
    [Required(ErrorMessage = "Pemasok wajib dipilih"), Display(Name = "Pemasok")]
    public int SupplierId { get; set; }

    [Required(ErrorMessage = "Gudang wajib dipilih"), Display(Name = "Gudang (kirim balik)")]
    public int WarehouseId { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Tanggal Retur")]
    public DateTime ReturnDate { get; set; } = DateTime.Today;

    [Display(Name = "Catatan"), StringLength(300)]
    public string? Note { get; set; }

    public List<PurchaseReturnLineInput> Lines { get; set; } = new();
}

public class PurchaseReturnLineInput
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
