# Dokumentasi Tahapan Pembangunan ERP Metronic

Folder ini berisi dokumentasi langkah-demi-langkah pembangunan aplikasi ERP. Setiap file
mewakili satu tahap yang dapat dikerjakan dan diverifikasi secara berurutan.

## Peta Tahapan

| # | Tahap | Tujuan | Status |
|---|-------|--------|--------|
| 0 | [Prasyarat & Persiapan](00-prasyarat.md) | Menyiapkan tools (.NET 8 SDK, SQL Server, EF Tools, file Metronic) | ✅ |
| 1 | [Setup Proyek & Struktur Solusi](01-setup-proyek.md) | Membuat solusi berlapis Domain/Infrastructure/Web | ✅ |
| 2 | [Integrasi Tema Metronic 8](02-integrasi-metronic.md) | Layout, sidebar, header, aset & cara swap ke Metronic asli | ✅ |
| 3 | [Database, EF Core & Migrasi](03-database-efcore.md) | DbContext, entitas, relasi, migrasi, seeder | ✅ |
| 4 | [Autentikasi & Otorisasi](04-autentikasi-otorisasi.md) | Identity, login/logout, role-based access | ✅ |
| 5 | [Manajemen User & Role](05-manajemen-user-role.md) | CRUD pengguna & peran | ✅ |
| 6 | [Dashboard](06-dashboard.md) | Widget statistik & ringkasan | ✅ |
| 7 | [Modul Master Data](07-master-data.md) | CRUD Produk, Kategori, Satuan, Pelanggan, Pemasok, Gudang | ✅ |
| 8 | [Deployment & Produksi](08-deployment.md) | Build rilis, konfigurasi, IIS/Docker | 📄 Panduan |
| 9 | [Roadmap Lanjutan](09-roadmap.md) | Modul Inventory, Purchasing, Sales, Finance | 📄 Rencana |
| 10 | [Master Menu (Menu Dinamis)](10-master-menu.md) | Sidebar berbasis DB: tambah/edit/hapus/urutkan (drag-drop) | ✅ |
| 11 | [Manajemen Stok (Inventory)](11-manajemen-stok.md) | Stok masuk/keluar/transfer/penyesuaian + saldo per gudang | ✅ |
| 12 | [Currency Management (Multi-Currency)](12-currency-management.md) | Mata uang dasar, kurs ber-tanggal, konversi + integrasi produk | ✅ |
| 13 | [Purchasing (Pembelian)](13-purchasing.md) | Purchase Order → penerimaan → stok masuk otomatis | ✅ |
| 14 | [Document Numbering](14-document-numbering.md) | Penomoran dokumen dapat dikustomisasi (prefix/format/reset) | ✅ |
| 15 | [Sales (Penjualan)](15-sales.md) | SO → pengiriman (stok keluar) → faktur (3-way) → pembayaran (piutang) | ✅ |

## Cara membaca dokumen ini

- Setiap tahap memiliki bagian **Tujuan**, **Langkah**, **Hasil/Verifikasi**, dan **Catatan**.
- Blok kode menampilkan perintah CLI atau potongan source yang benar-benar dipakai di repo ini.
- Tanda ✅ berarti tahap sudah diimplementasikan dan terverifikasi berjalan.

## Konvensi Proyek

- **Bahasa UI**: Indonesia. **Penamaan kode**: Inggris (konvensi .NET).
- **Namespace root**: `ErpMetronic`.
- **Database**: `ErpMetronicDb` di SQL Server.
- **Akun admin awal**: `admin@erpmetronic.local` / `Admin#12345` (lihat `DbSeeder`).
