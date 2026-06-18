using ErpMetronic.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace ErpMetronic.Infrastructure.Identity;

/// <summary>Pengguna aplikasi, memperluas IdentityUser dengan kolom profil.</summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Divisi/departemen pengguna (untuk hak akses menu).</summary>
    public int? DivisionId { get; set; }
    public Division? Division { get; set; }

    /// <summary>Posisi/jabatan pengguna (untuk hak akses menu).</summary>
    public int? PositionId { get; set; }
    public Position? Position { get; set; }
}
