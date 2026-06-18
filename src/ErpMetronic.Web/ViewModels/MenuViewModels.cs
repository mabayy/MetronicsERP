using System.ComponentModel.DataAnnotations;

namespace ErpMetronic.Web.ViewModels;

public class MenuItemViewModel
{
    public int Id { get; set; }

    [Required, Display(Name = "Judul"), StringLength(80)]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Ikon (Bootstrap Icons)"), StringLength(50)]
    public string? Icon { get; set; }

    [Display(Name = "Controller"), StringLength(80)]
    public string? Controller { get; set; }

    [Display(Name = "Action"), StringLength(80)]
    public string? Action { get; set; }

    [Display(Name = "URL Kustom"), StringLength(250)]
    public string? Url { get; set; }

    [Display(Name = "Menu Induk")]
    public int? ParentId { get; set; }

    [Display(Name = "Batasi untuk Role")]
    public string? RequiredRole { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    public bool IsSystem { get; set; }
}

/// <summary>Payload AJAX untuk menyusun ulang urutan menu.</summary>
public class ReorderRequest
{
    public int? ParentId { get; set; }
    public int[] Ids { get; set; } = Array.Empty<int>();
}
