# Tahap 22 — HPP Otomatis & Moving Average

## Tujuan
Menutup gap akuntansi persediaan ala **SAP B1 / Odoo** dengan **persediaan perpetual**:
- **Moving Average Cost (MAC)** — biaya rata-rata bergerak per produk, diperbarui tiap penerimaan.
- **HPP/COGS otomatis** — saat pengiriman barang: **Dr Harga Pokok Penjualan / Cr Persediaan**
  senilai biaya rata-rata yang keluar.

## Konsep
- **Saat barang masuk** (penerimaan PO / Goods Receipt) dengan biaya `C` & jumlah `Q`:
  `avg_baru = (qty_lama × avg_lama + Q × C) / (qty_lama + Q)` (4 desimal).
- **Saat barang keluar** (pengiriman) `Q` unit: `HPP = Q × avg`; biaya rata-rata **tidak berubah**.
- Transfer & penyesuaian dinilai pada biaya rata-rata saat itu (dicatat di kartu stok).
- HPP diakui saat **pengiriman** (perpetual / anglo-saxon), terpisah dari pengakuan pendapatan di faktur.

## Model Data
| Lokasi | Field |
|--------|-------|
| `Product` | `AverageCost` (decimal 18,4) — biaya rata-rata bergerak (mata uang dasar) |
| `StockMovement` | `UnitCost` (decimal 18,4) — biaya per unit pergerakan (masuk = beli; keluar = HPP) |

Akun GL baru: **5200 Harga Pokok Penjualan** (di-seed via `AccountCodes.Defaults`).

## Implementasi
- `IStockService.StockInAsync(..., decimal? unitCost = null)` — bila `unitCost` diisi, memperbarui MAC;
  `StockOutAsync` mengembalikan biaya rata-rata pada `StockResult.UnitCost` (untuk HPP).
- `IJournalService.PostDeliveryCogsAsync(deliveryId, date, ref, cogsAmount, user)` — posting
  Dr 5200 / Cr 1300, **idempoten** per `DeliveryOrder`.
- Hook:
  - **Penerimaan** (PO Receive & Goods Receipt) meneruskan biaya beli (`UnitPrice`/`UnitCost`) → MAC diperbarui.
  - **Pengiriman** (SO Deliver) menjumlahkan `qty × biaya rata-rata` lalu memposting HPP dalam transaksi yang sama.
- Produk seed diberi `AverageCost` awal = harga beli; data lama di-backfill `AverageCost = PurchasePrice`.

## Migrasi
```bash
dotnet ef migrations add AddInventoryCosting --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web --output-dir Persistence/Migrations
dotnet ef database update --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web
```

## Hasil / Verifikasi (teruji end-to-end)
PRD-0001 saldo awal 100 @ biaya rata-rata 10.000:

| Langkah | Biaya Rata-rata | Qty | Jurnal |
|---------|-----------------|-----|--------|
| Terima 100 @ 12.000 | **11.000** = (100×10.000 + 100×12.000)/200 | 200 | — |
| Kirim 50 | 11.000 (tetap) | 150 | **Dr HPP 550.000 / Cr Persediaan 550.000** ✅ |

Data uji dibersihkan; biaya rata-rata & saldo dikembalikan ke baseline.

## Catatan / pengembangan lanjut
- Biaya rata-rata bersifat **per produk (company-wide)**, seperti default Odoo.
- Penerimaan multi-currency saat ini diasumsikan dalam mata uang dasar (konversi biaya ke base
  adalah penyempurnaan berikutnya).
- Retur jual/beli belum menilai ulang persediaan pada biaya rata-rata (akan disesuaikan saat
  modul penilaian persediaan diperdalam).
