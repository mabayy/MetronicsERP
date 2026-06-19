# Tahap 12 — Currency Management (Multi-Currency)

## Tujuan
Menyediakan fondasi **multi-currency** untuk seluruh ERP: master mata uang dengan satu
**mata uang dasar (functional currency)**, **kurs (exchange rate) ber-tanggal efektif**,
layanan konversi, serta integrasi ke modul (Produk) — dengan business rules ERP yang umum.

## Model Data

| Entitas | Peran |
|---------|-------|
| `Currency` | Master mata uang: `Code` (ISO 4217), `Name`, `Symbol`, `DecimalPlaces`, `IsBaseCurrency` |
| `ExchangeRate` | Kurs ber-tanggal: `CurrencyId`, `Rate`, `EffectiveDate` |
| `Product.CurrencyId` | Mata uang harga produk (multi-currency lintas sistem) |

**Konvensi kurs**: `Rate` = jumlah unit mata uang **dasar** untuk 1 unit mata uang asing.
Contoh (base IDR): `1 USD = 16.000` → `Rate = 16000`. Mata uang dasar selalu berkurs **1**.

Konfigurasi DbContext:
- `Currency.Code` unik; **indeks unik terfilter** `WHERE IsBaseCurrency = 1` menjamin
  hanya satu mata uang dasar di level database.
- `ExchangeRate (CurrencyId, EffectiveDate)` unik; hapus mata uang → kurs ikut terhapus (cascade).
- `Product → Currency` restrict (mata uang dipakai produk tak bisa dihapus).

## Business Rules (umum di ERP)

| Aturan | Implementasi |
|--------|--------------|
| Tepat **satu** mata uang dasar | Indeks unik terfilter + aksi `SetBase` (dua tahap dalam transaksi: cabut base lama → set base baru, menghindari pelanggaran indeks) |
| Mata uang dasar berkurs **1** & tak perlu kurs | `CurrencyService` mengembalikan 1; menambah kurs untuk base **ditolak** |
| Mata uang dasar **tidak dapat dinonaktifkan/dihapus** | Divalidasi di controller |
| Mata uang dasar baru harus **aktif** | `SetBase` menolak mata uang nonaktif |
| Mata uang pertama otomatis jadi dasar | Aturan pada `Create` |
| Kode unik & format ISO (3 huruf, kapital) | Normalisasi + cek duplikat |
| Kurs harus > 0 | Validasi controller |
| **Satu kurs** per (mata uang, tanggal) | Cek duplikat + indeks unik |
| Mata uang dipakai produk **tak bisa dihapus** | Cek referensi produk |
| Konversi pakai **kurs terakhir ≤ tanggal** transaksi | `GetRateToBaseAsync` (effective-dated) |
| Pembulatan sesuai desimal mata uang tujuan | `Math.Round(..., DecimalPlaces)` |

## Layanan Konversi — `CurrencyService` (`ICurrencyService`)

Konversi memakai mata uang dasar sebagai poros:

```
amountBase = amount × rate(from)
result     = amountBase ÷ rate(to)     // dibulatkan ke desimal mata uang tujuan
```

```csharp
public async Task<decimal?> ConvertAsync(decimal amount, int fromId, int toId, DateTime asOf)
{
    if (fromId == toId) return amount;
    var rateFrom = await GetRateToBaseAsync(fromId, asOf);   // base → 1
    var rateTo   = await GetRateToBaseAsync(toId, asOf);
    if (rateFrom is null || rateTo is null || rateTo == 0m) return null; // kurs belum tersedia
    return Math.Round(amount * rateFrom.Value / rateTo.Value, decimalsOf(toId), MidpointRounding.AwayFromZero);
}
```

`GetRateToBaseAsync` mengambil kurs **terakhir yang berlaku pada/sebelum** tanggal acuan
(effective-dated) — inti perilaku multi-currency ERP.

## Controller & Halaman (`[Authorize(Roles = Administrator)]`)

| Controller | Halaman |
|-----------|---------|
| `CurrenciesController` | Index (kurs terkini + tombol **Jadikan Dasar**), Create, Edit, **SetBase**, Delete |
| `ExchangeRatesController` | Index (filter mata uang), Create, Edit, Delete |

Menu **Keuangan** → *Mata Uang* & *Kurs / Exchange Rate* (ditambahkan idempoten oleh seeder).

## Integrasi Multi-Currency ke Sistem
- **Produk** memiliki `CurrencyId`; form produk memuat pemilih mata uang.
- Daftar & detail produk menampilkan harga dalam mata uangnya **plus setara mata uang dasar**
  (mis. `$ 10.00  ≈ Rp 160.000`) memakai `CurrencyService`.
- Pola yang sama dapat dipakai modul lain (pembelian, penjualan, dsb.) untuk menyimpan nilai
  transaksi dalam mata uang dokumen + nilai setara mata uang dasar untuk pelaporan.

## Seeding
IDR (dasar, 0 desimal), USD, EUR + kurs awal (USD 16.000, EUR 17.500 per 2026-01-01).
Produk tanpa mata uang otomatis di-set ke mata uang dasar.

## Migrasi
```bash
dotnet ef migrations add AddCurrency --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web --output-dir Persistence/Migrations
dotnet ef database update --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web
```

## Hasil / Verifikasi (teruji end-to-end)
| Skenario | Hasil |
|----------|-------|
| Tambah kurs baru (USD, tanggal baru) | tersimpan |
| Tambah kurs duplikat (mata uang+tanggal sama) | **ditolak** |
| Tambah kurs untuk mata uang dasar | **ditolak** ("dasar selalu berkurs 1") |
| `SetBase` EUR lalu kembali IDR | base berpindah, jumlah base tetap **1** |
| Hapus / nonaktifkan mata uang dasar | **diblokir** |
| Hapus mata uang non-dasar | berhasil (kurs ikut terhapus) |
| Konversi produk USD $10 → dasar | tampil **≈ Rp 160.000** (10 × 16.000) |

## Catatan & Pengembangan Lanjutan
- Mengubah mata uang dasar tidak otomatis mengkonversi kurs lama—pastikan kurs diperbarui
  (pesan peringatan ditampilkan).
- Lanjutan: kurs jual/beli/tengah, sumber kurs otomatis (API bank sentral), realized/unrealized
  FX gain-loss saat pelunasan, penyimpanan nilai transaksi dalam mata uang dokumen + dasar.
