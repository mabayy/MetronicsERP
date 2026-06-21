# Tahap 27 — Tutup Buku (Period Close)

## Tujuan
Mengunci periode akuntansi & menutup tahun fiskal ala SAP B1/Odoo:
- **Jurnal penutup** otomatis: saldo Laba/Rugi (Pendapatan & Beban) dipindahkan ke **Laba Ditahan**.
- **Kunci periode**: tidak ada posting jurnal pada tahun yang ditutup & sebelumnya.
- **Buka kembali** (reopen) tahun terkunci terakhir untuk koreksi.

## Model Data
| Entitas | Peran |
|---------|-------|
| `FiscalYear` | `Year` (unik), `Status` (Open/Closed), `ClosedAt`, `ClosedBy` |
| `JournalEntry` | jurnal penutup ber-`SourceType` = `YearEndClosing` (idempoten per tahun) |

Akun GL baru: **3200 Laba Ditahan** (Equity, di-seed). Menu **Keuangan → Tutup Buku** (Administrator).

## Logika
**Tutup buku tahun Y** (`JournalService.CloseFiscalYearAsync`):
1. Kumpulkan saldo akun Pendapatan & Beban untuk periode 1 Jan–31 Des Y.
2. Posting **jurnal penutup** (tanggal 31 Des Y): **Dr Pendapatan**, **Cr Beban**, selisih (laba/rugi) ke
   **Laba Ditahan** (Cr bila laba, Dr bila rugi). Selalu seimbang.
3. Tandai tahun **Closed** → periode terkunci.

**Kunci periode**: `IsPeriodClosedAsync(date)` → true bila `date ≤ 31 Des tahun terkunci terakhir`.
Diberlakukan di:
- `JournalService.PostAsync` (semua posting otomatis) — kecuali jurnal penutup itu sendiri.
- Jurnal manual, Faktur Pembelian/Penjualan (Create), Pembayaran (Pay), dan Pengiriman (COGS) —
  menolak tanggal pada periode terkunci dengan pesan.

**Buka kembali** (`ReopenFiscalYearAsync`): hanya tahun terkunci **terakhir**; menghapus jurnal penutup
& mengembalikan status Open.

## Migrasi
```bash
dotnet ef migrations add AddFiscalYear --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web --output-dir Persistence/Migrations
dotnet ef database update --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web
```

## Hasil / Verifikasi (teruji end-to-end)
Tahun 2025: Pendapatan 100.000, Beban (HPP) 60.000 (laba 40.000):

| Aksi | Hasil |
|------|-------|
| Tutup buku 2025 | Jurnal penutup **Dr 4100 100.000 / Cr 5200 60.000 / Cr 3200 (Laba Ditahan) 40.000** (seimbang) ✅ |
| Jurnal manual tgl 2025 (setelah tutup) | **ditolak** (periode terkunci) ✅ |
| Jurnal manual tgl 2026 | diizinkan ✅ |
| Buka kembali 2025 | status Open, jurnal penutup dihapus ✅ |

Data uji dibersihkan; akun Laba Ditahan & menu tetap.

## Catatan
Tutup buku menyelesaikan **Tier 1** roadmap (laporan keuangan, termin/batas kredit, bank & kas, tutup buku).
