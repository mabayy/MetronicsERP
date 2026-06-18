using System.ComponentModel.DataAnnotations;

namespace ErpMetronic.Web.ViewModels;

public abstract class StockFormBase
{
    [Required(ErrorMessage = "Produk wajib dipilih"), Display(Name = "Produk")]
    public int ProductId { get; set; }

    [Required, Display(Name = "Tanggal")]
    [DataType(DataType.Date)]
    public DateTime MovementDate { get; set; } = DateTime.Today;

    [Display(Name = "Catatan"), StringLength(300)]
    public string? Note { get; set; }
}

public class StockInViewModel : StockFormBase
{
    [Required(ErrorMessage = "Gudang wajib dipilih"), Display(Name = "Gudang")]
    public int WarehouseId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Jumlah minimal 1"), Display(Name = "Jumlah Masuk")]
    public int Quantity { get; set; }
}

public class StockOutViewModel : StockFormBase
{
    [Required(ErrorMessage = "Gudang wajib dipilih"), Display(Name = "Gudang")]
    public int WarehouseId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Jumlah minimal 1"), Display(Name = "Jumlah Keluar")]
    public int Quantity { get; set; }
}

public class StockTransferViewModel : StockFormBase
{
    [Required(ErrorMessage = "Gudang asal wajib dipilih"), Display(Name = "Gudang Asal")]
    public int SourceWarehouseId { get; set; }

    [Required(ErrorMessage = "Gudang tujuan wajib dipilih"), Display(Name = "Gudang Tujuan")]
    public int DestinationWarehouseId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Jumlah minimal 1"), Display(Name = "Jumlah Transfer")]
    public int Quantity { get; set; }
}

public class StockAdjustmentViewModel : StockFormBase
{
    [Required(ErrorMessage = "Gudang wajib dipilih"), Display(Name = "Gudang")]
    public int WarehouseId { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Jumlah tidak boleh negatif"), Display(Name = "Jumlah Hasil Hitung Fisik")]
    public int CountedQuantity { get; set; }
}
