using System.ComponentModel.DataAnnotations;

namespace ErpMetronic.Web.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email wajib diisi")]
    [EmailAddress(ErrorMessage = "Format email tidak valid")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kata sandi wajib diisi")]
    [DataType(DataType.Password)]
    [Display(Name = "Kata Sandi")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ingat saya")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
