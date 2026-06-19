using ErpMetronic.Domain.Entities;
using ErpMetronic.Domain.Enums;
using ErpMetronic.Infrastructure.Identity;
using ErpMetronic.Infrastructure.Persistence;
using ErpMetronic.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

/// <summary>Master penomoran dokumen — pengguna dapat menambah kode dokumen sendiri.</summary>
[Authorize(Roles = AppRoles.Administrator)]
public class DocumentNumberingController : Controller
{
    private readonly ApplicationDbContext _db;
    public DocumentNumberingController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var items = await _db.DocumentNumberSequences.OrderByDescending(s => s.IsSystem).ThenBy(s => s.Code).ToListAsync();
        ViewBag.Preview = items.ToDictionary(s => s.Id, s => IDocumentNumberService.Format(s, DateTime.Today, s.NextNumber));
        return View(items);
    }

    public IActionResult Create()
        => View(new DocumentNumberSequence { Format = "{PREFIX}-{YYYY}{MM}-{SEQ}", Padding = 4, NextNumber = 1, ResetPeriod = NumberResetPeriod.Monthly, IsActive = true });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DocumentNumberSequence model)
    {
        model.Code = (model.Code ?? string.Empty).Trim().ToUpperInvariant();
        Validate(model);
        if (await _db.DocumentNumberSequences.AnyAsync(s => s.Code == model.Code))
            ModelState.AddModelError(nameof(model.Code), "Kode dokumen sudah dipakai.");

        if (!ModelState.IsValid)
        {
            ViewBag.Preview = IDocumentNumberService.Format(model, DateTime.Today, Math.Max(model.NextNumber, 1));
            return View(model);
        }

        model.IsSystem = false;
        model.Prefix = string.IsNullOrWhiteSpace(model.Prefix) ? null : model.Prefix.Trim();
        model.CreatedBy = User.Identity?.Name;
        _db.DocumentNumberSequences.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Kode dokumen '{model.Code}' berhasil ditambahkan.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.DocumentNumberSequences.FindAsync(id);
        if (item is null) return NotFound();
        ViewBag.Preview = IDocumentNumberService.Format(item, DateTime.Today, item.NextNumber);
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DocumentNumberSequence model)
    {
        var item = await _db.DocumentNumberSequences.FindAsync(id);
        if (item is null) return NotFound();

        Validate(model);
        if (!ModelState.IsValid)
        {
            model.Code = item.Code; // kode tidak berubah
            model.IsSystem = item.IsSystem;
            ViewBag.Preview = IDocumentNumberService.Format(model, DateTime.Today, Math.Max(model.NextNumber, 1));
            return View(model);
        }

        // Kode bersifat tetap (kunci rujukan aplikasi); hanya field lain yang diubah.
        item.Name = model.Name;
        item.Prefix = string.IsNullOrWhiteSpace(model.Prefix) ? null : model.Prefix.Trim();
        item.Format = model.Format.Trim();
        item.Padding = model.Padding;
        item.NextNumber = model.NextNumber;
        item.ResetPeriod = model.ResetPeriod;
        item.IsActive = model.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = User.Identity?.Name;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Penomoran dokumen berhasil diperbarui.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.DocumentNumberSequences.FindAsync(id);
        if (item is null) return NotFound();
        if (item.IsSystem)
        {
            TempData["Error"] = "Kode dokumen bawaan sistem tidak dapat dihapus.";
            return RedirectToAction(nameof(Index));
        }
        _db.DocumentNumberSequences.Remove(item);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Kode dokumen berhasil dihapus.";
        return RedirectToAction(nameof(Index));
    }

    private void Validate(DocumentNumberSequence model)
    {
        if (string.IsNullOrWhiteSpace(model.Code))
            ModelState.AddModelError(nameof(model.Code), "Kode wajib diisi.");
        if (string.IsNullOrWhiteSpace(model.Format) || !model.Format.Contains("{SEQ}"))
            ModelState.AddModelError(nameof(model.Format), "Format wajib memuat token {SEQ}.");
        if (model.Padding is < 1 or > 10)
            ModelState.AddModelError(nameof(model.Padding), "Padding antara 1–10.");
        if (model.NextNumber < 1)
            ModelState.AddModelError(nameof(model.NextNumber), "Nomor berikutnya minimal 1.");
    }
}
