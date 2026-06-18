using ErpMetronic.Infrastructure.Identity;
using ErpMetronic.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

[Authorize(Roles = AppRoles.Administrator)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public UsersController(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.ToListAsync();
        var list = new List<UserListItemViewModel>();
        foreach (var u in users)
        {
            list.Add(new UserListItemViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                IsActive = u.IsActive,
                Roles = await _userManager.GetRolesAsync(u)
            });
        }
        return View(list);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateRolesAsync();
        return View(new CreateUserViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateRolesAsync();
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            IsActive = model.IsActive,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors) ModelState.AddModelError(string.Empty, err.Description);
            await PopulateRolesAsync();
            return View(model);
        }

        if (model.SelectedRoles.Any())
            await _userManager.AddToRolesAsync(user, model.SelectedRoles);

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
            SelectedRoles = (await _userManager.GetRolesAsync(user)).ToList()
        };
        await PopulateRolesAsync();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateRolesAsync();
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user is null) return NotFound();

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.UserName = model.Email;
        user.IsActive = model.IsActive;
        await _userManager.UpdateAsync(user);

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles.Except(model.SelectedRoles));
        await _userManager.AddToRolesAsync(user, model.SelectedRoles.Except(currentRoles));

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

    private async Task PopulateRolesAsync()
        => ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
}
