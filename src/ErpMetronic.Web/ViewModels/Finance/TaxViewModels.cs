using System.ComponentModel.DataAnnotations;
using ErpMetronic.Domain.Enums;

namespace ErpMetronic.Web.ViewModels;

public class TaxCreateViewModel
{
    [Required(ErrorMessage = "Kode wajib diisi"), StringLength(20), Display(Name = "Kode")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nama wajib diisi"), StringLength(100), Display(Name = "Nama")]
    public string Name { get; set; } = string.Empty;

    [Range(0, 100, ErrorMessage = "Tarif 0–100%"), Display(Name = "Tarif (%)")]
    public decimal Rate { get; set; }

    [Display(Name = "Jenis")]
    public TaxKind Kind { get; set; } = TaxKind.ValueAdded;

    [Display(Name = "Berlaku Untuk")]
    public TaxApplicability AppliesTo { get; set; } = TaxApplicability.Both;

    [Required(ErrorMessage = "Akun GL wajib dipilih"), StringLength(20), Display(Name = "Akun GL")]
    public string AccountCode { get; set; } = string.Empty;

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}
