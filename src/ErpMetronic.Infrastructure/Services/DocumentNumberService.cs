using ErpMetronic.Domain.Entities;
using ErpMetronic.Domain.Enums;
using ErpMetronic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Infrastructure.Services;

public class DocumentNumberService : IDocumentNumberService
{
    private readonly ApplicationDbContext _db;
    public DocumentNumberService(ApplicationDbContext db) => _db = db;

    public async Task<string> NextAsync(string code, DateTime date)
    {
        var seq = await _db.DocumentNumberSequences.FirstOrDefaultAsync(s => s.Code == code);
        if (seq is null)
        {
            // Fallback aman bila konfigurasi belum ada: buat default memakai kode sebagai prefix.
            seq = new DocumentNumberSequence
            {
                Code = code,
                Name = code,
                Prefix = code,
                Format = "{PREFIX}-{YYYY}{MM}-{SEQ}",
                Padding = 4,
                NextNumber = 1,
                ResetPeriod = NumberResetPeriod.Monthly
            };
            _db.DocumentNumberSequences.Add(seq);
        }

        // Reset counter bila periode berganti.
        if (seq.ResetPeriod == NumberResetPeriod.Yearly && seq.LastResetYear != date.Year)
        {
            seq.NextNumber = 1;
            seq.LastResetYear = date.Year;
            seq.LastResetMonth = null;
        }
        else if (seq.ResetPeriod == NumberResetPeriod.Monthly &&
                 (seq.LastResetYear != date.Year || seq.LastResetMonth != date.Month))
        {
            seq.NextNumber = 1;
            seq.LastResetYear = date.Year;
            seq.LastResetMonth = date.Month;
        }

        var current = seq.NextNumber;
        var text = IDocumentNumberService.Format(seq, date, current);
        seq.NextNumber = current + 1;
        seq.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return text;
    }
}
