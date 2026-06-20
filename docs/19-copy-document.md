# Tahap 19 — Copy To / Copy From (Pembelian)

## Tujuan
Mempercepat alur pengadaan dengan fitur **menyalin dokumen** antar tahap pada modul pembelian,
mengikuti rantai dokumen: **PR → RFQ → PO → Penerimaan → Faktur**. Pengguna dapat menyalin baris
item dari dokumen sumber ke form dokumen berikutnya tanpa input ulang.

## Konsep
Setiap relasi salin memiliki dua titik masuk yang setara:

- **Copy To** — tombol di halaman **Details** dokumen sumber → membuka form tujuan yang sudah terisi.
- **Copy From** — panel pemilih di atas form **Create** dokumen tujuan → pilih sumber lalu "Salin".

Keduanya menuju aksi `Create` yang sama dengan parameter sumber, sehingga hasilnya identik.

## Matriks Salin

| Dari | Ke | Yang disalin | Syarat sumber |
|------|----|--------------|---------------|
| Purchase Requisition | RFQ | produk + jumlah | PR **Disetujui** |
| Purchase Requisition | Purchase Order | produk + jumlah + estimasi harga → harga satuan | PR **Disetujui** |
| RFQ | Purchase Order | produk + jumlah + **pemasok pemenang** | RFQ **Ditutup** (ada pemenang) |
| Purchase Order | Faktur Pembelian | baris yang dapat difaktur (3-way matching) | PO sudah diterima (sebagian/penuh) |

> Penerimaan barang (PO → Goods Receipt) memang sudah menyalin baris *outstanding* PO pada form
> Terima Barang, dan Faktur menyalin baris yang dapat difaktur — keduanya bentuk "copy from PO".

## Implementasi

### Controller
- `PurchaseOrdersController.Create(int? fromPr, int? fromRfq)` — memuat sumber & mengisi
  `PurchaseLineInput` (PR: harga = `EstimatedPrice`; RFQ: pemasok = penawaran `IsSelected`).
  `PopulateAsync` juga menyiapkan `ViewBag.SourcePrs` (PR disetujui) & `ViewBag.SourceRfqs` (RFQ ditutup).
- `RequestForQuotationsController.Create(int? prId)` — mengisi `RfqLineInput` dari PR disetujui;
  `PopulateAsync` menyiapkan `ViewBag.SourcePrs`.
- Pesan inline `ViewBag.CopiedFrom` ditampilkan saat form terisi hasil salinan.

### View
- Form **Create** PO & RFQ kini **merender baris `Model.Items`/`Model.Lines` di server** (sebelumnya
  selalu satu baris kosong) dan hanya menambah baris kosong via JS bila tidak ada salinan.
- Panel **Copy From** berisi dropdown sumber + tombol **Salin** (navigasi ke `Create?fromPr=`/`?fromRfq=`/`?prId=`).
- Tombol **Copy To** di Details: PR → "Salin ke RFQ" & "Salin ke PO"; RFQ → "Salin ke PO";
  PO → "Salin ke Faktur".

## Hasil / Verifikasi (teruji end-to-end)
PR (produk PRD-0001, qty 4, estimasi 12.000) disetujui:

| Aksi salin | Hasil di form tujuan |
|------------|----------------------|
| PR → PO | qty 4, harga satuan 12.000, produk terpilih, alert "Disalin dari PR" ✅ |
| PR → RFQ | qty 4, produk terpilih, alert "Disalin dari PR" ✅ |
| RFQ → PO | qty 4, **pemasok pemenang terpilih** (PT Sumber Makmur), alert "Disalin dari RFQ" ✅ |

Data uji dibersihkan setelah verifikasi.
