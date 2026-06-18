# Tahap 0 — Prasyarat & Persiapan Lingkungan

## Tujuan
Memastikan semua perangkat lunak dan aset yang dibutuhkan tersedia sebelum membangun aplikasi.

## Kebutuhan Perangkat Lunak

| Komponen | Versi | Verifikasi |
|----------|-------|-----------|
| .NET SDK | 8.0.x | `dotnet --version` |
| SQL Server | 2019/2022 atau LocalDB | `sqllocaldb info` atau cek service `MSSQLSERVER` |
| EF Core Tools | 8.0.x | `dotnet ef --version` |
| Editor | Visual Studio 2022 / VS Code / Rider | — |
| Tema Metronic 8 | Lisensi KeenThemes (HTML/Bootstrap demo) | file `dist/assets` |

## Langkah

### 1. Pasang .NET 8 SDK
Unduh dari <https://dotnet.microsoft.com/download/dotnet/8.0>. Verifikasi:
```bash
dotnet --list-sdks
# harus memuat baris 8.0.xxx
```

### 2. Siapkan SQL Server
Gunakan salah satu:
- **SQL Server Express/Developer** (instance `MSSQLSERVER`) — connection: `Server=localhost;...`
- **LocalDB** — connection: `Server=(localdb)\\MSSQLLocalDB;...`

Cek instance aktif:
```powershell
Get-Service | Where-Object { $_.Name -like 'MSSQL*' }
```

### 3. Pasang EF Core Tools (global)
```bash
dotnet tool install --global dotnet-ef --version 8.0.11
```

### 4. Siapkan file Metronic 8
Proyek ini berjalan dengan **lapisan tema bergaya Metronic** (Bootstrap 5 + CSS kustom) agar
langsung bisa dijalankan tanpa file berlisensi. Jika Anda memiliki lisensi Metronic 8:

1. Unduh paket Metronic dari akun KeenThemes Anda.
2. Salin folder `metronic/demo1/dist/assets` ke dalam `src/ErpMetronic.Web/wwwroot/assets`.
3. Ikuti [Tahap 2](02-integrasi-metronic.md) untuk mengganti referensi CSS/JS.

> **Catatan lisensi:** Metronic adalah produk komersial KeenThemes. Jangan men-commit file
> berlisensi ke repositori publik.

## Hasil / Verifikasi
Lingkungan siap apabila keempat perintah verifikasi pada tabel di atas mengembalikan versi yang sesuai.

## Selanjutnya
➡️ [Tahap 1 — Setup Proyek & Struktur Solusi](01-setup-proyek.md)
