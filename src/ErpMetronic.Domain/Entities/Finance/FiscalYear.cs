using ErpMetronic.Domain.Common;

namespace ErpMetronic.Domain.Entities;

public enum FiscalYearStatus
{
    Open = 1,
    Closed = 2
}

/// <summary>
/// Status tutup buku per tahun fiskal. Tahun yang ditutup mengunci posting jurnal pada tahun
/// tersebut & sebelumnya, serta memiliki jurnal penutup (laba/rugi → Laba Ditahan).
/// </summary>
public class FiscalYear : BaseEntity
{
    public int Year { get; set; }

    public FiscalYearStatus Status { get; set; } = FiscalYearStatus.Open;

    public DateTime? ClosedAt { get; set; }
    public string? ClosedBy { get; set; }
}
