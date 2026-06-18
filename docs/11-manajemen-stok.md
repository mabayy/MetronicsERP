# Tahap 11 — Manajemen Stok (Inventory)

## Tujuan
Mengelola pergerakan stok produk per gudang melalui empat operasi: **Stok Masuk**,
**Stok Keluar**, **Transfer antar gudang**, dan **Penyesuaian (stock opname)** — lengkap dengan
buku besar pergerakan dan saldo per gudang.

## Model Data

| Entitas | Peran |
|---------|-------|
| `ProductStock` | Saldo stok satu produk pada satu gudang (`ProductId` + `WarehouseId` unik) |
| `StockMovement` | Buku besar setiap transaksi stok (header) |
| `MovementType` (enum) | `StockIn`, `StockOut`, `Transfer`, `Adjustment` |

`StockMovement` menyimpan: `ReferenceNumber` (mis. `IN-20260618-0001`), `MovementDate`, `Type`,
`ProductId`, `Quantity`, `WarehouseId` (asal/utama), `DestinationWarehouseId` (khusus transfer),
`Note`, serta audit `CreatedBy`/`CreatedAt`. `Product.StockQuantity` tetap dijaga sebagai
**total** lintas gudang.

Konfigurasi penting di `ApplicationDbContext`:
- `ProductStock`: indeks unik `(ProductId, WarehouseId)`.
- `StockMovement`: **dua** relasi ke `Warehouse` (asal & tujuan), semuanya `OnDelete(Restrict)`.

## Logika Bisnis — `StockService`

Seluruh aturan stok dienkapsulasi di `Infrastructure/Services/StockService.cs` (di-DI sebagai
`IStockService`), tiap operasi memvalidasi lalu menyimpan dalam satu `SaveChangesAsync` (atomik):

| Operasi | Efek saldo | Validasi |
|---------|-----------|----------|
| **Stock In** | `+qty` di gudang; total produk `+qty` | qty > 0 |
| **Stock Out** | `-qty` di gudang; total `-qty` | qty > 0 **dan** saldo ≥ qty |
| **Transfer** | `-qty` gudang asal, `+qty` gudang tujuan; total **tetap** | qty > 0, asal ≠ tujuan, saldo asal ≥ qty |
| **Adjustment** | saldo di-set ke hasil hitung fisik; total `± selisih` | hasil hitung ≥ 0 |

Nomor referensi dibuat otomatis: `{IN|OUT|TRF|ADJ}-yyyyMMdd-####`.

```csharp
public async Task<StockResult> TransferAsync(int productId, int sourceWh, int destWh, int qty, ...)
{
    if (qty <= 0) return StockResult.Fail("Jumlah harus lebih dari 0.");
    if (sourceWh == destWh) return StockResult.Fail("Gudang asal dan tujuan harus berbeda.");
    var source = await GetOrCreateStockAsync(productId, sourceWh);
    if (source.Quantity < qty) return StockResult.Fail($"Stok gudang asal tidak mencukupi...");
    source.Quantity -= qty;
    (await GetOrCreateStockAsync(productId, destWh)).Quantity += qty;   // total tidak berubah
    _db.StockMovements.Add(BuildMovement(MovementType.Transfer, ...));
    await _db.SaveChangesAsync();
    return StockResult.Ok(movement);
}
```

## Controller & Halaman — `StockController` (`[Authorize]`)

| Action | Halaman |
|--------|---------|
| `In` / `Out` / `Transfer` / `Adjust` (GET+POST) | Form tiap operasi |
| `Movements` | Riwayat pergerakan (filter jenis & produk, badge berwarna, jumlah bertanda) |
| `Balances` | Saldo per produk per gudang (sorot stok menipis) |
| `GetBalance` (AJAX) | Mengembalikan saldo saat ini untuk produk+gudang terpilih (JSON) |

Form memuat partial bersama `_StockBalanceScript.cshtml` yang menampilkan **saldo saat ini**
begitu produk & gudang dipilih (memanggil `GetBalance`). Operasi gagal (mis. stok kurang)
menampilkan pesan dan **tidak** mengubah data.

## Menu & Seeding

`DbSeeder` menambahkan (idempoten) grup menu **Manajemen Stok** beserta enam sub-menunya,
gudang default (`WH-01`, `WH-02`), dan **alokasi saldo awal** (memindahkan `StockQuantity`
tiap produk ke gudang pertama). Karena sidebar dinamis (lihat [Tahap 10](10-master-menu.md)),
menu langsung muncul.

## Migrasi
```bash
dotnet ef migrations add AddInventory --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web --output-dir Persistence/Migrations
dotnet ef database update --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web
```

## Hasil / Verifikasi (teruji end-to-end)
Skenario produk PRD-0001 (saldo awal WH-01 = 100):

| Langkah | Hasil |
|---------|-------|
| Stok Masuk 50 → WH-01 | WH-01 = 150 |
| Stok Keluar 20 ← WH-01 | WH-01 = 130 |
| Transfer 30 WH-01 → WH-02 | WH-01 = 100, WH-02 = 30 (total tetap) |
| Penyesuaian WH-02 → 25 | WH-02 = 25 (selisih −5), **total produk = 125** |
| Stok keluar 999 (saldo 25) | **Ditolak** — "stok tidak mencukupi", saldo tak berubah |
| Transfer gudang sama | **Ditolak** — "gudang asal dan tujuan harus berbeda" |
| `GetBalance?productId=1&warehouseId=1` | `{"quantity":100}` |

Empat pergerakan tercatat dengan nomor referensi; semua halaman Stock mengembalikan **200**.

## Pengembangan Lanjutan
- Kartu stok (stock card) per produk, nilai persediaan (qty × harga).
- Integrasi ke modul Pembelian (penerimaan → otomatis Stok Masuk) & Penjualan
  (pengiriman → otomatis Stok Keluar) — lihat [Roadmap](09-roadmap.md).
