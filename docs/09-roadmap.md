# Tahap 9 — Roadmap Pengembangan Lanjutan

Fondasi (Auth, User/Role, Dashboard, Master Data) telah selesai. Dokumen ini merinci modul ERP
lanjutan yang dapat dibangun di atasnya, mengikuti pola arsitektur yang sama.

## Modul yang Direncanakan

### 1. Inventory (Persediaan)
- Entitas: `StockMovement` (in/out/adjustment), `StockOpname`.
- Relasi ke `Product` & `Warehouse`.
- Kartu stok (stock card), stok per gudang, nilai persediaan.

### 2. Purchasing (Pembelian)
- Entitas: `PurchaseOrder`, `PurchaseOrderItem`, `GoodsReceipt`.
- Alur: PO → Penerimaan barang → update stok otomatis (memicu `StockMovement`).
- Relasi ke `Supplier` & `Product`.

### 3. Sales (Penjualan)
- Entitas: `SalesOrder`, `SalesOrderItem`, `Invoice`, `Payment`.
- Alur: SO → pengiriman (kurangi stok) → Invoice → Pembayaran.
- Relasi ke `Customer` & `Product`.

### 4. Finance / Akuntansi Dasar
- Entitas: `ChartOfAccount`, `JournalEntry`, `JournalLine`.
- Buku besar (general ledger), neraca saldo sederhana.
- Posting otomatis dari transaksi pembelian/penjualan.

## Peningkatan Teknis

| Area | Rencana |
|------|---------|
| Validasi | FluentValidation untuk aturan bisnis kompleks |
| Mapping | AutoMapper / Mapster antara entity ↔ viewmodel |
| Query | Repository / CQRS (MediatR) bila kompleksitas meningkat |
| Audit | Audit trail otomatis via interceptor EF Core (`SaveChanges`) |
| Tabel | Server-side DataTables / paging + sorting + export (Excel/PDF) |
| Multi-tenant | Pemisahan data per perusahaan/cabang |
| API | Endpoint Web API + Swagger untuk integrasi/mobile |
| Lokalisasi | Multi-bahasa via `IStringLocalizer` |
| Notifikasi | SignalR untuk notifikasi real-time (mis. stok menipis) |
| Pengujian | xUnit + integration test (WebApplicationFactory) |

## Pola Menambah Modul Baru (ringkas)
1. **Domain**: buat entitas mewarisi `BaseEntity`.
2. **Infrastructure**: tambah `DbSet` + konfigurasi `OnModelCreating` → buat migrasi.
3. **Web**: controller (CRUD/transaksi) + view (Index/Create/Edit/Details).
4. **UI**: tambahkan item menu di `_Sidebar.cshtml`.
5. **Keamanan**: terapkan `[Authorize(Roles = ...)]` sesuai kebutuhan.
6. **Verifikasi**: build + uji alur via browser/HTTP.

## Prioritas Saran
1. Inventory (paling dekat dengan master data yang sudah ada).
2. Purchasing → memperkaya pergerakan stok masuk.
3. Sales → pergerakan stok keluar + piutang.
4. Finance → mengikat semuanya menjadi laporan keuangan.
