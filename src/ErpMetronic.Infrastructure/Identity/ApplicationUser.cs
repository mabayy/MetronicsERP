using Microsoft.AspNetCore.Identity;

namespace ErpMetronic.Infrastructure.Identity;

/// <summary>Pengguna aplikasi, memperluas IdentityUser dengan kolom profil.</summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
