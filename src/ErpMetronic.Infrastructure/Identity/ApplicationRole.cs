using Microsoft.AspNetCore.Identity;

namespace ErpMetronic.Infrastructure.Identity;

/// <summary>Peran/role aplikasi, memperluas IdentityRole dengan deskripsi.</summary>
public class ApplicationRole : IdentityRole
{
    public ApplicationRole() { }

    public ApplicationRole(string roleName) : base(roleName) { }

    public string? Description { get; set; }
}
