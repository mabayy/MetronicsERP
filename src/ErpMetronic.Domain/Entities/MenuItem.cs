using System.ComponentModel.DataAnnotations;
using ErpMetronic.Domain.Common;

namespace ErpMetronic.Domain.Entities;

/// <summary>
/// Item menu navigasi yang dapat dikelola pengguna (master menu).
/// Mendukung hierarki satu tingkat: item induk (header grup) dengan anak-anaknya,
/// serta pengurutan via <see cref="SortOrder"/>.
/// </summary>
public class MenuItem : BaseEntity
{
    [Required, StringLength(80)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Kelas ikon Bootstrap Icons, mis. "bi-box".</summary>
    [StringLength(50)]
    public string? Icon { get; set; }

    /// <summary>Nama controller MVC tujuan (mis. "Products"). Kosong untuk header grup.</summary>
    [StringLength(80)]
    public string? Controller { get; set; }

    /// <summary>Nama action, default "Index".</summary>
    [StringLength(80)]
    public string? Action { get; set; }

    /// <summary>URL kustom/eksternal. Bila diisi, diutamakan daripada Controller/Action.</summary>
    [StringLength(250)]
    public string? Url { get; set; }

    /// <summary>Urutan tampil di antara item sejajar (semakin kecil semakin atas).</summary>
    public int SortOrder { get; set; }

    /// <summary>Bila diisi, item hanya tampil untuk pengguna pada role tersebut.</summary>
    [StringLength(80)]
    public string? RequiredRole { get; set; }

    public int? ParentId { get; set; }
    public MenuItem? Parent { get; set; }
    public ICollection<MenuItem> Children { get; set; } = new List<MenuItem>();

    /// <summary>Divisi yang diizinkan mengakses menu ini (kosong = tidak dibatasi divisi).</summary>
    public ICollection<MenuItemDivision> AllowedDivisions { get; set; } = new List<MenuItemDivision>();

    /// <summary>Posisi yang diizinkan mengakses menu ini (kosong = tidak dibatasi posisi).</summary>
    public ICollection<MenuItemPosition> AllowedPositions { get; set; } = new List<MenuItemPosition>();

    /// <summary>Item bawaan sistem—boleh diedit/diurutkan, tetapi tidak dapat dihapus.</summary>
    public bool IsSystem { get; set; }
}
