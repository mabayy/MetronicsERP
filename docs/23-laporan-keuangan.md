# Tahap 23 — Laporan Keuangan (Laba Rugi & Neraca)

## Tujuan
Melengkapi modul [Finance](17-finance.md) dengan dua laporan inti ala SAP B1/Odoo, dihitung langsung
dari jurnal (`JournalLine`) berdasarkan jenis akun (`AccountType`):
- **Laba Rugi (Income Statement)** — Pendapatan − Beban untuk satu periode.
- **Neraca (Balance Sheet)** — posisi Aset = Liabilitas + Ekuitas pada satu tanggal.

## Logika
Saldo akun: akun **debit-normal** (Aset, Beban) = Σdebit − Σkredit; akun **kredit-normal**
(Liabilitas, Ekuitas, Pendapatan) = Σkredit − Σdebit.

**Laba Rugi** (periode `from`–`to`, default awal tahun s.d. hari ini):
- Pendapatan = Σ akun Revenue, Beban = Σ akun Expense (termasuk **HPP**).
- **Laba Bersih** = Pendapatan − Beban.

**Neraca** (s.d. tanggal):
- Aset, Liabilitas, Ekuitas (terposting) dari saldo akun masing-masing.
- **Laba berjalan** = Pendapatan − Beban s.d. tanggal, ditambahkan ke Ekuitas.
- **Total Ekuitas** = Ekuitas terposting + Laba berjalan.
- Selalu seimbang: **Aset = Liabilitas + Total Ekuitas** (karena tiap jurnal balance). UI menampilkan
  badge **Seimbang / Tidak seimbang**.

> Catatan: saldo awal yang belum dijurnal (mis. stok awal hasil seeding, setoran modal) tidak muncul
> di laporan sampai dibukukan sebagai jurnal — laporan mencerminkan transaksi yang terposting.

## Implementasi
- `FinanceReportsController.IncomeStatement(from, to)` → `IncomeStatementVm`.
- `FinanceReportsController.BalanceSheet(asOf)` → `BalanceSheetVm`.
- View `Views/FinanceReports/IncomeStatement.cshtml` & `BalanceSheet.cshtml`.
- Menu **Keuangan → Laba Rugi / Neraca** (seeder, idempoten, role Administrator).

## Hasil / Verifikasi (teruji end-to-end)
Siklus penjualan PRD-0001 (biaya rata-rata 10.000): kirim 10 (HPP 100.000) → faktur 10 × 20.000 + PPN 11%:

| Laporan | Hasil |
|---------|-------|
| Laba Rugi | Pendapatan **200.000** − HPP **100.000** = **Laba 100.000** ✅ |
| Neraca | Aset (Piutang 222.000 − Persediaan 100.000) = Liabilitas (PPN 22.000) + Ekuitas (laba 100.000) → **Seimbang** ✅ |

Data uji dibersihkan setelah verifikasi.
