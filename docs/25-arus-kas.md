# Tahap 25 — Laporan Arus Kas (Cash Flow)

## Tujuan
Melengkapi trio laporan keuangan ([Laba Rugi & Neraca](23-laporan-keuangan.md)) dengan **Laporan Arus
Kas** berbasis kas (cash basis): mutasi akun **Kas/Bank (1100)** dalam satu periode, dikelompokkan per
kategori, dengan saldo awal → akhir.

## Logika
Mengambil seluruh `JournalLine` pada akun Kas (`AccountCodes.Cash`):
- **Saldo Awal** = Σ(debit − kredit) sebelum tanggal mulai.
- **Kas Masuk** = debit; **Kas Keluar** = kredit (per baris jurnal).
- **Saldo Akhir** = Saldo Awal + (Total Masuk − Total Keluar).
- **Kategori** diturunkan dari `JournalEntry.SourceType`:
  Penerimaan Penjualan, Pembayaran Pembelian, Retur Penjualan, Retur Pembelian, Jurnal Manual, Lainnya.

Tampilan: kartu ringkas (Saldo Awal / Masuk / Keluar / Saldo Akhir), tabel **Ringkasan per Kategori**
(masuk/keluar/neto), dan **Rincian Mutasi** (tanggal, referensi, kategori, keterangan, masuk, keluar,
saldo berjalan).

## Implementasi
- `FinanceReportsController.CashFlow(from, to)` → `CashFlowVm` (default bulan berjalan).
- View `Views/FinanceReports/CashFlow.cshtml`.
- Menu **Keuangan → Arus Kas** (seeder, idempoten, Administrator).

## Hasil / Verifikasi (teruji end-to-end)
Dalam satu periode: penerimaan penjualan 100.000 (kas masuk) & pembayaran pembelian 50.000 (kas keluar):

| Item | Nilai |
|------|-------|
| Kas Masuk (Penerimaan Penjualan) | 100.000 |
| Kas Keluar (Pembayaran Pembelian) | 50.000 |
| **Perubahan Kas Bersih** | **50.000** (cocok dengan saldo akun Kas) ✅ |

Data uji dibersihkan setelah verifikasi.

## Catatan
- Saat ini seluruh kas/bank memakai satu akun GL (1100). Modul **Bank & Kas** (banyak akun bank/kas,
  pembayaran satu-ke-banyak faktur, uang muka, rekonsiliasi) tetap di [roadmap](09-roadmap.md) Tier 1.
