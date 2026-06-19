# Tahap 13 — Purchasing (Pembelian)

## Tujuan
Mengimplementasikan modul **Pembelian** sesuai [Roadmap](09-roadmap.md): **Purchase Order (PO)**
→ **Penerimaan barang** → **update stok otomatis**, dengan relasi ke `Supplier` & `Product`.

## Model Data

| Entitas | Peran |
|---------|-------|
| `PurchaseOrder` | Header PO: nomor, tanggal, pemasok, gudang tujuan, mata uang, status, item |
| `PurchaseOrderItem` | Baris PO: produk, jumlah, harga satuan, **jumlah diterima** (akumulasi) |
| `PurchaseOrderStatus` (enum) | `Draft`, `Ordered`, `PartiallyReceived`, `Received`, `Cancelled` |
| `GoodsReceipt.PurchaseOrderId` | Menautkan dokumen penerimaan ke PO sumber |

`PurchaseOrderItem.OutstandingQuantity` = `Quantity − ReceivedQuantity` (sisa yang belum diterima).

## Alur & Business Rules (umum ERP)

```
Draft ──Konfirmasi──▶ Ordered ──Terima──▶ PartiallyReceived ──Terima──▶ Received
  │                      │
  └──────── Batal ───────┘  (hanya bila belum ada penerimaan)
```

| Aturan | Implementasi |
|--------|--------------|
| PO baru berstatus **Draft** (belum memengaruhi stok) | `Create` |
| Hanya **Draft** yang bisa dikonfirmasi → **Ordered** | `Confirm` |
| Penerimaan hanya untuk **Ordered / PartiallyReceived** | `Receive` |
| Jumlah terima **≤ sisa (outstanding)** per item | validasi di `Receive` |
| Penerimaan → **Stok Masuk otomatis** + update `ReceivedQuantity` | `IStockService.StockInAsync` dalam transaksi |
| Status PO otomatis: semua item lunas → **Received**, sebagian → **PartiallyReceived** | dihitung ulang setelah terima |
| **Batal** hanya bila Draft/Ordered **dan belum ada penerimaan** | `Cancel` |
| PO **Received/Cancelled** tidak bisa diterima/dibatalkan lagi | guard status |
| Multi-currency: PO punya mata uang harga | `CurrencyId` |

Penerimaan membuat dokumen `GoodsReceipt` (lihat [Tahap 11](11-manajemen-stok.md)) yang tertaut ke
PO (`PurchaseOrderId`), sehingga muncul di **Riwayat Penerimaan** pada detail PO dan di **Kartu Stok**.

## Posting Penerimaan (transaksional)

```csharp
await using var tx = await _db.Database.BeginTransactionAsync();
var receipt = new GoodsReceipt { ..., PurchaseOrderId = po.Id, Lines = [...] };
_db.GoodsReceipts.Add(receipt); await _db.SaveChangesAsync();
foreach (var (item, qty) in toReceive) {
    await _stock.StockInAsync(item.ProductId, po.WarehouseId, qty, date, $"Penerimaan {receipt.Ref} (PO {po.Ref})", user);
    item.ReceivedQuantity += qty;
}
po.Status = po.Items.All(i => i.ReceivedQuantity >= i.Quantity)
    ? PurchaseOrderStatus.Received : PurchaseOrderStatus.PartiallyReceived;
await _db.SaveChangesAsync();
await tx.CommitAsync();
```

## Controller & Halaman — `PurchaseOrdersController` (`[Authorize]`)

| Action | Fungsi |
|--------|--------|
| `Index` | Daftar PO + status |
| `Create` | Buat PO (baris dinamis, subtotal otomatis) → Draft |
| `Details` | Header, item (Dipesan/Diterima/Sisa), tombol aksi sesuai status, riwayat penerimaan |
| `Confirm` | Draft → Ordered |
| `Receive` (GET/POST) | Form terima per baris (default = sisa) → posting stok |
| `Cancel` | Membatalkan PO yang belum diterima |

Menu **Pembelian → Purchase Order** (ditambahkan idempoten oleh seeder).

## Migrasi
```bash
dotnet ef migrations add AddPurchasing --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web --output-dir Persistence/Migrations
dotnet ef database update --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web
```

## Hasil / Verifikasi (teruji end-to-end)
PO 40 unit produk PRD-0001 (saldo awal 100):

| Langkah | Status PO | Stok | Diterima |
|---------|-----------|------|----------|
| Buat PO | Draft | 100 | 0 |
| Konfirmasi | Ordered | 100 | 0 |
| Terima 15 | PartiallyReceived | **115** | 15 |
| Terima 25 | Received | **140** | 40 |
| Terima lagi (PO selesai) | — | tetap 140 | **ditolak** |
| Batalkan PO selesai | tetap Received | — | **ditolak** |
| Terima 999 (sisa 5) | — | tetap | **ditolak** ("melebihi sisa") |

Penerimaan tercatat sebagai `GoodsReceipt` tertaut PO & memunculkan pergerakan di Kartu Stok.

## Edit PO (hanya saat Draft) — ✅
PO dapat **diubah penuh** (pemasok, gudang, mata uang, item) selama berstatus **Draft**. Setelah
dikonfirmasi (Ordered) atau menerima barang, PO tidak dapat diedit (jaga integritas). Tombol
**Edit** muncul di halaman detail PO hanya pada status Draft.

## Faktur Pembelian & Pembayaran (3-way matching) — ✅

| Entitas | Peran |
|---------|-------|
| `PurchaseInvoice` + `PurchaseInvoiceLine` | Faktur (hutang) dibuat **dari PO** |
| `PurchasePayment` | Pembayaran terhadap faktur (mengurangi sisa) |
| `PurchaseInvoiceStatus` | Unpaid → PartiallyPaid → Paid |

**3-way matching (PO ↔ Penerimaan ↔ Faktur):** jumlah yang dapat difaktur per produk =
**diterima − sudah difaktur**. Faktur melebihi itu **ditolak**. Faktur dibuat dari layar
*Faktur dari PO* (`PurchaseInvoicesController.SelectPo` → `Create?poId=`), prefilled jumlah sisa
difaktur & harga dari PO.

**Pembayaran (hutang):** mencatat `PurchasePayment` (≤ sisa tagihan), memperbarui `PaidAmount`
dan status (Lunas bila terbayar penuh). Nomor faktur (`PINV`) & pembayaran (`PPAY`) dari
[Document Numbering](14-document-numbering.md).

### Verifikasi (teruji)
| Skenario (PO 25, diterima 12) | Hasil |
|-------------------------------|-------|
| Edit PO saat Draft (qty 20→25) | tersimpan |
| Faktur 8 (≤ diterima 12) | total 72.000, status Belum Dibayar |
| Faktur 10 (sisa difaktur 4) | **ditolak** ("melebihi yang dapat difaktur") |
| Bayar 50.000 | Dibayar Sebagian, sisa 22.000 |
| Bayar 22.000 | **Lunas** |

## Master data
Pemasok & Pelanggan kini **di-seed** otomatis (2 contoh masing-masing) agar PO/penjualan langsung
bisa dicoba; tambah/ubah lewat master data.

## Pengembangan Lanjutan
- Modul **Penjualan** (Sales Order → Pengiriman → Invoice → Pembayaran) — langkah berikutnya.
- Faktur tanpa PO (biaya lain), tiga-arah matching lintas multi-penerimaan, retur pembelian.
