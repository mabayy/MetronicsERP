# Tahap 32 — Batch/Lot & Serial Number

## Tujuan
Pelacakan persediaan ala SAP B1/Odoo: per **batch/lot** (dengan **kedaluwarsa**) dan per **nomor seri**,
terintegrasi ke seluruh pergerakan stok melalui `StockService`, tanpa mengubah metode HPP (moving average).

## Arsitektur
Sub-ledger pelacakan berada **di bawah** `ProductStock` (saldo per gudang) & `Product.AverageCost`
(biaya). `StockService` menjadi satu-satunya pintu sehingga sub-ledger **selalu konsisten**:
- **Masuk** (`StockInAsync`): produk berlacak **Lot** → catat/akumulasi `StockLot` (nomor lot + kedaluwarsa;
  auto-nomor bila kosong). **Serial** → buat record `SerialNumber` (in-stock); nomor dari input atau
  auto-generate bila jumlah tak sesuai.
- **Keluar** (`StockOutAsync`): **Lot** → konsumsi **FEFO** (kedaluwarsa terdekat dulu); **Serial** →
  tandai sejumlah unit in-stock menjadi keluar. Bersifat *best-effort* (tak memblokir stok-keluar valid).

Karena semua alur (Penerimaan, PO Receive, Pengiriman SO, retur, stok manual) melewati `StockService`,
lot/serial otomatis terpelihara.

## Model Data
| Entitas | Peran |
|---------|-------|
| `Product.TrackingType` | None / Lot / Serial |
| `StockLot` | ProductId, WarehouseId, LotNumber, ExpiryDate, Quantity (unik per produk+gudang+lot) |
| `SerialNumber` | ProductId, SerialNo, WarehouseId, IsInStock (unik per produk+serial) |

## Antarmuka
- **Form Produk**: pilih **Pelacakan** (None/Lot/Serial).
- **Penerimaan Barang**: per baris ada input **No. Lot**, **Kadaluarsa**, dan **No. Seri** (pisahkan koma);
  dipakai sesuai jenis pelacakan produk.
- **Laporan** (menu **Manajemen Stok**): **Stok per Lot** (qty + kedaluwarsa, badge "≤30 hari"/"Kedaluwarsa")
  dan **Nomor Seri** (filter Di Stok/Keluar).

## Migrasi
```bash
dotnet ef migrations add AddLotSerial --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web --output-dir Persistence/Migrations
dotnet ef database update --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web
```

## Hasil / Verifikasi (teruji end-to-end)
Produk A (Lot), Produk B (Serial):

| Aksi | Hasil |
|------|-------|
| Terima A 20 unit, Lot "LOT-A", exp 31/12/2026 | StockLot LOT-A qty 20 + kedaluwarsa ✅ |
| Terima B 3 unit, serial SN-001..003 | 3 SerialNumber in-stock ✅ |
| SO kirim A 5 unit | **FEFO**: LOT-A 20 → **15** ✅ |
| SO kirim B 1 unit | 1 serial **Keluar**, 2 tetap Di Stok ✅ |
| Laporan Stok per Lot & Nomor Seri | render & akurat ✅ |

Data uji dibersihkan; produk dikembalikan ke baseline (TrackingType None).

## Catatan / batasan
- Saldo awal yang belum ber-lot/serial (mis. stok seeding) tidak otomatis memiliki lot/serial; sub-ledger
  bersifat best-effort untuk unit yang memang dilacak sejak penerimaan.
- Biaya tetap **moving average company-wide** (lot tidak dipakai untuk costing terpisah).
- Penyempurnaan lanjut: pemilihan lot/serial **eksplisit** saat pengeluaran (kini FEFO/auto), serta
  kolom lot/serial pada faktur & retur.
