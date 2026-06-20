using ErpMetronic.Domain.Entities;
using ErpMetronic.Domain.Enums;
using ErpMetronic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Infrastructure.Services;

/// <summary>Pembulatan uang konsisten (2 desimal, setengah ke atas) agar selaras dengan perhitungan di sisi browser.</summary>
public static class TaxMath
{
    public static decimal R2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

/// <summary>Akses master pajak untuk dropdown & perhitungan snapshot pada transaksi.</summary>
public interface ITaxService
{
    /// <summary>Pajak PPN (VAT) aktif yang berlaku untuk konteks (Sales/Purchase).</summary>
    Task<List<Tax>> GetVatTaxesAsync(TaxApplicability context);

    /// <summary>Pajak PPh (withholding) aktif yang berlaku untuk konteks.</summary>
    Task<List<Tax>> GetWithholdingTaxesAsync(TaxApplicability context);

    /// <summary>Ambil pajak berdasarkan kumpulan id (untuk hitung snapshot).</summary>
    Task<Dictionary<int, Tax>> GetByIdsAsync(IEnumerable<int?> ids);
}

public class TaxService : ITaxService
{
    private readonly ApplicationDbContext _db;
    public TaxService(ApplicationDbContext db) => _db = db;

    public Task<List<Tax>> GetVatTaxesAsync(TaxApplicability context)
        => QueryFor(TaxKind.ValueAdded, context);

    public Task<List<Tax>> GetWithholdingTaxesAsync(TaxApplicability context)
        => QueryFor(TaxKind.Withholding, context);

    private Task<List<Tax>> QueryFor(TaxKind kind, TaxApplicability context)
        => _db.Taxes.Where(t => t.IsActive && t.Kind == kind &&
                (t.AppliesTo == context || t.AppliesTo == TaxApplicability.Both))
            .OrderBy(t => t.Code).ToListAsync();

    public async Task<Dictionary<int, Tax>> GetByIdsAsync(IEnumerable<int?> ids)
    {
        var set = ids.Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
        if (set.Count == 0) return new();
        return await _db.Taxes.Where(t => set.Contains(t.Id)).ToDictionaryAsync(t => t.Id);
    }
}
