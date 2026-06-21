using System.ComponentModel.DataAnnotations;
using ErpMetronic.Domain.Entities;

namespace ErpMetronic.Web.ViewModels;

public class CashBankAccountCreateViewModel
{
    [Required(ErrorMessage = "Kode wajib diisi"), StringLength(20), Display(Name = "Kode")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nama wajib diisi"), StringLength(100), Display(Name = "Nama")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Jenis")]
    public CashBankAccountKind Kind { get; set; } = CashBankAccountKind.Cash;

    [Required(ErrorMessage = "Akun GL wajib dipilih"), StringLength(20), Display(Name = "Akun GL")]
    public string AccountCode { get; set; } = string.Empty;

    [StringLength(100), Display(Name = "Nama Bank")]
    public string? BankName { get; set; }

    [StringLength(50), Display(Name = "No. Rekening")]
    public string? AccountNumber { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}
