using System.ComponentModel.DataAnnotations;

namespace ErpMetronic.Web.ViewModels;

public class ApprovalRuleCreateViewModel
{
    [Required(ErrorMessage = "Nama wajib diisi"), StringLength(100), Display(Name = "Nama Aturan")]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(40), Display(Name = "Jenis Dokumen")]
    public string DocumentType { get; set; } = "PurchaseOrder";

    [Range(0, double.MaxValue, ErrorMessage = "Ambang tidak boleh negatif"), Display(Name = "Ambang Nilai (≥)")]
    public decimal MinAmount { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    /// <summary>Daftar jabatan penyetuju berurutan (level = urutan).</summary>
    public List<int> StepPositionIds { get; set; } = new();
}
