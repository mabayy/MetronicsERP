# Tahap 7 — Modul Master Data

## Tujuan
Menyediakan CRUD untuk data master ERP: Produk, Kategori, Satuan (UoM), Pelanggan, Pemasok,
dan Gudang.

## Daftar Modul & Controller

| Modul | Controller | Entitas | Field Utama |
|-------|-----------|---------|-------------|
| Produk | `ProductsController` | `Product` | SKU, Nama, Harga Beli/Jual, Stok, Kategori, Satuan |
| Kategori | `CategoriesController` | `Category` | Kode, Nama, Deskripsi |
| Satuan | `UnitOfMeasuresController` | `UnitOfMeasure` | Kode, Nama |
| Pelanggan | `CustomersController` | `Customer` | Kode, Nama, Email, Telepon, Alamat, Kota |
| Pemasok | `SuppliersController` | `Supplier` | Kode, Nama, Kontak, Email, Telepon, Alamat |
| Gudang | `WarehousesController` | `Warehouse` | Kode, Nama, Lokasi |

Semua controller ditandai `[Authorize]`.

## Pola CRUD Standar

Setiap controller master data mengikuti pola yang konsisten:

```csharp
public async Task<IActionResult> Index() => View(await _db.Set.OrderBy(...).ToListAsync());

public IActionResult Create() => View(new TEntity());

[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> Create(TEntity model)
{
    if (!ModelState.IsValid) return View(model);
    model.CreatedBy = User.Identity?.Name;
    _db.Set.Add(model);
    await _db.SaveChangesAsync();
    TempData["Success"] = "...berhasil ditambahkan.";
    return RedirectToAction(nameof(Index));
}
// Edit (GET/POST), Delete (POST) mengikuti pola serupa + audit UpdatedBy/UpdatedAt
```

### Kolom audit
`CreatedBy`/`CreatedAt` diisi saat create; `UpdatedBy`/`UpdatedAt` saat edit — diambil dari
`User.Identity?.Name`.

### Penghapusan aman
Kategori & Satuan menangkap `DbUpdateException` (relasi `Restrict`) dan menampilkan pesan
"tidak dapat dihapus karena masih dipakai produk".

## Modul Produk (lebih kaya)

`ProductsController` menambah:
- **Pencarian** pada Index (`?search=` berdasarkan Nama atau SKU).
- **Dropdown relasi** Kategori & Satuan via `SelectList` (`ViewBag.Categories`, `ViewBag.Units`).
- **Halaman Details** dengan `Include(Category)` & `Include(UnitOfMeasure)`.
- Badge stok: merah bila `StockQuantity <= ReorderLevel`, hijau bila aman.

```csharp
private async Task PopulateDropdownsAsync(Product? model = null)
{
    ViewBag.Categories = new SelectList(await _db.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", model?.CategoryId);
    ViewBag.Units      = new SelectList(await _db.UnitOfMeasures.OrderBy(u => u.Name).ToListAsync(), "Id", "Name", model?.UnitOfMeasureId);
}
```

## Tampilan
- **Index**: kartu + tabel, tombol "Tambah", aksi Edit/Hapus (konfirmasi via `confirm()`),
  badge status Aktif/Nonaktif.
- **Create/Edit**: form Bootstrap grid dengan validasi client+server
  (`asp-validation-for` + `_ValidationScriptsPartial`).
- Navigasi master data tersedia di sidebar bagian "Master Data".

## Hasil / Verifikasi
Seluruh halaman Index/Create/Edit/Details master data mengembalikan **200**. Uji tulis penuh
(buat Kategori via POST) terverifikasi: redirect 302 → data muncul di daftar.

## Catatan Perluasan
Menambah master data baru = ulangi pola di atas: entitas (Domain) → `DbSet` + konfigurasi
(DbContext) → migrasi → controller → 3 view → item menu sidebar.

## Selanjutnya
➡️ [Tahap 8 — Deployment & Produksi](08-deployment.md)
