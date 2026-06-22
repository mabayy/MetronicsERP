# ERP Metronic — Web ERP dengan .NET 8, SQL Server & UI Kit Metronic 8

Aplikasi web ERP modular berbasis **ASP.NET Core 8 MVC (Razor)**, **Entity Framework Core**,
**SQL Server**, dan tema **UI Metronic 8 (Bootstrap 5)**. Proyek dibangun secara bertahap dengan
dokumentasi lengkap pada folder [`docs/`](docs/).

## ✨ Fitur yang sudah tersedia

| Modul | Status | Keterangan |
|-------|--------|-----------|
| Autentikasi (Login/Logout) | ✅ | ASP.NET Core Identity + cookie auth |
| Manajemen Pengguna | ✅ | CRUD user + divisi & posisi (hak akses dari posisi) |
| Divisi & Posisi (akses) | ✅ | Master divisi/jabatan; posisi menentukan hak admin & akses menu |
| Master Menu (menu dinamis) | ✅ | Sidebar dari DB: tambah/edit/hapus/urutkan via drag-and-drop |
| Tema warna (3 skema) | ✅ | Sapphire / Mocha / Emerald — ganti via header, tersimpan di browser |
| Sidebar expand/collapse | ✅ | Ciutkan jadi rail ikon (flyout saat hover), status tersimpan |
| Manajemen Stok (Inventory) | ✅ | Stok masuk/keluar/transfer/penyesuaian, saldo per gudang, **kartu stok**, **nilai persediaan** |
| Penerimaan & Pengeluaran | ✅ | Dokumen penerimaan (auto stok masuk) & pengiriman (auto stok keluar) terintegrasi stok |
| Pengadaan awal (PR & RFQ) | ✅ | Purchase Requisition (approval) → Request for Quotation (penawaran & pemenang) |
| Copy To / Copy From | ✅ | Salin dokumen antar tahap pembelian: PR → RFQ → PO → Faktur (isi otomatis) |
| Pembelian (Purchasing) | ✅ | PO (edit saat Draft) → konfirmasi → penerimaan (auto stok masuk), multi-currency |
| Faktur & Pembayaran Pembelian | ✅ | Faktur dari PO dengan 3-way matching + pembayaran hutang (status Lunas) |
| Penjualan (Sales) | ✅ | SO → pengiriman (stok keluar) → faktur (3-way) → pembayaran (piutang) |
| Retur Jual & Beli | ✅ | Retur penjualan (stok masuk) / pembelian (stok keluar) + jurnal otomatis |
| Umur Piutang & Hutang | ✅ | Laporan aging AR/AP per mitra (ember 0–30/31–60/61–90/>90) |
| Finance (Akuntansi) | ✅ | Bagan akun → jurnal double-entry → buku besar → neraca saldo, posting otomatis pembelian/penjualan |
| Pajak (PPN & PPh) | ✅ | Master pajak, PPN per baris + PPh withholding (gaya SAP B1/Odoo), posting jurnal otomatis |
| Diskon (Baris & Header) | ✅ | Diskon per baris item + diskon header dialokasikan sebelum pajak (gaya SAP B1/Odoo) |
| HPP & Moving Average | ✅ | Persediaan perpetual: biaya rata-rata bergerak + posting HPP otomatis saat pengiriman |
| Laporan Keuangan | ✅ | Laba Rugi (Income Statement) & Neraca (Balance Sheet) dari jurnal |
| Termin & Batas Kredit | ✅ | Termin pembayaran, jatuh tempo otomatis, batas kredit pelanggan, aging by due date |
| Laporan Arus Kas | ✅ | Cash flow: mutasi kas/bank per kategori + saldo awal/akhir |
| Bank & Kas | ✅ | Master akun kas/bank, routing pembayaran ke GL, rekonsiliasi bank |
| Tutup Buku | ✅ | Jurnal penutup laba/rugi → Laba Ditahan + kunci periode (period lock) |
| Daftar Harga (Price List) | ✅ | Daftar harga per pelanggan, harga jual auto-isi di Sales Order |
| Penawaran Penjualan | ✅ | Sales Quotation → konversi ke Sales Order (gaya Odoo) |
| Workflow Persetujuan | ✅ | Approval berjenjang per jabatan (ambang nilai) untuk Purchase Order |
| Reorder & Saran Pembelian | ✅ | Titik reorder + saran pembelian otomatis → buat PO draft |
| Document Numbering | ✅ | Penomoran dokumen dapat dikustomisasi: prefix, format token, padding, reset bulanan/tahunan |
| Currency Management | ✅ | Multi-currency: mata uang dasar, kurs ber-tanggal, konversi + harga produk multi-currency |
| Dashboard | ✅ | Kartu statistik, produk terbaru, stok menipis |
| Master Data — Produk | ✅ | CRUD + pencarian + relasi kategori/satuan |
| Master Data — Kategori | ✅ | CRUD |
| Master Data — Satuan (UoM) | ✅ | CRUD |
| Master Data — Pelanggan | ✅ | CRUD |
| Master Data — Pemasok | ✅ | CRUD |
| Master Data — Gudang | ✅ | CRUD |

