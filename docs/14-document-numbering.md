# Tahap 14 — Document Numbering (Penomoran Dokumen)

## Tujuan
Master **penomoran dokumen** yang dapat dikustomisasi per jenis dokumen (prefix, format,
padding, reset berkala) sebagai sumber tunggal nomor referensi seluruh dokumen ERP.

## Pendekatan: berbasis **object code** (best practice)

Penomoran **tidak** terikat enum hardcoded. Setiap jenis dokumen diidentifikasi oleh
**`Code`** (string unik, mis. `PO`, `GR`, `MEMO`). Tabel terbuka: kode bawaan ditandai
`IsSystem` (tak bisa dihapus, kode tak bisa diubah), sedangkan **pengguna dapat menambah kode
dokumen sendiri** lewat UI. Aplikasi merujuk kode bawaan melalui konstanta `DocumentCodes`
sehingga dokumen sistem tetap berfungsi.

> Sebelumnya modul ini memakai `enum DocumentType` (hanya 7 jenis, hanya bisa di-edit). Kini
> diganti menjadi kode terbuka agar bisa **ditambah manual** oleh pengguna.

## Model Data

| Entitas | Peran |
|---------|-------|
| `DocumentNumberSequence` | Konfigurasi penomoran per `Code` (unik) |
| `DocumentCodes` (konstanta) | Kode bawaan: PO, GR, DO, IN, OUT, TRF, ADJ |
| `NumberResetPeriod` (enum) | Never, Yearly, Monthly |

Field `DocumentNumberSequence`: `Code`, `Name`, `Prefix`, `Format`, `Padding`, `NextNumber`,
`ResetPeriod`, `LastResetYear/Month`, `IsActive`, `IsSystem`.

### Format & token
Format memakai token yang diganti saat pembuatan nomor:

| Token | Arti |
|-------|------|
| `{PREFIX}` | Prefix (mis. PO) |
| `{YYYY}` / `{YY}` | Tahun 4/2 digit |
| `{MM}` | Bulan 2 digit |
| `{DD}` | Tanggal 2 digit |
| `{SEQ}` | Nomor urut (di-pad sesuai `Padding`) — **wajib ada** |

Contoh: `"{PREFIX}-{YYYY}{MM}-{SEQ}"`, prefix `PO`, padding 4 → **`PO-202606-0001`**.
Contoh kustom: `"{PREFIX}/{YYYY}/{SEQ}"`, padding 5 → **`PO/2026/00010`**.

## Service — `IDocumentNumberService`

```csharp
public async Task<string> NextAsync(string code, DateTime date)
{
    var seq = await _db.DocumentNumberSequences.FirstOrDefaultAsync(s => s.Code == code)
              ?? /* fallback: buat default dengan prefix = code */;
    // reset bila periode (bulan/tahun) berganti dibanding LastReset
    // bentuk nomor dari Format + token, lalu NextNumber++
    await _db.SaveChangesAsync();
    return text;
}
```

- **Reset berkala**: bila `ResetPeriod = Monthly/Yearly` dan periode (`date`) berbeda dari
  `LastResetYear/Month`, `NextNumber` di-reset ke 1.
- **Aman transaksi**: dipanggil di dalam transaksi pemanggil (mis. penerimaan PO), sehingga bila
  operasi gagal & rollback, kenaikan `NextNumber` ikut dibatalkan (tanpa "lompat nomor").
- `IDocumentNumberService.Format(...)` (static) dipakai untuk **pratinjau** tanpa menaikkan counter.

## Integrasi
Semua nomor dokumen kini bersumber dari service ini (menggantikan format hardcoded):

| Dokumen | Pemanggil | Kode (`DocumentCodes`) |
|---------|-----------|------------------------|
| Purchase Order | `PurchaseOrdersController` | `PO` |
| Penerimaan (PO & langsung) | `PurchaseOrdersController`, `GoodsReceiptsController` | `GR` |
| Pengeluaran/Pengiriman | `DeliveryOrdersController` | `DO` |
| Pergerakan stok (masuk/keluar/transfer/penyesuaian) | `StockService` | `IN`/`OUT`/`TRF`/`ADJ` |

## UI — `DocumentNumberingController` (`[Authorize(Roles = Administrator)]`)
- **Index**: daftar konfigurasi + **pratinjau** nomor berikutnya; tombol **Tambah Kode Dokumen**.
- **Create**: pengguna menambah kode sendiri (Kode + Nama + Prefix + Format + Padding + Reset).
- **Edit**: ubah Nama, Prefix, Format, Padding, No. Berikutnya, Reset, Status (**Kode tetap**).
- **Delete**: hanya untuk kode **non-sistem**. Validasi: `{SEQ}` wajib, padding 1–10, no. berikutnya ≥ 1, Kode unik.

Menu **Administrasi → Penomoran Dokumen**. Seeder mengisi 7 kode bawaan (`IsSystem`, reset bulanan).

## Migrasi
```bash
dotnet ef migrations add AddDocumentNumbering --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web --output-dir Persistence/Migrations
dotnet ef database update --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web
```

## Migrasi data (enum → code)
Migrasi `AddDocumentCodes` menambah kolom `Code` & `IsSystem`, **memetakan** nilai enum lama
(1→PO, 2→GR, …) ke `Code` lewat SQL, lalu menghapus kolom `DocumentType` dan membuat indeks unik
pada `Code` — sehingga data konfigurasi yang sudah ada tetap utuh.

## Hasil / Verifikasi (teruji end-to-end)
| Skenario | Hasil |
|----------|-------|
| Migrasi data | 7 kode (PO/GR/DO/IN/OUT/TRF/ADJ), semua `IsSystem=1` |
| Tambah kode custom `MEMO` (format `{PREFIX}/{YY}/{SEQ}`) | tersimpan, `IsSystem=0` |
| Hapus kode sistem (PO) | **ditolak** (dilindungi) |
| Hapus kode custom (MEMO) | berhasil |
| Dokumen bawaan (buat PO) setelah refactor | tetap bernomor `PO-202606-0001` |

## Pengembangan Lanjutan
- Penomoran per cabang/gudang atau per pemasok.
- Token tambahan (kode cabang, inisial user) & unique index pada nomor dokumen final sebagai jaminan anti-duplikat.
