using System.ComponentModel.DataAnnotations;

namespace ErpMetronic.Web.ViewModels;

public class PaymentTermCreateViewModel
{
    [Required(ErrorMessage = "Kode wajib diisi"), StringLength(20), Display(Name = "Kode")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nama wajib diisi"), StringLength(100), Display(Name = "Nama")]
    public string Name { get; set; } = string.Empty;

    [Range(0, 3650, ErrorMessage = "Hari 0–3650"), Display(Name = "Jatuh Tempo (hari)")]
    public int NetDays { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}
