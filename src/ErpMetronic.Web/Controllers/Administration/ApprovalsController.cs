using ErpMetronic.Domain.Entities;
using ErpMetronic.Domain.Enums;
using ErpMetronic.Infrastructure.Identity;
using ErpMetronic.Infrastructure.Persistence;
using ErpMetronic.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Web.Controllers;

/// <summary>Kotak Persetujuan: daftar permintaan menunggu keputusan & aksi setuju/tolak.</summary>
[Authorize]
public class ApprovalsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IApprovalService _approval;
    private readonly UserManager<ApplicationUser> _users;

    public ApprovalsController(ApplicationDbContext db, IApprovalService approval, UserManager<ApplicationUser> users)
    {
        _db = db;
        _approval = approval;
        _users = users;
    }

    public async Task<IActionResult> Index()
    {
        var (posId, isAdmin) = await MeAsync();
        var pending = await _db.ApprovalRequests.Include(r => r.Steps).ThenInclude(s => s.Position)
            .Where(r => r.Status == ApprovalStatus.Pending)
            .OrderByDescending(r => r.Id).ToListAsync();
        // Hanya tampilkan yang langkah berjalannya menjadi tanggung jawab jabatan saya (admin: semua).
        var mine = pending.Where(r => isAdmin || r.Steps.Any(s => s.Level == r.CurrentLevel && s.PositionId == posId)).ToList();
        ViewBag.IsAdmin = isAdmin;
        return View(mine);
    }

    public async Task<IActionResult> Details(int id)
    {
        var req = await _db.ApprovalRequests.Include(r => r.Steps).ThenInclude(s => s.Position)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (req is null) return NotFound();
        var (posId, isAdmin) = await MeAsync();
        var current = req.Steps.FirstOrDefault(s => s.Level == req.CurrentLevel);
        ViewBag.CanDecide = req.Status == ApprovalStatus.Pending && current is not null && (isAdmin || current.PositionId == posId);
        return View(req);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? note)
    {
        var (posId, isAdmin) = await MeAsync();
        var req = await _db.ApprovalRequests.FindAsync(id);
        if (req is null) return NotFound();

        var (ok, completed, error) = await _approval.ApproveAsync(id, posId, isAdmin, User.Identity?.Name, note);
        if (!ok) { TempData["Error"] = error; return RedirectToAction(nameof(Details), new { id }); }

        if (completed && req.DocumentType == "PurchaseOrder")
        {
            var po = await _db.PurchaseOrders.FindAsync(req.DocumentId);
            if (po is not null && po.Status == PurchaseOrderStatus.PendingApproval)
            {
                po.Status = PurchaseOrderStatus.Ordered;
                await _db.SaveChangesAsync();
            }
        }
        TempData["Success"] = completed ? "Disetujui sepenuhnya — dokumen dilanjutkan." : "Langkah disetujui, diteruskan ke penyetuju berikutnya.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? note)
    {
        var (posId, isAdmin) = await MeAsync();
        var req = await _db.ApprovalRequests.FindAsync(id);
        if (req is null) return NotFound();

        var (ok, error) = await _approval.RejectAsync(id, posId, isAdmin, User.Identity?.Name, note);
        if (!ok) { TempData["Error"] = error; return RedirectToAction(nameof(Details), new { id }); }

        if (req.DocumentType == "PurchaseOrder")
        {
            var po = await _db.PurchaseOrders.FindAsync(req.DocumentId);
            if (po is not null && po.Status == PurchaseOrderStatus.PendingApproval)
            {
                po.Status = PurchaseOrderStatus.Draft; // kembalikan ke Draft untuk revisi
                await _db.SaveChangesAsync();
            }
        }
        TempData["Success"] = "Permintaan ditolak; dokumen dikembalikan ke Draft.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<(int? PositionId, bool IsAdmin)> MeAsync()
    {
        var u = await _users.GetUserAsync(User);
        return (u?.PositionId, User.IsInRole(AppRoles.Administrator));
    }
}
