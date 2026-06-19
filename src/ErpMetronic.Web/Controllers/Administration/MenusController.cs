using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Identity;
using ErpMetronic.Infrastructure.Persistence;
using ErpMetronic.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

[Authorize(Roles = AppRoles.Administrator)]
public class MenusController : Controller
{
    private readonly ApplicationDbContext _db;
    public MenusController(ApplicationDbContext db) => _db = db;

    // Daftar menu dalam bentuk pohon (induk → anak), siap di-drag untuk reorder.
    public async Task<IActionResult> Index()
    {
        var all = await _db.MenuItems
            .OrderBy(m => m.SortOrder).ThenBy(m => m.Title)
            .ToListAsync();
        return View(all);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateSelectsAsync();
        return View(new MenuItemViewModel { Action = "Index", IsActive = true });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MenuItemViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSelectsAsync(model.ParentId);
            return View(model);
        }

        var nextOrder = await NextSortOrderAsync(model.ParentId);
        var entity = new MenuItem
        {
            Title = model.Title,
            Icon = Normalize(model.Icon),
            Controller = Normalize(model.Controller),
            Action = Normalize(model.Action),
            Url = Normalize(model.Url),
            ParentId = model.ParentId,
            RequiredRole = Normalize(model.RequiredRole),
            IsActive = model.IsActive,
            SortOrder = nextOrder,
            CreatedBy = User.Identity?.Name
        };
        foreach (var divId in model.DivisionIds.Distinct())
            entity.AllowedDivisions.Add(new MenuItemDivision { DivisionId = divId });
        foreach (var posId in model.PositionIds.Distinct())
            entity.AllowedPositions.Add(new MenuItemPosition { PositionId = posId });

        _db.MenuItems.Add(entity);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Menu berhasil ditambahkan.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.MenuItems
            .Include(m => m.AllowedDivisions)
            .Include(m => m.AllowedPositions)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (item is null) return NotFound();

        await PopulateSelectsAsync(item.ParentId, item.Id);
        return View(new MenuItemViewModel
        {
            Id = item.Id,
            Title = item.Title,
            Icon = item.Icon,
            Controller = item.Controller,
            Action = item.Action,
            Url = item.Url,
            ParentId = item.ParentId,
            RequiredRole = item.RequiredRole,
            DivisionIds = item.AllowedDivisions.Select(a => a.DivisionId).ToList(),
            PositionIds = item.AllowedPositions.Select(a => a.PositionId).ToList(),
            IsActive = item.IsActive,
            IsSystem = item.IsSystem
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MenuItemViewModel model)
    {
        var item = await _db.MenuItems
            .Include(m => m.AllowedDivisions)
            .Include(m => m.AllowedPositions)
            .FirstOrDefaultAsync(m => m.Id == model.Id);
        if (item is null) return NotFound();

        if (model.ParentId == item.Id)
            ModelState.AddModelError(nameof(model.ParentId), "Menu tidak boleh menjadi induk dirinya sendiri.");

        if (!ModelState.IsValid)
        {
            await PopulateSelectsAsync(model.ParentId, item.Id);
            return View(model);
        }

        // Bila induk berubah, tempatkan di urutan terakhir grup baru.
        if (item.ParentId != model.ParentId)
            item.SortOrder = await NextSortOrderAsync(model.ParentId);

        item.Title = model.Title;
        item.Icon = Normalize(model.Icon);
        item.Controller = Normalize(model.Controller);
        item.Action = Normalize(model.Action);
        item.Url = Normalize(model.Url);
        item.ParentId = model.ParentId;
        item.RequiredRole = Normalize(model.RequiredRole);
        item.IsActive = model.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = User.Identity?.Name;

        // Sinkronkan hak akses divisi & posisi
        var divIds = model.DivisionIds.Distinct().ToHashSet();
        var posIds = model.PositionIds.Distinct().ToHashSet();
        item.AllowedDivisions.Clear();
        item.AllowedPositions.Clear();
        foreach (var d in divIds) item.AllowedDivisions.Add(new MenuItemDivision { MenuItemId = item.Id, DivisionId = d });
        foreach (var p in posIds) item.AllowedPositions.Add(new MenuItemPosition { MenuItemId = item.Id, PositionId = p });

        await _db.SaveChangesAsync();
        TempData["Success"] = "Menu berhasil diperbarui.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.MenuItems.Include(m => m.Children).FirstOrDefaultAsync(m => m.Id == id);
        if (item is null) return NotFound();

        if (item.IsSystem)
        {
            TempData["Error"] = "Menu bawaan sistem tidak dapat dihapus (silakan nonaktifkan saja).";
            return RedirectToAction(nameof(Index));
        }

        if (item.Children.Any())
            _db.MenuItems.RemoveRange(item.Children);
        _db.MenuItems.Remove(item);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Menu berhasil dihapus.";
        return RedirectToAction(nameof(Index));
    }

    // Reorder via drag-and-drop (AJAX). Menyusun ulang SortOrder dan memindahkan induk.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder([FromBody] ReorderRequest request)
    {
        if (request?.Ids is not { Length: > 0 }) return Json(new { success = true });

        var items = await _db.MenuItems.Where(m => request.Ids.Contains(m.Id)).ToListAsync();
        for (var i = 0; i < request.Ids.Length; i++)
        {
            var item = items.FirstOrDefault(x => x.Id == request.Ids[i]);
            if (item is null) continue;
            item.SortOrder = i + 1;
            item.ParentId = request.ParentId;
            item.UpdatedAt = DateTime.UtcNow;
            item.UpdatedBy = User.Identity?.Name;
        }
        await _db.SaveChangesAsync();
        return Json(new { success = true });
    }

    private async Task<int> NextSortOrderAsync(int? parentId)
    {
        var max = await _db.MenuItems
            .Where(m => m.ParentId == parentId)
            .Select(m => (int?)m.SortOrder)
            .MaxAsync();
        return (max ?? 0) + 1;
    }

    private async Task PopulateSelectsAsync(int? selectedParent = null, int? excludeId = null)
    {
        // Hanya item level atas (tanpa induk) yang boleh menjadi induk → hierarki 1 tingkat.
        var parents = await _db.MenuItems
            .Where(m => m.ParentId == null && (excludeId == null || m.Id != excludeId))
            .OrderBy(m => m.SortOrder)
            .ToListAsync();

        ViewBag.Parents = new SelectList(parents, "Id", "Title", selectedParent);
        ViewBag.Divisions = await _db.Divisions.OrderBy(d => d.Name).ToListAsync();
        ViewBag.Positions = await _db.Positions.OrderBy(p => p.Name).ToListAsync();
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
