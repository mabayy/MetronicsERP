# Tahap 15 — Sales (Penjualan)

## Tujuan
Modul **Penjualan** sesuai [Roadmap](09-roadmap.md): **Sales Order (SO) → Pengiriman (kurangi
stok) → Faktur Penjualan → Pembayaran (piutang)**. Mencerminkan modul [Pembelian](13-purchasing.md).

## Model Data

| Entitas | Peran |
|---------|-------|
| `SalesOrder` + `SalesOrderItem` | Header SO + baris (qty, harga jual, `DeliveredQuantity`) |
| `SalesOrderStatus` | Draft, Confirmed, PartiallyDelivered, Delivered, Cancelled |
| `DeliveryOrder` (+`SalesOrderId`) | Dokumen pengiriman tertaut SO (auto **Stok Keluar**) |
| `SalesInvoice` + `SalesInvoiceLine` | Faktur (piutang) dibuat **dari SO** |
| `SalesPayment` | Penerimaan pembayaran pelanggan |
| `SalesInvoiceStatus` | Unpaid, PartiallyPaid, Paid |

## Alur & Business Rules

```
Draft ──Konfirmasi──▶ Confirmed ──Kirim──▶ PartiallyDelivered ──Kirim──▶ Delivered
                                                │
                          (Faktur dari SO: dikirim − sudah difaktur)
                                                ▼
                              SalesInvoice ──Terima Pembayaran──▶ Lunas
```

- **SO** dibuat Draft (boleh **diedit** saat Draft), dikonfirmasi → Confirmed.
- **Pengiriman** (dari SO Confirmed/PartiallyDelivered): membuat `DeliveryOrder` tertaut SO,
  memposting **Stok Keluar** via `IStockService` dalam transaksi, memvalidasi **saldo stok**
  dan **sisa SO**; memperbarui `DeliveredQuantity` & status SO.
- **Faktur Penjualan**: 3-way matching — jumlah difaktur ≤ **dikirim − sudah difaktur**.
- **Pembayaran (piutang)**: ≤ sisa tagihan; memperbarui status (Lunas bila penuh).
- **Batal**: hanya bila belum ada pengiriman.
- Penomoran `SO`/`SINV`/`SPAY` (pengiriman pakai `DO`) dari [Document Numbering](14-document-numbering.md).

## Controller & Halaman (`[Authorize]`)
| Controller | Aksi |
|-----------|------|
| `SalesOrdersController` | Index, Create, **Edit (Draft)**, Confirm, **Deliver** (GET/POST), Cancel, Details (+ riwayat pengiriman) |
| `SalesInvoicesController` | Index, SelectSo, Create (3-way), Details, **Pay** (piutang) |

Menu **Penjualan → Sales Order / Faktur Penjualan** (seeder, idempoten).

## Migrasi
```bash
dotnet ef migrations add AddSales --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web --output-dir Persistence/Migrations
dotnet ef database update --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web
```

## Hasil / Verifikasi (teruji end-to-end)
SO 15 unit PRD-0001 (stok awal 100):

| Langkah | Status SO | Stok | Catatan |
|---------|-----------|------|---------|
| Buat SO → Konfirmasi | Confirmed | 100 | — |
| Kirim 10 | PartiallyDelivered | **90** | DeliveryOrder + Stok Keluar |
| Faktur 6 (dari dikirim 10) | — | — | total 90.000, Belum Dibayar |
| Faktur 8 (sisa difaktur 4) | — | — | **ditolak** ("melebihi yang dapat difaktur") |
| Terima pembayaran 90.000 | — | — | Faktur **Lunas** |
| Kirim 99 (sisa SO 5) | — | tetap 90 | **ditolak** ("melebihi sisa") |

## Pengembangan Lanjutan
- Retur penjualan/pembelian; laporan piutang/hutang per pelanggan/pemasok (umur).
- Akuntansi: posting otomatis penjualan/pembelian ke jurnal (lihat Roadmap modul Finance).
