# Tahap 30 — Workflow Persetujuan (Approval) Berjenjang

## Tujuan
Kontrol persetujuan ala SAP B1 (Approval Procedure)/Odoo: dokumen yang nilainya melewati **ambang**
wajib disetujui melalui **beberapa langkah berurutan** (per jabatan) sebelum diproses. Diterapkan pada
**Purchase Order**, dengan desain generik (`DocumentType`) agar dokumen lain dapat menyusul.

## Model Data
| Entitas | Peran |
|---------|-------|
| `ApprovalRule` | `Name`, `DocumentType`, `MinAmount` (ambang), `IsActive`, `Steps` |
| `ApprovalRuleStep` | `Level` (urutan), `PositionId` (jabatan penyetuju) |
| `ApprovalRequest` | dibuat saat dokumen melewati ambang: `DocumentType`, `DocumentId`, `Amount`, `Status`, `CurrentLevel`, `Steps` |
| `ApprovalStep` | `Level`, `PositionId`, `Status`, `DecidedBy/At`, `Note` |
| `ApprovalStatus` | Pending, Approved, Rejected |

PO mendapat status baru **PendingApproval**. Menu **Administrasi → Aturan Persetujuan** (admin) dan
**Persetujuan** (top-level, kotak masuk untuk penyetuju).

## Alur
```
PO Draft ──Konfirmasi──▶ (nilai ≥ ambang?) ── ya ─▶ PendingApproval + ApprovalRequest (langkah 1..n)
                                            └─ tidak ▶ Ordered
Approve L1 ▶ L2 ▶ … ▶ Ln  ─(semua disetujui)─▶ PO Ordered
Tolak di langkah mana pun ─▶ PO kembali Draft (untuk revisi)
```
- **Aturan paling spesifik** yang dipakai: ambang tertinggi yang ≤ nilai dokumen.
- **Penyetuju**: pengguna yang jabatannya (`Position`) sama dengan langkah berjalan; **Administrator**
  dapat menyetujui langkah apa pun (override). Kotak Persetujuan menampilkan permintaan yang menunggu
  keputusan jabatan pengguna.
- PO `PendingApproval` tidak bisa menerima barang sampai disetujui.

## Migrasi
```bash
dotnet ef migrations add AddApproval --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web --output-dir Persistence/Migrations
dotnet ef database update --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web
```

## Hasil / Verifikasi (teruji end-to-end)
Aturan: PO ≥ 50.000, 2 langkah (Manajer → Supervisor).

| Skenario | Hasil |
|----------|-------|
| PO 100.000 dikonfirmasi | status **PendingApproval**, request 2 langkah, level 1 ✅ |
| Setujui langkah 1 | level berjalan → 2, PO tetap PendingApproval ✅ |
| Setujui langkah 2 | request **Approved**, PO → **Ordered** ✅ |
| PO 10.000 (< ambang) dikonfirmasi | langsung **Ordered**, tanpa request ✅ |
| PO di atas ambang lalu **ditolak** | request **Rejected**, PO kembali **Draft** ✅ |

Data uji dibersihkan.

## Pengembangan lanjut
- Terapkan ke Sales Order / Faktur / dokumen lain (mesin sudah generik via `DocumentType`).
- Notifikasi ke penyetuju & kondisi aturan tambahan (mis. per kategori/pemasok).
