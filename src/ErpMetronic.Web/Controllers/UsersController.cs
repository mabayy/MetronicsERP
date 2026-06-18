using ErpMetronic.Infrastructure.Identity;
using ErpMetronic.Infrastructure.Persistence;
using ErpMetronic.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

[Authorize(Roles = AppRoles.Administrator)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public UsersController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var divisions = await _db.Divisions.ToDictionaryAsync(d => d.Id, d => d.Name);
        var positions = await _db.Positions.ToDictionaryAsync(p => p.Id, p => p);

        var users = await _userManager.Users.ToListAsync();
        var list = users.Select(u => new UserListItemViewModel
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email ?? string.Empty,
            IsActive = u.IsActive,
            Division = u.DivisionId.HasValue && divisions.TryGetValue(u.DivisionId.Value, out var dn) ? dn : null,
            Position = u.PositionId.HasValue && positions.TryGetValue(u.PositionId.Value, out var pos) ? pos.Name : null,
            IsAdministrator = u.PositionId.HasValue && positions.TryGetValue(u.PositionId.Value, out var p2) && p2.IsAdministrator
        }).ToList();
        return View(list);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateLookupsAsync();
        return View(new CreateUserViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(model.DivisionId, model.PositionId);
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            IsActive = model.IsActive,
            DivisionId = model.DivisionId,
            PositionId = model.PositionId,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors) ModelState.AddModelError(string.Empty, err.Description);
            await PopulateLookupsAsync(model.DivisionId, model.PositionId);
            return View(model);
        }

        TempData["Success"] = "Pengguna berhasil ditambahkan.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var model = new EditUserViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive,
            DivisionId = user.DivisionId,
            PositionId = user.PositionId
        };
        await PopulateLookupsAsync(user.DivisionId, user.PositionId);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(model.DivisionId, model.PositionId);
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user is null) return NotFound();

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.UserName = model.Email;
        user.IsActive = model.IsActive;
        user.DivisionId = model.DivisionId;
        user.PositionId = model.PositionId;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = "Pengguna berhasil diperbarui.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        if (string.Equals(user.Email, ErpMetronic.Infrastructure.Persistence.DbSeeder.DefaultAdminEmail, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Akun administrator default tidak dapat dihapus.";
            return RedirectToAction(nameof(Index));
        }

        await _userManager.DeleteAsync(user);
        TempData["Success"] = "Pengguna berhasil dihapus.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateLookupsAsync(int? division = null, int? position = null)
    {
        ViewBag.Divisions = new SelectList(await _db.Divisions.OrderBy(d => d.Name).ToListAsync(), "Id", "Name", division);
        ViewBag.Positions = new SelectList(await _db.Positions.OrderBy(p => p.Name).ToListAsync(), "Id", "Name", position);
    }
}
