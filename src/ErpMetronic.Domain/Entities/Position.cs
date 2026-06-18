using System.ComponentModel.DataAnnotations;
using ErpMetronic.Domain.Common;

namespace ErpMetronic.Domain.Entities;

/// <summary>Posisi / jabatan (master data).</summary>
public class Position : BaseEntity
{
    [Required, StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Description { get; set; }

    /// <summary>Posisi dengan hak administrator (akses penuh ke seluruh menu & administrasi).</summary>
    public bool IsAdministrator { get; set; }
}
