# Tahap 29 — Penawaran Penjualan (Sales Quotation)

## Tujuan
Melengkapi sisi penjualan dengan **Penawaran (Quotation)** sebelum Sales Order — sejajar dengan
RFQ di sisi pembelian, mengikuti alur Odoo (Quotation → Sales Order).

## Model Data
| Entitas | Peran |
|---------|-------|
| `SalesQuotation` | `ReferenceNumber` (SQ), `QuotationDate`, `ValidUntil`, Customer/Warehouse/Currency, `Status`, diskon header + PPh (mirror SO), `ConvertedSalesOrderId` |
| `SalesQuotationItem` | produk, qty, harga, `DiscountPercent`, snapshot PPN (`TaxId/TaxRate/TaxAmount`) |
| `SalesQuotationStatus` | Draft, Sent, Accepted, Rejected |

Penomoran `SQ` (di-seed). Menu **Penjualan → Penawaran**.

## Alur & Perilaku
```
Draft ──Kirim──▶ Terkirim ──Terima──▶ Diterima ──Konversi──▶ Sales Order
                      └────Tolak────▶ Ditolak
```
- **Create/Edit** (saat Draft): editor baris dengan **diskon per baris + PPN per baris + diskon header
  + PPh**, ringkasan total langsung, dan **auto-isi harga** dari [Daftar Harga](28-price-list.md) pelanggan
  (sama seperti Sales Order).
- **Konversi ke Sales Order** (status Diterima): membuat SO Draft dengan menyalin header (diskon/PPh) &
  baris (qty, harga, diskon, snapshot pajak) apa adanya; penawaran ditandai tertaut SO (`ConvertedSalesOrderId`)
  dan tidak dapat dikonversi ulang.

## Migrasi
```bash
dotnet ef migrations add AddSalesQuotation --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web --output-dir Persistence/Migrations
dotnet ef database update --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web
```

## Hasil / Verifikasi (teruji end-to-end)
Penawaran SQ-202606-0001: PRD-0001 qty 5 @20.000, diskon baris 10%, PPN 11%.

| Aksi | Hasil |
|------|-------|
| Buat penawaran | Neto baris 90.000, PPN 9.900 ✅ |
| Kirim → Terima → Konversi | SO-202606-0001 dibuat; status penawaran **Diterima** + tertaut SO ✅ |
| Baris SO hasil konversi | qty 5, harga 20.000, diskon 10%, PPN 9.900 (tersalin) ✅ |

Data uji dibersihkan.

## Pengembangan lanjut
- Cetak/email penawaran (PDF) & penawaran kedaluwarsa otomatis saat lewat `ValidUntil`.
