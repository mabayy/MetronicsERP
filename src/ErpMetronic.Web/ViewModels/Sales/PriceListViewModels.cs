using System.ComponentModel.DataAnnotations;

namespace ErpMetronic.Web.ViewModels;

public class PriceListCreateViewModel
{
    [Required(ErrorMessage = "Kode wajib diisi"), StringLength(20), Display(Name = "Kode")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nama wajib diisi"), StringLength(100), Display(Name = "Nama")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Mata Uang")]
    public int? CurrencyId { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}

public class PriceListItemInput
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal? Price { get; set; }
}

public class PriceListManageViewModel
{
    public int PriceListId { get; set; }
    public string PriceListName { get; set; } = string.Empty;
    public List<PriceListItemInput> Items { get; set; } = new();
}
