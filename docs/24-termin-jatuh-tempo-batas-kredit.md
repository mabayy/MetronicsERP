# Tahap 24 — Termin Pembayaran, Jatuh Tempo & Batas Kredit

## Tujuan
Kontrol piutang/hutang ala SAP B1/Odoo:
- **Termin Pembayaran (Payment Term)** master yang dapat dipakai ulang (mis. Net 30).
- **Jatuh Tempo (Due Date)** faktur otomatis = tanggal faktur + termin.
- **Batas Kredit (Credit Limit)** pelanggan — faktur penjualan diblokir bila eksposur melebihi batas.
- **Aging** dihitung berdasarkan **jatuh tempo** (bukan tanggal faktur).

## Model Data
| Entitas | Tambahan |
|---------|----------|
| `PaymentTerm` (master) | `Code`, `Name`, `NetDays`, `IsActive`, `IsSystem` |
| `Customer` | `CreditLimit` (0 = tanpa batas), `PaymentTermId` (termin default) |
| `Supplier` | `PaymentTermId` (termin default) |
| `PurchaseInvoice` / `SalesInvoice` | `PaymentTermId`, `DueDate` |

Termin bawaan (seeder, idempoten): **Tunai (0)**, **Net 7**, **Net 14**, **Net 30**, **Net 60**.
CRUD master di menu **Master Data → Termin Pembayaran** (Administrator); termin sistem tidak bisa dihapus.

## Perilaku
- **Faktur**: termin default diambil dari mitra (pelanggan/pemasok), dapat diubah di form faktur.
  `DueDate = InvoiceDate + NetDays`.
- **Batas kredit** (penjualan): saat membuat faktur, hitung **eksposur** = Σ sisa tagihan faktur
  penjualan pelanggan yang belum lunas; bila `eksposur + total faktur > CreditLimit` → **diblokir**
  dengan pesan, faktur tidak dibuat (cek dilakukan **sebelum** nomor dokumen digenerate agar tidak boros nomor).
- **Aging** (umur piutang/hutang) memakai jatuh tempo: ember **Belum Jatuh Tempo / 1–30 / 31–60 / > 60** hari lewat.
- **Details faktur** menampilkan Jatuh Tempo + Termin, dengan badge **"Jatuh Tempo Lewat"** bila belum lunas & melewati tanggal.

## Migrasi
```bash
dotnet ef migrations add AddPaymentTerms --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web --output-dir Persistence/Migrations
dotnet ef database update --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web
```
Faktur lama di-backfill `DueDate = InvoiceDate`.

## Hasil / Verifikasi (teruji end-to-end)
Pelanggan termin **Net 30**, batas kredit **100.000**:

| Uji | Hasil |
|-----|-------|
| Faktur 200.000 (> batas 100.000) | **diblokir**, faktur tidak dibuat, pesan batas kredit ✅ |
| Batas dinaikkan → faktur 200.000 | dibuat; **tgl 21/06 → jatuh tempo 21/07** (Net 30) ✅ |

Data uji dibersihkan; master pelanggan dikembalikan ke baseline.
