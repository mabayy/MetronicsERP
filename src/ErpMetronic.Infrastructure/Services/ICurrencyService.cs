using ErpMetronic.Domain.Entities;

namespace ErpMetronic.Infrastructure.Services;

/// <summary>Layanan multi-currency: mata uang dasar, kurs ber-tanggal, dan konversi.</summary>
public interface ICurrencyService
{
    Task<Currency?> GetBaseCurrencyAsync();

    /// <summary>Kurs ke mata uang dasar (unit base per 1 unit mata uang) per tanggal; null bila tidak ada.</summary>
    Task<decimal?> GetRateToBaseAsync(int currencyId, DateTime asOf);

    /// <summary>Konversi nilai antar mata uang pada tanggal tertentu; null bila kurs tidak tersedia.</summary>
    Task<decimal?> ConvertAsync(decimal amount, int fromCurrencyId, int toCurrencyId, DateTime asOf);

    /// <summary>Format nilai sesuai simbol & jumlah desimal mata uang.</summary>
    string Format(decimal amount, Currency currency);
}
