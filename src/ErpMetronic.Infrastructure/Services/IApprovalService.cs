using ErpMetronic.Domain.Entities;
using ErpMetronic.Domain.Enums;
using ErpMetronic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Infrastructure.Services;

/// <summary>Mesin persetujuan berjenjang: membuat permintaan dari aturan & memproses keputusan.</summary>
public interface IApprovalService
{
    /// <summary>Buat permintaan persetujuan bila ada aturan aktif yang terpicu. True = perlu persetujuan.</summary>
    Task<bool> CreateRequestAsync(string documentType, int documentId, string? reference, decimal amount, string? user);

    /// <summary>Permintaan aktif (Pending) untuk dokumen tertentu (untuk ditampilkan di Details).</summary>
    Task<ApprovalRequest?> GetForDocumentAsync(string documentType, int documentId);

    /// <summary>Setujui langkah berjalan. Completed=true bila seluruh langkah selesai (dokumen disetujui).</summary>
    Task<(bool Ok, bool Completed, string? Error)> ApproveAsync(int requestId, int? userPositionId, bool isAdmin, string? user, string? note);

    /// <summary>Tolak permintaan pada langkah berjalan.</summary>
    Task<(bool Ok, string? Error)> RejectAsync(int requestId, int? userPositionId, bool isAdmin, string? user, string? note);
}

public class ApprovalService : IApprovalService
{
    private readonly ApplicationDbContext _db;
    public ApprovalService(ApplicationDbContext db) => _db = db;

    public async Task<bool> CreateRequestAsync(string documentType, int documentId, string? reference, decimal amount, string? user)
    {
        // Aturan paling spesifik: ambang tertinggi yang ≤ nilai dokumen.
        var rule = await _db.ApprovalRules.Include(r => r.Steps)
            .Where(r => r.IsActive && r.DocumentType == documentType && r.MinAmount <= amount && r.Steps.Any())
            .OrderByDescending(r => r.MinAmount).FirstOrDefaultAsync();
        if (rule is null) return false;

        var steps = rule.Steps.OrderBy(s => s.Level).ToList();
        var request = new ApprovalRequest
        {
            DocumentType = documentType,
            DocumentId = documentId,
            ReferenceNumber = reference,
            Amount = amount,
            Status = ApprovalStatus.Pending,
            CurrentLevel = steps.First().Level,
            CreatedBy = user,
            Steps = steps.Select(s => new ApprovalStep { Level = s.Level, PositionId = s.PositionId, Status = ApprovalStatus.Pending }).ToList()
        };
        _db.ApprovalRequests.Add(request);
        await _db.SaveChangesAsync();
        return true;
    }

    public Task<ApprovalRequest?> GetForDocumentAsync(string documentType, int documentId)
        => _db.ApprovalRequests.Include(r => r.Steps).ThenInclude(s => s.Position)
            .Where(r => r.DocumentType == documentType && r.DocumentId == documentId)
            .OrderByDescending(r => r.Id).FirstOrDefaultAsync();

    public async Task<(bool Ok, bool Completed, string? Error)> ApproveAsync(int requestId, int? userPositionId, bool isAdmin, string? user, string? note)
    {
        var req = await _db.ApprovalRequests.Include(r => r.Steps).FirstOrDefaultAsync(r => r.Id == requestId);
        if (req is null) return (false, false, "Permintaan tidak ditemukan.");
        if (req.Status != ApprovalStatus.Pending) return (false, false, "Permintaan sudah selesai diproses.");

        var step = req.Steps.FirstOrDefault(s => s.Level == req.CurrentLevel);
        if (step is null) return (false, false, "Langkah tidak valid.");
        if (!isAdmin && step.PositionId != userPositionId) return (false, false, "Anda tidak berwenang menyetujui langkah ini.");

        step.Status = ApprovalStatus.Approved;
        step.DecidedBy = user; step.DecidedAt = DateTime.UtcNow; step.Note = note;

        var next = req.Steps.Where(s => s.Level > req.CurrentLevel).OrderBy(s => s.Level).FirstOrDefault();
        var completed = next is null;
        if (completed) req.Status = ApprovalStatus.Approved;
        else req.CurrentLevel = next!.Level;

        await _db.SaveChangesAsync();
        return (true, completed, null);
    }

    public async Task<(bool Ok, string? Error)> RejectAsync(int requestId, int? userPositionId, bool isAdmin, string? user, string? note)
    {
        var req = await _db.ApprovalRequests.Include(r => r.Steps).FirstOrDefaultAsync(r => r.Id == requestId);
        if (req is null) return (false, "Permintaan tidak ditemukan.");
        if (req.Status != ApprovalStatus.Pending) return (false, "Permintaan sudah selesai diproses.");

        var step = req.Steps.FirstOrDefault(s => s.Level == req.CurrentLevel);
        if (step is null) return (false, "Langkah tidak valid.");
        if (!isAdmin && step.PositionId != userPositionId) return (false, "Anda tidak berwenang menolak langkah ini.");

        step.Status = ApprovalStatus.Rejected;
        step.DecidedBy = user; step.DecidedAt = DateTime.UtcNow; step.Note = note;
        req.Status = ApprovalStatus.Rejected;
        await _db.SaveChangesAsync();
        return (true, null);
    }
}
