using System.ComponentModel.DataAnnotations;

namespace ErpMetronic.Web.ViewModels;

public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Division { get; set; }
    public string? Position { get; set; }
    public bool IsAdministrator { get; set; }
}

public class CreateUserViewModel
{
    [Required, Display(Name = "Nama Lengkap"), StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Display(Name = "Kata Sandi")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Minimal 8 karakter")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password), Display(Name = "Konfirmasi Kata Sandi")]
    [Compare(nameof(Password), ErrorMessage = "Konfirmasi kata sandi tidak cocok")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Divisi/Departemen")]
    public int? DivisionId { get; set; }

    [Display(Name = "Posisi/Jabatan")]
    public int? PositionId { get; set; }

    public bool IsActive { get; set; } = true;
}

public class EditUserViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required, Display(Name = "Nama Lengkap"), StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Divisi/Departemen")]
    public int? DivisionId { get; set; }

    [Display(Name = "Posisi/Jabatan")]
    public int? PositionId { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; }
}
