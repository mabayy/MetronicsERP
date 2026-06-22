using System.ComponentModel.DataAnnotations;

namespace ErpMetronic.Web.ViewModels;

public class GoodsReceiptCreateViewModel
{
    [Required(ErrorMessage = "Pemasok wajib dipilih"), Display(Name = "Pemasok")]
    public int SupplierId { get; set; }

    [Required(ErrorMessage = "Gudang wajib dipilih"), Display(Name = "Gudang Tujuan")]
    public int WarehouseId { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Tanggal Terima")]
    public DateTime ReceiptDate { get; set; } = DateTime.Today;

    [Display(Name = "Catatan"), StringLength(300)]
    public string? Note { get; set; }

    public List<ReceiptLineInput> Lines { get; set; } = new();
}

public class ReceiptLineInput
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }

    /// <summary>Untuk produk berlacak Lot: nomor batch & kedaluwarsa (opsional).</summary>
    public string? LotNumber { get; set; }
    [DataType(DataType.Date)]
    public DateTime? ExpiryDate { get; set; }

    /// <summary>Untuk produk berlacak Serial: nomor seri, pisahkan dengan koma/baris baru.</summary>
    public string? Serials { get; set; }
}

public class DeliveryCreateViewModel
{
    [Required(ErrorMessage = "Pelanggan wajib dipilih"), Display(Name = "Pelanggan")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Gudang wajib dipilih"), Display(Name = "Gudang Sumber")]
    public int WarehouseId { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Tanggal Kirim")]
    public DateTime DeliveryDate { get; set; } = DateTime.Today;

    [Display(Name = "Catatan"), StringLength(300)]
    public string? Note { get; set; }

    public List<DeliveryLineInput> Lines { get; set; } = new();
}

public class DeliveryLineInput
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
