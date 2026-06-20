# Tahap 21 — Diskon (Per Baris & Header)

## Tujuan
Diskon bergaya **SAP Business One / Odoo**: **diskon per baris item** (%) dan **diskon header**
(tingkat dokumen, %) pada PO, SO, Faktur Pembelian & Penjualan, terintegrasi dengan
[Pajak PPN & PPh](20-pajak-ppn-pph.md) dan posting jurnal.

## Konsep & Urutan Hitung (best practice)
1. **Bruto baris** = jumlah × harga.
2. **Diskon baris** (%) → **Neto baris** = bruto − (bruto × disk%).
3. **Subtotal** = Σ neto baris.
4. **Diskon header** (%) → dihitung atas Subtotal, lalu **dialokasikan proporsional** ke tiap baris
   (sesuai bobot neto) — *sebelum* PPN, sesuai praktik SAP B1 (diskon dokumen mengurangi DPP).
5. **DPP** = Subtotal − Diskon header.
6. **PPN** dihitung **per baris** atas (neto baris − alokasi diskon header).
7. **PPh** dihitung atas DPP.
8. **Total** = DPP + PPN − PPh.

Semua nilai di-**snapshot** ke dokumen (tahan terhadap perubahan master). Pembulatan 2 desimal
(setengah ke atas) konsisten antara server (`TaxMath.R2`) & layar (JavaScript).

## Model Data (tambahan)
| Lokasi | Field |
|--------|-------|
| Baris (PO/SO/Faktur) | `DiscountPercent` + computed `LineGross`, `LineDiscountAmount`, `LineNet` |
| Header (PO/SO/Faktur) | `HeaderDiscountPercent`, `HeaderDiscountAmount` (snapshot) + computed `NetBeforeHeaderDiscount`, `Subtotal` (=DPP) |

Tidak ada akun GL diskon terpisah — diskon mengurangi nilai neto yang diposting (Persediaan/HPP atau
Pendapatan), sesuai default SAP B1/Odoo. Karena itu `JournalService` tidak berubah (memakai `Subtotal`
yang sudah neto).

## Antarmuka
- **Form Create/Edit** (PO, SO, Faktur): kolom **Disk %** per baris + input **Diskon Header (%)**, dengan
  ringkasan langsung: Subtotal → Diskon Header → DPP → PPN → PPh → **Total**.
- **Details**: kolom diskon per baris (neto), baris Subtotal & Diskon Header pada rincian total.
- Diskon **mengalir** PO→Faktur (Pembelian) & SO→Faktur (Penjualan) saat disalin.

## Migrasi
```bash
dotnet ef migrations add AddDiscounts --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web --output-dir Persistence/Migrations
dotnet ef database update --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web
```

## Hasil / Verifikasi (teruji end-to-end)
PO 10 × 10.000, diskon baris **10%**, diskon header **5%**, PPN-IN **11%**, PPh23 **2%**:

| Komponen | Nilai |
|----------|-------|
| Bruto | 100.000 |
| Diskon baris 10% | −10.000 → Neto 90.000 |
| Diskon header 5% (atas 90.000) | −4.500 |
| **DPP** | **85.500** |
| PPN 11% (atas DPP) | 9.405 |
| PPh 2% (atas DPP) | −1.710 |
| **Total** | **93.195** |

Jurnal Faktur Pembelian: Dr Persediaan 85.500, Dr PPN Masukan 9.405, Cr Hutang PPh 1.710,
Cr Hutang Usaha 93.195 → **balance 94.905 = 94.905** ✅. Data uji dibersihkan setelah verifikasi.
