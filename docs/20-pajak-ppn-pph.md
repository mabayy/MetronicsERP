# Tahap 20 — Sistem Pajak (PPN & PPh)

## Tujuan
Sistem pajak Indonesia bergaya **SAP Business One / Odoo**: master **kode pajak** yang dipakai
ulang, **PPN (VAT)** per baris item, dan **PPh (withholding)** dipotong per dokumen — mengalir dari
**PO/SO → Faktur** dengan **posting jurnal otomatis** ke akun pajak. Mengandalkan modul
[Finance](17-finance.md).

## Konsep (best practice)
- **PPN (VAT)** — `TaxKind.ValueAdded`. Dipilih **per baris** (mirip Odoo/SAP B1), **menambah** nilai
  dokumen. Pembelian → **PPN Masukan** (aset), Penjualan → **PPN Keluaran** (hutang pajak).
- **PPh (withholding)** — `TaxKind.Withholding`. Dipilih **per dokumen** atas DPP, **mengurangi** nilai
  dibayar. Pembelian → kita memotong → **Hutang PPh**. Penjualan → pelanggan memotong → **PPh Dibayar
  Dimuka** (kredit pajak).
- **Snapshot**: tarif & nilai pajak disimpan di dokumen saat dibuat, sehingga perubahan master pajak
  tidak mengubah dokumen historis.

## Model Data
| Entitas | Peran |
|---------|-------|
| `Tax` | Master: `Code`, `Name`, `Rate` (%), `Kind` (PPN/PPh), `AppliesTo` (Sales/Purchase/Both), `AccountCode`, `IsActive`, `IsSystem` |
| Baris (PO/SO/Faktur) | `TaxId` + snapshot `TaxRate`, `TaxAmount`; computed `LineSubtotal` |
| Header (PO/SO/Faktur) | `WithholdingTaxId` + snapshot `WithholdingRate`, `WithholdingAmount`; computed `Subtotal`, `TaxTotal`, `Total`/`GrandTotal` |

Perhitungan: **DPP** = Σ(qty×harga); **PPN** = Σ round(DPP_baris × tarif); **PPh** = round(DPP × tarif);
**Total** = DPP + PPN − PPh.

## Akun & Master Bawaan (seeder, idempoten)
| Kode Akun | Nama | Jenis |
|-----------|------|-------|
| 1310 | PPN Masukan | Asset |
| 1320 | PPh Dibayar Dimuka | Asset |
| 2110 | PPN Keluaran | Liability |
| 2130 | Hutang PPh | Liability |

| Kode Pajak | Nama | Tarif | Berlaku | Akun |
|-----------|------|------|---------|------|
| PPN-OUT | PPN Keluaran 11% | 11% | Penjualan | 2110 |
| PPN-IN | PPN Masukan 11% | 11% | Pembelian | 1310 |
| PPH23 | PPh Pasal 23 (2%) | 2% | Keduanya | 2130 |

Master pajak dapat ditambah/diubah lewat menu **Keuangan → Pajak** (`TaxesController`, Administrator).
Pajak sistem tidak dapat dihapus & kode/jenisnya terkunci.

## Posting Jurnal Otomatis (`JournalService`)
**Faktur Pembelian** (DPP 100.000, PPN 11.000, PPh 2.000):
| Akun | Debit | Kredit |
|------|-------|--------|
| Persediaan (1300) | 100.000 | |
| PPN Masukan (1310) | 11.000 | |
| Hutang PPh (2130) | | 2.000 |
| Hutang Usaha (2100) | | 109.000 |

**Faktur Penjualan** (DPP 100.000, PPN 11.000, PPh 2.000):
| Akun | Debit | Kredit |
|------|-------|--------|
| Piutang Usaha (1200) | 109.000 | |
| PPh Dibayar Dimuka (1320) | 2.000 | |
| Pendapatan (4100) | | 100.000 |
| PPN Keluaran (2110) | | 11.000 |

## Antarmuka
- **PO/SO & Faktur (Create/Edit)**: kolom **PPN** per baris (dengan tarif), pemilih **PPh** per dokumen,
  dan ringkasan **DPP / PPN / PPh / Total** yang dihitung langsung di layar (JavaScript).
- **Details**: rincian PPN per baris + blok total DPP/PPN/PPh/Total.
- Pajak **mengalir** PO→Faktur (Pembelian) & SO→Faktur (Penjualan) saat disalin.

## Migrasi
```bash
dotnet ef migrations add AddTaxes --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web --output-dir Persistence/Migrations
dotnet ef database update --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web
```

## Hasil / Verifikasi (teruji end-to-end)
| Alur | DPP | PPN | PPh | Jurnal balance |
|------|-----|-----|-----|----------------|
| PO→terima→Faktur Pembelian (10×10.000, PPN-IN, PPh23) | 100.000 | 11.000 | 2.000 | **111.000 = 111.000** ✅ |
| SO→kirim→Faktur Penjualan (5×20.000, PPN-OUT, PPh23) | 100.000 | 11.000 | 2.000 | **111.000 = 111.000** ✅ |

Data uji dibersihkan; master pajak, akun, & menu tetap.
