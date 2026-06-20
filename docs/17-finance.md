# Tahap 17 — Finance (Akuntansi)

## Tujuan
Modul **Keuangan/Akuntansi** sesuai [Roadmap](09-roadmap.md): **Bagan Akun (Chart of Accounts) →
Jurnal (double-entry) → Buku Besar (General Ledger) → Neraca Saldo (Trial Balance)**, dengan
**posting otomatis** dari transaksi Pembelian & Penjualan.

## Model Data

| Entitas | Peran |
|---------|-------|
| `ChartOfAccount` | Akun buku besar: `Code` (unik), `Name`, `Type`, `IsSystem`, `IsDebitNormal` (computed) |
| `AccountType` | Asset, Liability, Equity, Revenue, Expense |
| `JournalEntry` | Header jurnal: `ReferenceNumber`, `EntryDate`, `Description`, `SourceType`, `SourceId` |
| `JournalLine` | Baris jurnal: `AccountId`, `Debit`, `Credit`, `Description` |

Bagan akun standar di-seed (idempoten) lewat `AccountCodes.Defaults`:

| Kode | Akun | Tipe |
|------|------|------|
| 1100 | Kas | Asset |
| 1200 | Piutang Usaha | Asset |
| 1300 | Persediaan | Asset |
| 2100 | Hutang Usaha | Liability |
| 3100 | Modal | Equity |
| 4100 | Pendapatan Penjualan | Revenue |
| 5100 | Beban Pembelian / HPP | Expense |

## Posting Otomatis (`IJournalService`)
`PostAsync(date, description, sourceType, sourceId, lines, user)` — **idempoten** berdasarkan
`(SourceType, SourceId)` sehingga transaksi tidak terjurnal ganda; memvalidasi **debit == kredit > 0**,
mengubah `AccountCode` → akun, dan memberi nomor `JV` dari [Document Numbering](14-document-numbering.md).
Nilai dikonversi ke **mata uang dasar** (`ToBaseAsync`) sebelum diposting.

| Transaksi | Jurnal otomatis |
|-----------|-----------------|
| Faktur Pembelian | Dr **Persediaan** (1300) / Cr **Hutang Usaha** (2100) |
| Pembayaran Pembelian | Dr **Hutang Usaha** (2100) / Cr **Kas** (1100) |
| Faktur Penjualan | Dr **Piutang Usaha** (1200) / Cr **Pendapatan** (4100) |
| Penerimaan Pembayaran | Dr **Kas** (1100) / Cr **Piutang Usaha** (1200) |

Hook dipasang di `PurchaseInvoicesController` & `SalesInvoicesController` (saat Create faktur & Pay).

## Controller & Halaman
| Controller | Aksi | Akses |
|-----------|------|-------|
| `ChartOfAccountsController` | Index, Create, Edit, Delete (blokir bila `IsSystem`/terpakai) | Administrator |
| `JournalEntriesController` | Index, Create (manual, harus balance), Details | Administrator |
| `FinanceReportsController` | **GeneralLedger** (saldo berjalan), **TrialBalance** | Administrator |

- **Buku Besar**: pilih akun + rentang tanggal → saldo awal, mutasi (debit−kredit), saldo akhir berjalan.
- **Neraca Saldo**: saldo bersih tiap akun s.d. tanggal → kolom Debit/Kredit + total (harus seimbang).

Menu **Keuangan → Bagan Akun / Jurnal / Buku Besar / Neraca Saldo** (seeder, idempoten, role Administrator).

## Migrasi
```bash
dotnet ef migrations add AddFinance --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web --output-dir Persistence/Migrations
dotnet ef database update --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web
```

## Hasil / Verifikasi (teruji end-to-end)
PO → terima → Faktur Pembelian 90.000 → Bayar 90.000:

| Dokumen | Jurnal | Debit | Kredit |
|---------|--------|-------|--------|
| Faktur Pembelian | JV-202606-0001 | 1300 = 90.000 | 2100 = 90.000 |
| Pembayaran | JV-202606-0002 | 2100 = 90.000 | 1100 = 90.000 |

Neraca Saldo: **Total Debit 180.000 = Total Kredit 180.000** ✅. Idempotensi mencegah jurnal ganda.

## Pengembangan Lanjutan
- Laba/Rugi & Neraca (Balance Sheet) penuh; tutup buku periode.
- Buku besar pembantu (sub-ledger) piutang/hutang per mitra (lihat [Retur & Umur](18-retur-aging.md)).
