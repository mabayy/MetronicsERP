using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Identity;
using ErpMetronic.Infrastructure.Persistence;
using ErpMetronic.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

/// <summary>Master Aturan Persetujuan (approval procedure) berjenjang per jabatan.</summary>
[Authorize(Roles = AppRoles.Administrator)]
public class ApprovalRulesController : Controller
{
    private readonly ApplicationDbContext _db;
    public ApprovalRulesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
        => View(await _db.ApprovalRules.Include(r => r.Steps).ThenInclude(s => s.Position)
            .OrderBy(r => r.DocumentType).ThenBy(r => r.MinAmount).ToListAsync());

    public async Task<IActionResult> Create() { await PopulateAsync(); return View(new ApprovalRuleCreateViewModel()); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ApprovalRuleCreateViewModel model)
    {
        var positions = model.StepPositionIds.Where(p => p > 0).ToList();
        if (positions.Count == 0) ModelState.AddModelError(string.Empty, "Minimal satu langkah penyetuju.");
        if (!ModelState.IsValid) { await PopulateAsync(); return View(model); }

        var rule = new ApprovalRule
        {
            Name = model.Name, DocumentType = model.DocumentType, MinAmount = model.MinAmount, IsActive = model.IsActive,
            CreatedBy = User.Identity?.Name,
            Steps = positions.Select((pid, idx) => new ApprovalRuleStep { Level = idx + 1, PositionId = pid }).ToList()
        };
        _db.ApprovalRules.Add(rule);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Aturan persetujuan ditambahkan.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var rule = await _db.ApprovalRules.Include(r => r.Steps).FirstOrDefaultAsync(r => r.Id == id);
        if (rule is null) return NotFound();
        await PopulateAsync();
        ViewBag.RuleId = rule.Id;
        return View(new ApprovalRuleCreateViewModel
        {
            Name = rule.Name, DocumentType = rule.DocumentType, MinAmount = rule.MinAmount, IsActive = rule.IsActive,
            StepPositionIds = rule.Steps.OrderBy(s => s.Level).Select(s => s.PositionId).ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ApprovalRuleCreateViewModel model)
    {
        var rule = await _db.ApprovalRules.Include(r => r.Steps).FirstOrDefaultAsync(r => r.Id == id);
        if (rule is null) return NotFound();
        var positions = model.StepPositionIds.Where(p => p > 0).ToList();
        if (positions.Count == 0) ModelState.AddModelError(string.Empty, "Minimal satu langkah penyetuju.");
        if (!ModelState.IsValid) { await PopulateAsync(); ViewBag.RuleId = rule.Id; return View(model); }

        rule.Name = model.Name; rule.DocumentType = model.DocumentType; rule.MinAmount = model.MinAmount; rule.IsActive = model.IsActive;
        rule.UpdatedAt = DateTime.UtcNow; rule.UpdatedBy = User.Identity?.Name;
        _db.ApprovalRuleSteps.RemoveRange(rule.Steps);
        rule.Steps = positions.Select((pid, idx) => new ApprovalRuleStep { Level = idx + 1, PositionId = pid }).ToList();
        await _db.SaveChangesAsync();
        TempData["Success"] = "Aturan persetujuan diperbarui.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var rule = await _db.ApprovalRules.Include(r => r.Steps).FirstOrDefaultAsync(r => r.Id == id);
        if (rule is null) return NotFound();
        _db.ApprovalRuleSteps.RemoveRange(rule.Steps);
        _db.ApprovalRules.Remove(rule);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Aturan persetujuan dihapus.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateAsync()
    {
        ViewBag.Positions = await _db.Positions.OrderBy(p => p.Name)
            .Select(p => new { p.Id, Display = p.Name }).ToListAsync();
        ViewBag.DocumentTypes = new SelectList(new[] { "PurchaseOrder" });
    }
}
