using System.ComponentModel.DataAnnotations;

namespace ErpMetronic.Web.ViewModels;

public class SalesReturnCreateViewModel
{
    [Required(ErrorMessage = "Pelanggan wajib dipilih"), Display(Name = "Pelanggan")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Gudang wajib dipilih"), Display(Name = "Gudang (terima kembali)")]
    public int WarehouseId { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Tanggal Retur")]
    public DateTime ReturnDate { get; set; } = DateTime.Today;

    [Display(Name = "Catatan"), StringLength(300)]
    public string? Note { get; set; }

    public List<SalesReturnLineInput> Lines { get; set; } = new();
}

public class SalesReturnLineInput
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
