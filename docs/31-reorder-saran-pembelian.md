# Tahap 31 — Titik Reorder & Saran Pembelian

## Tujuan
Bagian dari Tier 2 #8: **titik pemesanan ulang (reorder point)** + **saran pembelian** otomatis —
produk yang stoknya turun ke/di bawah titik reorder dikumpulkan dan dapat langsung dijadikan PO draft.

## Model Data
| Field (Product) | Peran |
|-----------------|-------|
| `ReorderLevel` | Titik reorder (stok minimum). Bila stok ≤ nilai ini → muncul di saran. |
| `ReorderQuantity` | Jumlah pemesanan ulang yang disarankan (lot size). 0 = isi sampai titik reorder. |

## Perilaku
- **Form Produk** menampilkan **Titik Reorder** & **Jumlah Pemesanan**.
- **Saran Pembelian** (`PurchaseSuggestions`, menu **Pembelian → Saran Pembelian**): daftar produk dengan
  `ReorderLevel > 0` dan `StockQuantity ≤ ReorderLevel`. Jumlah saran =
  `ReorderQuantity > 0 ? ReorderQuantity : max(ReorderLevel − stok, 1)`.
- Pilih beberapa produk (centang + sesuaikan jumlah), pilih **pemasok** & **gudang**, lalu
  **Buat PO Draft** — sistem membuat satu Purchase Order Draft (harga = harga beli produk) yang dapat
  dilanjutkan ke alur pembelian biasa (konfirmasi, persetujuan, penerimaan, dst.).

## Migrasi
```bash
dotnet ef migrations add AddReorderQuantity --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web --output-dir Persistence/Migrations
dotnet ef database update --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web
```

## Hasil / Verifikasi (teruji end-to-end)
PRD-0001: stok 5, titik reorder 10, jumlah pemesanan 50.

| Aksi | Hasil |
|------|-------|
| Buka Saran Pembelian | PRD-0001 tampil, jumlah saran **50** ✅ |
| Buat PO Draft (pilih PRD-0001) | **PO-202606-0001** Draft, item qty 50 @ harga beli ✅ |

Data uji dibersihkan; produk dikembalikan ke baseline.

## Catatan — Batch/Lot & Serial (bagian lain dari #8)
Pelacakan **Batch/Lot & Serial number** (dengan kedaluwarsa & alokasi saat pengeluaran/HPP) **belum**
diimplementasikan karena menyentuh inti alur stok & HPP (perlu alokasi FEFO/serial per pengeluaran).
Direkomendasikan sebagai modul tersendiri pada [roadmap](09-roadmap.md) Tier 2.
