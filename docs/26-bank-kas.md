# Tahap 26 — Modul Bank & Kas

## Tujuan
Modul **Bank & Kas** ala SAP B1/Odoo:
- **Master Akun Kas/Bank** yang dapat banyak, tiap akun terhubung ke satu akun GL.
- **Routing pembayaran**: penerimaan/pembayaran memilih akun kas/bank → jurnal diposting ke GL akun itu.
- **Rekonsiliasi Bank** di level mutasi GL (cocokkan dengan rekening koran).
- **Arus Kas** kini mencakup seluruh akun kas/bank.

## Model Data
| Entitas | Tambahan |
|---------|----------|
| `CashBankAccount` (master) | `Code`, `Name`, `Kind` (Cash/Bank), `AccountCode` (GL), `BankName`, `AccountNumber`, `IsActive`, `IsSystem` |
| `SalesPayment` / `PurchasePayment` | `CashBankAccountId` (akun sumber/tujuan dana) |
| `JournalLine` | `IsReconciled`, `ReconciledDate` (penanda rekonsiliasi) |

Akun GL baru: **1110 Bank** (di-seed). Akun kas/bank bawaan: **KAS → 1100**, **BANK → 1110**.
Menu **Keuangan → Bank & Kas** (master) dan **Rekonsiliasi Bank**.

## Perilaku
- **Pembayaran**: form Bayar/Terima memilih **Akun Kas/Bank**. Jurnal:
  - Penerimaan penjualan: Dr **akun kas/bank terpilih** / Cr Piutang.
  - Pembayaran pembelian: Dr Hutang / Cr **akun kas/bank terpilih**.
  Bila tidak dipilih, default ke Kas (1100).
- **Rekonsiliasi**: pilih akun + periode → daftar mutasi GL akun tersebut (masuk/keluar) dengan kotak
  centang **terekonsiliasi**. Menampilkan **Saldo Buku** & **Saldo Terekonsiliasi**; input **Saldo
  Rekening Koran** menghitung **Selisih** (klien). Simpan menandai baris jurnal `IsReconciled`.
- **Arus Kas** menjumlahkan mutasi seluruh akun GL yang terdaftar sebagai kas/bank (Kas + Bank).

## Migrasi
```bash
dotnet ef migrations add AddCashBank --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web --output-dir Persistence/Migrations
dotnet ef database update --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web
```

## Hasil / Verifikasi (teruji end-to-end)
| Uji | Hasil |
|-----|-------|
| Penerimaan 100.000 via **BANK** | Jurnal **Dr 1110 Bank / Cr 1200 Piutang** ✅ |
| Rekonsiliasi tandai baris bank | `JournalLine.IsReconciled = true` + tanggal ✅ |
| Arus Kas | menampilkan pemasukan 100.000 dari akun bank ✅ |

Data uji dibersihkan; akun kas/bank & menu tetap.

## Pengembangan lanjut (belum termasuk)
- **Satu pembayaran untuk banyak faktur** & **uang muka (down payment)** — perlu dokumen pembayaran
  ber-alokasi terpisah; tetap di [roadmap](09-roadmap.md) Tier 1.
- Konversi biaya/pembayaran multi-currency ke mata uang dasar untuk akun bank valas.
