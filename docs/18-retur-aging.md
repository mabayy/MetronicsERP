# Tahap 18 — Retur & Umur Piutang/Hutang

## Tujuan
Pengembangan lanjutan [Sales](15-sales.md): **Retur Penjualan/Pembelian** (membalik stok + jurnal)
dan **laporan umur (aging) piutang/hutang** per mitra. Posting jurnal mengandalkan modul
[Finance](17-finance.md).

## Model Data

| Entitas | Peran |
|---------|-------|
| `SalesReturn` + `SalesReturnLine` | Retur dari pelanggan (`CustomerId`, `WarehouseId`, baris produk/qty/harga) |
| `PurchaseReturn` + `PurchaseReturnLine` | Retur ke pemasok (`SupplierId`, `WarehouseId`, baris produk/qty/harga) |

## Alur & Business Rules

**Retur Penjualan** (barang kembali dari pelanggan → masuk gudang):
- Memposting **Stok Masuk** per baris via `IStockService` dalam transaksi.
- Jurnal: Dr **Pendapatan** (4100) / Cr **Piutang Usaha** (1200) — membalik penjualan.

**Retur Pembelian** (barang dikembalikan ke pemasok → keluar gudang):
- Memvalidasi **ketersediaan stok** (`GetBalanceAsync`) lalu memposting **Stok Keluar**.
- Jurnal: Dr **Hutang Usaha** (2100) / Cr **Persediaan** (1300) — mengurangi hutang & persediaan.

Penomoran `SRET` / `PRET` dari [Document Numbering](14-document-numbering.md); jurnal ber-`SourceType`
`SalesReturn`/`PurchaseReturn` (idempoten).

## Laporan Umur (Aging)
Mengelompokkan **faktur belum lunas** per mitra ke dalam ember umur berdasarkan tanggal faktur:

| Ember | Rentang |
|-------|---------|
| Current | 0–30 hari |
| 31–60 | 31–60 hari |
| 61–90 | 61–90 hari |
| > 90 | lebih dari 90 hari |

- **Umur Piutang (AR)**: `SalesInvoicesController.Aging` per pelanggan.
- **Umur Hutang (AP)**: `PurchaseInvoicesController.Aging` per pemasok.
- Nilai tiap baris = `Total − PaidAmount` (sisa terutang); ada baris **Total** keseluruhan.

## Controller & Halaman (`[Authorize]`)
| Controller | Aksi |
|-----------|------|
| `SalesReturnsController` | Index, Create (stok masuk + jurnal), Details |
| `PurchaseReturnsController` | Index, Create (validasi stok → stok keluar + jurnal), Details |
| `SalesInvoicesController` | **Aging** (umur piutang) |
| `PurchaseInvoicesController` | **Aging** (umur hutang) |

Menu: **Penjualan → Retur Penjualan / Umur Piutang**; **Pembelian → Retur Pembelian / Umur Hutang**.

## Migrasi
```bash
dotnet ef migrations add AddReturns --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web --output-dir Persistence/Migrations
dotnet ef database update --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web
```

## Hasil / Verifikasi (teruji end-to-end)
Stok awal PRD-0001 = 100:

| Aksi | Stok | Jurnal |
|------|------|--------|
| Retur Penjualan 5 @15.000 (SRET-202606-0001) | **105** | Dr 4100 75.000 / Cr 1200 75.000 |
| Retur Pembelian 3 @10.000 (PRET-202606-0001) | **102** | Dr 2100 30.000 / Cr 1300 30.000 |

Kedua jurnal seimbang; halaman Umur Piutang/Hutang menampilkan ember 0–30/31–60/61–90/>90 + total. ✅
