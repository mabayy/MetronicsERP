# Tahap 28 — Daftar Harga (Price List)

## Tujuan
Memulai **Tier 2** (fitur komersial) ala SAP B1/Odoo: **Daftar Harga** yang dapat dipakai ulang &
ditetapkan sebagai harga default pelanggan, sehingga harga jual **terisi otomatis** saat membuat
Sales Order.

## Model Data
| Entitas | Peran |
|---------|-------|
| `PriceList` | `Code` (unik), `Name`, `CurrencyId` (opsional), `IsActive`, `IsSystem`, `Items` |
| `PriceListItem` | `PriceListId`, `ProductId`, `Price` (unik per list+produk) |
| `Customer` | `PriceListId` — daftar harga default pelanggan |

Menu **Master Data → Daftar Harga** (Administrator).

## Perilaku
- **CRUD daftar harga** + halaman **Kelola Harga** (`Manage`): semua produk dengan input harga; harga
  kosong/0 = pakai harga jual default produk (override dihapus).
- **Pelanggan** memilih daftar harga default.
- **Sales Order (Create)**: saat memilih produk pada baris (atau mengganti pelanggan), **harga satuan
  terisi otomatis** dari daftar harga pelanggan; bila produk tak ada di daftar → fallback ke harga jual
  produk. Harga tetap dapat diubah manual. (Implementasi via data `ViewBag` → JavaScript.)

## Migrasi
```bash
dotnet ef migrations add AddPriceList --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web --output-dir Persistence/Migrations
dotnet ef database update --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web
```

## Hasil / Verifikasi (teruji end-to-end)
Daftar harga **GROSIR** (Produk PRD-0001 = 13.000), ditetapkan ke pelanggan:

| Cek | Hasil |
|-----|-------|
| Simpan harga grosir produk 1 | PriceListItem = 13.000 ✅ |
| SO Create memuat peta harga | `custList {pelanggan→list}`, `listPrices {list→{produk→13.000}}`, `default {produk→15.000}` ✅ |

Pemilihan produk pada SO untuk pelanggan tsb. mengisi harga **13.000** (grosir), bukan 15.000 (default).
Data uji dibersihkan.

## Pengembangan lanjut
- Harga bertingkat per kuantitas (tiered) & aturan diskon berbasis formula (Odoo pricelist rules).
- Daftar harga **pembelian** per pemasok.
