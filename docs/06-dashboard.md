# Tahap 6 — Dashboard

## Tujuan
Menyajikan ringkasan cepat kondisi bisnis pada halaman utama setelah login.

## Komponen
- `Controllers/DashboardController.cs` — mengumpulkan agregat dari `ApplicationDbContext`.
- `Views/Dashboard/Index.cshtml` — kartu statistik + tabel ringkasan.

## Data yang Ditampilkan

```csharp
ViewBag.ProductCount  = await _db.Products.CountAsync();
ViewBag.CategoryCount = await _db.Categories.CountAsync();
ViewBag.CustomerCount = await _db.Customers.CountAsync();
ViewBag.SupplierCount = await _db.Suppliers.CountAsync();

// 5 produk dengan stok <= titik pemesanan ulang
ViewBag.LowStock = await _db.Products
    .Where(p => p.StockQuantity <= p.ReorderLevel)
    .OrderBy(p => p.StockQuantity).Take(5).ToListAsync();

// 5 produk terbaru
ViewBag.RecentProducts = await _db.Products
    .Include(p => p.Category)
    .OrderByDescending(p => p.CreatedAt).Take(5).ToListAsync();
```

## Tampilan
- **4 kartu statistik** dengan gradien warna (Produk, Kategori, Pelanggan, Pemasok) memakai
  kelas `stat-card bg-grad-*`.
- **Tabel "Produk Terbaru"** — SKU, nama, kategori, stok.
- **Tabel "Stok Menipis"** — menyorot produk yang perlu di-restock (badge merah).

## Pengembangan Lanjutan (opsional)
- Tambahkan grafik (mis. ApexCharts yang dibawa Metronic) untuk tren penjualan.
- Filter periode (harian/bulanan) ketika modul transaksi tersedia.
- Widget per-role (Manager melihat ringkasan berbeda dari Staff).

## Hasil / Verifikasi
`/Dashboard` mengembalikan 200 dengan judul "Dashboard - ERP Metronic" dan menampilkan angka
agregat sesuai isi database.

## Selanjutnya
➡️ [Tahap 7 — Modul Master Data](07-master-data.md)
