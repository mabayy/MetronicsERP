using System.Security.Claims;
using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Identity;
using ErpMetronic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.ViewComponents;

/// <summary>
/// Merender menu sidebar dari database (master menu). Memfilter item berdasarkan
/// status aktif, role, serta hak akses divisi/posisi pengguna, lalu menyusun pohon
/// induk → anak terurut.
/// </summary>
public class SidebarMenuViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _db;
    public SidebarMenuViewComponent(ApplicationDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var items = await _db.MenuItems
            .Where(m => m.IsActive)
            .Include(m => m.AllowedDivisions)
            .Include(m => m.AllowedPositions)
            .OrderBy(m => m.SortOrder).ThenBy(m => m.Title)
            .ToListAsync();

        var isAdmin = User?.IsInRole(AppRoles.Administrator) ?? false;

        // Divisi & posisi pengguna saat ini (untuk mencocokkan hak akses menu)
        int? divisionId = null, positionId = null;
        var userId = (User as ClaimsPrincipal)?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!isAdmin && userId is not null)
        {
            var profile = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.DivisionId, u.PositionId })
                .FirstOrDefaultAsync();
            divisionId = profile?.DivisionId;
            positionId = profile?.PositionId;
        }

        bool Visible(MenuItem m)
        {
            // Administrator melihat semua menu (untuk pengelolaan).
            if (isAdmin) return true;

            // Batasan role (mis. menu Administrasi) tetap berlaku.
            if (!string.IsNullOrEmpty(m.RequiredRole) && !(User?.IsInRole(m.RequiredRole) ?? false))
                return false;

            var divisionRestricted = m.AllowedDivisions.Count > 0;
            var positionRestricted = m.AllowedPositions.Count > 0;

            // Tanpa batasan divisi/posisi → terbuka untuk semua pengguna login.
            if (!divisionRestricted && !positionRestricted) return true;

            // Berbatas → boleh akses bila divisi ATAU posisi pengguna termasuk yang diizinkan.
            if (divisionRestricted && divisionId is int d && m.AllowedDivisions.Any(a => a.DivisionId == d)) return true;
            if (positionRestricted && positionId is int p && m.AllowedPositions.Any(a => a.PositionId == p)) return true;

            return false;
        }

        var roots = items
            .Where(m => m.ParentId == null && Visible(m))
            .Select(m => new MenuNode
            {
                Item = m,
                Children = items.Where(c => c.ParentId == m.Id && Visible(c))
                                .OrderBy(c => c.SortOrder).ToList()
            })
            // Sembunyikan grup header yang semua anaknya tidak terlihat & tanpa tujuan sendiri.
            .Where(n => n.Children.Count > 0 || HasTarget(n.Item))
            .ToList();

        return View(roots);
    }

    private static bool HasTarget(MenuItem m)
        => !string.IsNullOrEmpty(m.Controller) || !string.IsNullOrEmpty(m.Url);
}

public class MenuNode
{
    public MenuItem Item { get; set; } = default!;
    public List<MenuItem> Children { get; set; } = new();
}
