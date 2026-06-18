namespace ErpMetronic.Domain.Common;

/// <summary>
/// Kelas dasar untuk seluruh entitas bisnis. Menyediakan kolom audit standar
/// sehingga setiap tabel memiliki jejak siapa & kapan dibuat/diubah.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