## 🏗️ Arsitektur

```
ErpMetronic.sln
└── src/
    ├── ErpMetronic.Domain          # Entitas bisnis murni (tanpa dependensi framework)
    ├── ErpMetronic.Infrastructure  # EF Core DbContext, Identity, migrasi, seeder
    └── ErpMetronic.Web             # ASP.NET Core MVC: Controllers, Views, Tema Metronic
```

Pola: **Layered / Clean-ish architecture**. `Web` → `Infrastructure` → `Domain`.

## 🚀 Menjalankan secara cepat

```bash
# 1. Pastikan SQL Server berjalan & sesuaikan connection string di
#    src/ErpMetronic.Web/appsettings.json

# 2. Terapkan migrasi (membuat database ErpMetronicDb)
dotnet ef database update --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web

# 3. Jalankan aplikasi
dotnet run --project src/ErpMetronic.Web
```

Buka `https://localhost:7082` lalu masuk dengan akun admin default:

| Email | Kata Sandi |
|-------|-----------|
| `admin@erpmetronic.local` | `Admin#12345` |

> Database dan data awal (role, admin, contoh master data) dibuat otomatis saat aplikasi pertama dijalankan.

## 📚 Dokumentasi Tahapan

Seluruh proses pembangunan dipecah menjadi tahapan yang jelas. Lihat **[docs/README.md](docs/README.md)**
untuk indeks lengkap, atau langsung ke tiap tahap:

0. [Prasyarat & Persiapan Lingkungan](docs/00-prasyarat.md)
1. [Setup Proyek & Struktur Solusi](docs/01-setup-proyek.md)
2. [Integrasi Tema Metronic 8](docs/02-integrasi-metronic.md)
3. [Database, EF Core & Migrasi](docs/03-database-efcore.md)
4. [Autentikasi & Otorisasi](docs/04-autentikasi-otorisasi.md)
5. [Manajemen User & Role](docs/05-manajemen-user-role.md)
6. [Dashboard](docs/06-dashboard.md)
7. [Modul Master Data](docs/07-master-data.md)
8. [Deployment & Produksi](docs/08-deployment.md)
9. [Roadmap Pengembangan Lanjutan](docs/09-roadmap.md)
10. [Master Menu (Menu Dinamis)](docs/10-master-menu.md)
11. [Manajemen Stok (Inventory)](docs/11-manajemen-stok.md)
12. [Currency Management](docs/12-currency-management.md)
13. [Purchasing (Pembelian)](docs/13-purchasing.md)
14. [Document Numbering](docs/14-document-numbering.md)
15. [Sales (Penjualan)](docs/15-sales.md)
16. [Purchase Requisition & RFQ](docs/16-pr-rfq.md)
17. [Finance (Akuntansi)](docs/17-finance.md)
18. [Retur & Umur Piutang/Hutang](docs/18-retur-aging.md)
19. [Copy To / Copy From (Pembelian)](docs/19-copy-document.md)
20. [Sistem Pajak (PPN & PPh)](docs/20-pajak-ppn-pph.md)
21. [Diskon (Baris & Header)](docs/21-diskon.md)
22. [HPP & Moving Average](docs/22-hpp-moving-average.md)
23. [Laporan Keuangan](docs/23-laporan-keuangan.md)
24. [Termin, Jatuh Tempo & Batas Kredit](docs/24-termin-jatuh-tempo-batas-kredit.md)
25. [Laporan Arus Kas](docs/25-arus-kas.md)
26. [Modul Bank & Kas](docs/26-bank-kas.md)
27. [Tutup Buku](docs/27-tutup-buku.md)
28. [Daftar Harga (Price List)](docs/28-price-list.md)
29. [Penawaran Penjualan (Quotation)](docs/29-sales-quotation.md)
30. [Workflow Persetujuan](docs/30-approval-workflow.md)
31. [Reorder & Saran Pembelian](docs/31-reorder-saran-pembelian.md)

## 🧰 Tech Stack

- .NET 8 (ASP.NET Core MVC)
- Entity Framework Core 8 (SQL Server provider)
- ASP.NET Core Identity
- Bootstrap 5 + Bootstrap Icons (lapisan tema bergaya Metronic 8)
- SQL Server (default instance / LocalDB)
