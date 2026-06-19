using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Infrastructure.Services;

public class CurrencyService : ICurrencyService
{
    private readonly ApplicationDbContext _db;
    public CurrencyService(ApplicationDbContext db) => _db = db;

    public Task<Currency?> GetBaseCurrencyAsync()
        => _db.Currencies.FirstOrDefaultAsync(c => c.IsBaseCurrency);

    public async Task<decimal?> GetRateToBaseAsync(int currencyId, DateTime asOf)
    {
        var currency = await _db.Currencies.FindAsync(currencyId);
        if (currency is null) return null;

        // Mata uang dasar selalu berkurs 1.
        if (currency.IsBaseCurrency) return 1m;

        // Ambil kurs terakhir yang berlaku pada/sebelum tanggal acuan.
        return await _db.ExchangeRates
            .Where(r => r.CurrencyId == currencyId && r.EffectiveDate <= asOf)
            .OrderByDescending(r => r.EffectiveDate)
            .Select(r => (decimal?)r.Rate)
            .FirstOrDefaultAsync();
    }

    public async Task<decimal?> ConvertAsync(decimal amount, int fromCurrencyId, int toCurrencyId, DateTime asOf)
    {
        if (fromCurrencyId == toCurrencyId) return amount;

        var rateFrom = await GetRateToBaseAsync(fromCurrencyId, asOf);
        var rateTo = await GetRateToBaseAsync(toCurrencyId, asOf);
        if (rateFrom is null || rateTo is null || rateTo == 0m) return null;

        var amountInBase = amount * rateFrom.Value;
        var result = amountInBase / rateTo.Value;

        var target = await _db.Currencies.FindAsync(toCurrencyId);
        var decimals = target?.DecimalPlaces ?? 2;
        return Math.Round(result, decimals, MidpointRounding.AwayFromZero);
    }

    public string Format(decimal amount, Currency currency)
    {
        var text = amount.ToString("N" + currency.DecimalPlaces);
        return string.IsNullOrEmpty(currency.Symbol) ? $"{currency.Code} {text}" : $"{currency.Symbol} {text}";
    }
}
