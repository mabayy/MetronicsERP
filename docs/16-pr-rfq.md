# Tahap 16 — Pengadaan Awal: Purchase Requisition & RFQ

## Tujuan
Dua dokumen **pengadaan** yang mendahului Purchase Order: **Purchase Requisition (PR)** dan
**Request for Quotation (RFQ)**. Alur pengadaan menjadi: **PR → RFQ → PO**.

> Catatan: PR & RFQ adalah bagian alur **Pembelian/Pengadaan** (bukan penjualan). Keduanya
> ditempatkan di grup menu **Pembelian**.

## Organisasi per modul
Sesuai permintaan, kode modul dipisah agar mudah dicari & dimaintain — tanpa MVC Areas
(yang akan memaksa perubahan skema menu dinamis):

| Lapisan | Lokasi |
|---------|--------|
| Entity | `Domain/Entities/Procurement/` (namespace `ErpMetronic.Domain.Entities`) |
| ViewModel | `ViewModels/Procurement/` (namespace `ErpMetronic.Web.ViewModels`) |
| Controller | `Controllers/Procurement/` (namespace `…Controllers.Procurement`) |
| View | `Views/PurchaseRequisitions/`, `Views/RequestForQuotations/` (per-controller) |

> Alternatif isolasi lebih ketat = **MVC Areas** (Areas/Procurement/...). Itu memerlukan kolom
> `Area` pada `MenuItem` + perubahan routing/`SidebarMenu`. Karena berisiko ke menu yang sudah
> berjalan, dipilih pendekatan subfolder/namespace.

## Purchase Requisition (PR)
Entitas `PurchaseRequisition` + `PurchaseRequisitionLine` (produk, jumlah, estimasi harga, catatan).
Status: **Draft → Submitted → Approved / Rejected**.

| Aksi | Aturan |
|------|--------|
| Create / Edit | Edit hanya saat **Draft** |
| Submit | Draft → Submitted |
| Approve | Submitted → Approved (mencatat `ApprovedBy`/`ApprovedAt`) |
| Reject | Submitted → Rejected (+ alasan) |
| Delete | hanya Draft |
| Buat RFQ | tersedia saat **Approved** (menyalin baris ke RFQ) |

## Request for Quotation (RFQ)
Entitas `RequestForQuotation` + `RfqLine` (produk, jumlah) + `RfqQuote` (penawaran per pemasok:
nilai, lead time, catatan, penanda pemenang). Status: **Draft → Sent → Closed**.

| Aksi | Aturan |
|------|--------|
| Create (`?prId=`) | dapat menyalin baris dari PR yang **Approved** |
| Tambah Penawaran | per pemasok (tak boleh duplikat pemasok); ditolak bila RFQ Closed |
| Send | Draft → Sent |
| Award (pilih pemenang) | hanya saat **Sent**; menandai `IsSelected` & menutup RFQ (Closed) |
| Delete | hanya Draft |

Nomor dokumen `PR` & `RFQ` dari [Document Numbering](14-document-numbering.md) (otomatis `IsSystem`).
Menu **Pembelian → Purchase Requisition / Request for Quotation** (seeder, idempoten).

## Migrasi
```bash
dotnet ef migrations add AddProcurement --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web --output-dir Persistence/Migrations
dotnet ef database update --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web
```

## Hasil / Verifikasi (teruji end-to-end)
| Skenario | Hasil |
|----------|-------|
| PR: Buat → Ajukan → Setujui | status Approved, `ApprovedBy` terisi |
| Buat RFQ dari PR | baris ter-copy dari PR |
| RFQ: Kirim → 2 penawaran → pilih termurah | pemenang 178.000, RFQ **Closed** |
| Tolak PR (Submitted) | status Rejected + alasan tersimpan |
| Tambah penawaran duplikat / pada RFQ Closed | **ditolak** |

## Pengembangan Lanjutan
- Konversi penawaran terpilih RFQ → **Purchase Order** otomatis (supplier + harga pemenang).
- Penawaran per-baris (harga per item) untuk perbandingan rinci antar pemasok.
