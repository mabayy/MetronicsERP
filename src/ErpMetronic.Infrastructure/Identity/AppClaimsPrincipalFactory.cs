using System.Security.Claims;
using ErpMetronic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ErpMetronic.Infrastructure.Identity;

/// <summary>
/// Membentuk klaim pengguna saat sign-in. Hak administrator tidak lagi berasal dari
/// Identity Role, melainkan diturunkan dari <c>Position.IsAdministrator</c>: bila posisi
/// pengguna ditandai administrator, klaim role "Administrator" ditambahkan sehingga
/// atribut [Authorize(Roles = "Administrator")] dan User.IsInRole tetap berfungsi.
/// </summary>
public class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
{
    private readonly ApplicationDbContext _db;

    public AppClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> options,
        ApplicationDbContext db)
        : base(userManager, roleManager, options)
    {
        _db = db;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (user.PositionId is int positionId)
        {
            var isAdmin = await _db.Positions
                .Where(p => p.Id == positionId)
                .Select(p => p.IsAdministrator)
                .FirstOrDefaultAsync();

            if (isAdmin && !identity.HasClaim(ClaimTypes.Role, AppRoles.Administrator))
                identity.AddClaim(new Claim(ClaimTypes.Role, AppRoles.Administrator));
        }

        return identity;
    }
}
