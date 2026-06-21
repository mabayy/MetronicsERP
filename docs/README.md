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
| 16 | [Purchase Requisition & RFQ](16-pr-rfq.md) | Pengadaan awal: PR (approval) → RFQ (penawaran/pemenang) | ✅ |
| 17 | [Finance (Akuntansi)](17-finance.md) | Bagan akun → jurnal → buku besar → neraca saldo + posting otomatis | ✅ |
| 18 | [Retur & Umur Piutang/Hutang](18-retur-aging.md) | Retur jual/beli (balik stok + jurnal) + laporan umur AR/AP | ✅ |
| 19 | [Copy To / Copy From (Pembelian)](19-copy-document.md) | Salin dokumen antar tahap PR → RFQ → PO → Faktur | ✅ |
| 20 | [Sistem Pajak (PPN & PPh)](20-pajak-ppn-pph.md) | Master pajak, PPN per baris + PPh withholding, posting jurnal otomatis | ✅ |
| 21 | [Diskon (Baris & Header)](21-diskon.md) | Diskon per baris item + diskon header (gaya SAP B1/Odoo), terintegrasi pajak | ✅ |
| 22 | [HPP & Moving Average](22-hpp-moving-average.md) | Biaya rata-rata bergerak + posting HPP otomatis saat pengiriman (perpetual) | ✅ |
| 23 | [Laporan Keuangan](23-laporan-keuangan.md) | Laba Rugi (Income Statement) & Neraca (Balance Sheet) dari jurnal | ✅ |
| 24 | [Termin, Jatuh Tempo & Batas Kredit](24-termin-jatuh-tempo-batas-kredit.md) | Payment term, due date otomatis, batas kredit, aging by due date | ✅ |
| 25 | [Laporan Arus Kas](25-arus-kas.md) | Cash flow: mutasi kas/bank per kategori + saldo awal/akhir | ✅ |
| 26 | [Modul Bank & Kas](26-bank-kas.md) | Master akun kas/bank, routing pembayaran ke GL, rekonsiliasi bank | ✅ |
| 27 | [Tutup Buku](27-tutup-buku.md) | Jurnal penutup laba/rugi → Laba Ditahan + kunci periode | ✅ |

## Cara membaca dokumen ini

- Setiap tahap memiliki bagian **Tujuan**, **Langkah**, **Hasil/Verifikasi**, dan **Catatan**.
- Blok kode menampilkan perintah CLI atau potongan source yang benar-benar dipakai di repo ini.
- Tanda ✅ berarti tahap sudah diimplementasikan dan terverifikasi berjalan.

## Konvensi Proyek

- **Bahasa UI**: Indonesia. **Penamaan kode**: Inggris (konvensi .NET).
- **Namespace root**: `ErpMetronic`.
- **Database**: `ErpMetronicDb` di SQL Server.
- **Akun admin awal**: `admin@erpmetronic.local` / `Admin#12345` (lihat `DbSeeder`).
