using ErpMetronic.Domain.Entities;

namespace ErpMetronic.Infrastructure.Services;

/// <summary>Menghasilkan nomor dokumen sesuai konfigurasi penomoran (per object code).</summary>
public interface IDocumentNumberService
{
    /// <summary>Ambil nomor berikutnya untuk kode dokumen pada tanggal tertentu (sekaligus menaikkan counter).</summary>
    Task<string> NextAsync(string code, DateTime date);

    /// <summary>Bentuk contoh nomor dari sebuah konfigurasi (untuk pratinjau), tanpa menaikkan counter.</summary>
    static string Format(DocumentNumberSequence s, DateTime date, int seqValue)
    {
        var seqText = seqValue.ToString().PadLeft(Math.Clamp(s.Padding, 1, 10), '0');
        return (s.Format ?? string.Empty)
            .Replace("{PREFIX}", s.Prefix ?? string.Empty)
            .Replace("{YYYY}", date.ToString("yyyy"))
            .Replace("{YY}", date.ToString("yy"))
            .Replace("{MM}", date.ToString("MM"))
            .Replace("{DD}", date.ToString("dd"))
            .Replace("{SEQ}", seqText);
    }
}
