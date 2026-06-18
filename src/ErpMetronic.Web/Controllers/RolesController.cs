using ErpMetronic.Infrastructure.Identity;
using ErpMetronic.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

[Authorize(Roles = AppRoles.Administrator)]
public class RolesController : Controller
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public RolesController(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        var list = new List<RoleViewModel>();
        foreach (var r in roles)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(r.Name!);
            list.Add(new RoleViewModel { Id = r.Id, Name = r.Name!, Description = r.Description, UserCount = usersInRole.Count });
        }
        return View(list);
    }

    public IActionResult Create() => View(new RoleViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        if (await _roleManager.RoleExistsAsync(model.Name))
        {
            ModelState.AddModelError(nameof(model.Name), "Role dengan nama ini sudah ada.");
            return View(model);
        }
        await _roleManager.CreateAsync(new ApplicationRole(model.Name) { Description = model.Description });
        TempData["Success"] = "Role berhasil ditambahkan.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null) return NotFound();
        return View(new RoleViewModel { Id = role.Id, Name = role.Name!, Description = role.Description });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RoleViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var role = await _roleManager.FindByIdAsync(model.Id!);
        if (role is null) return NotFound();
        role.Name = model.Name;
        role.Description = model.Description;
        await _roleManager.UpdateAsync(role);
        TempData["Success"] = "Role berhasil diperbarui.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null) return NotFound();
        if (AppRoles.All.Contains(role.Name))
        {
            TempData["Error"] = "Role bawaan sistem tidak dapat dihapus.";
            return RedirectToAction(nameof(Index));
        }
        await _roleManager.DeleteAsync(role);
        TempData["Success"] = "Role berhasil dihapus.";
        return RedirectToAction(nameof(Index));
    }
}
