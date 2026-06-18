using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.ViewComponents;

/// <summary>
/// Merender menu sidebar dari database (master menu). Memfilter item berdasarkan
/// status aktif dan role pengguna, lalu menyusun pohon induk → anak terurut.
/// </summary>
public class SidebarMenuViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _db;
    public SidebarMenuViewComponent(ApplicationDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var items = await _db.MenuItems
            .Where(m => m.IsActive)
            .OrderBy(m => m.SortOrder).ThenBy(m => m.Title)
            .ToListAsync();

        bool Visible(MenuItem m) =>
            string.IsNullOrEmpty(m.RequiredRole) || (User?.IsInRole(m.RequiredRole) ?? false);

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
