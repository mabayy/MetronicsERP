using System.ComponentModel.DataAnnotations;
using ErpMetronic.Domain.Common;

namespace ErpMetronic.Domain.Entities;

/// <summary>
/// Mata uang (master data multi-currency). Tepat satu mata uang menjadi
/// <see cref="IsBaseCurrency"/> (mata uang fungsional/pelaporan) dengan kurs selalu 1.
/// </summary>
public class Currency : BaseEntity
{
    /// <summary>Kode ISO 4217, mis. IDR, USD, EUR (3 huruf, huruf besar).</summary>
    [Required, StringLength(3, MinimumLength = 3)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Simbol tampilan, mis. Rp, $, €.</summary>
    [StringLength(8)]
    public string? Symbol { get; set; }

    /// <summary>Jumlah angka desimal (0–6). IDR biasanya 0/2, USD 2.</summary>
    [Range(0, 6)]
    public int DecimalPlaces { get; set; } = 2;

    /// <summary>Mata uang dasar/fungsional. Hanya boleh satu dan kursnya selalu 1.</summary>
    public bool IsBaseCurrency { get; set; }

    public ICollection<ExchangeRate> ExchangeRates { get; set; } = new List<ExchangeRate>();
}
