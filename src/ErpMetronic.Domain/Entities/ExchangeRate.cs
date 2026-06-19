using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpMetronic.Domain.Common;

namespace ErpMetronic.Domain.Entities;

/// <summary>
/// Kurs sebuah mata uang terhadap mata uang dasar, berlaku sejak tanggal tertentu
/// (effective-dated). Konvensi: <see cref="Rate"/> = jumlah unit mata uang DASAR untuk
/// 1 unit mata uang ini. Contoh (base IDR): 1 USD = 16.000 → Rate = 16000.
/// </summary>
public class ExchangeRate : BaseEntity
{
    public int CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal Rate { get; set; }

    [DataType(DataType.Date)]
    public DateTime EffectiveDate { get; set; } = DateTime.Today;
}
