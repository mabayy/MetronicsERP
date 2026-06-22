using System.ComponentModel.DataAnnotations;

namespace ErpMetronic.Web.ViewModels;

/// <summary>Baris saran pembelian (produk di bawah titik reorder).</summary>
public class SuggestionRow
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Stock { get; set; }
    public int ReorderLevel { get; set; }
    public int SuggestedQty { get; set; }
    public decimal PurchasePrice { get; set; }
}

public class ReorderPoViewModel
{
    [Required(ErrorMessage = "Pemasok wajib dipilih"), Display(Name = "Pemasok")]
    public int SupplierId { get; set; }

    [Required(ErrorMessage = "Gudang wajib dipilih"), Display(Name = "Gudang Tujuan")]
    public int WarehouseId { get; set; }

    public List<ReorderLineInput> Lines { get; set; } = new();
}

public class ReorderLineInput
{
    public bool Selected { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
