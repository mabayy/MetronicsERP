# Tahap 2 — Integrasi Tema Metronic 8

## Tujuan
Membangun kerangka tampilan (layout, sidebar, header) bergaya **Metronic 8 Demo1**, serta
menyediakan jalur yang jelas untuk beralih ke bundle Metronic berlisensi.

## Pendekatan
Agar aplikasi langsung berjalan tanpa file berlisensi, proyek memakai **lapisan tema bergaya
Metronic** di atas Bootstrap 5 (sudah tersedia di template MVC):

- `wwwroot/css/erp-theme.css` — variabel warna & komponen meniru Metronic 8 (sidebar gelap
  `#1e1e2d`, primary `#009ef7`, body `#f5f8fa`, kartu rounded, widget statistik).
- Bootstrap Icons via CDN untuk ikon.
- Layout `_Layout.cshtml` dengan **sidebar tetap + header sticky + area konten + footer**.

## Struktur File Tampilan

```
Views/Shared/
├── _Layout.cshtml        # Shell aplikasi (sidebar + header + body)
├── _AuthLayout.cshtml    # Layout halaman login (tanpa sidebar)
└── _Sidebar.cshtml       # Partial menu navigasi (penanda aktif otomatis)
wwwroot/css/
└── erp-theme.css         # Tema bergaya Metronic
```

## Komponen Kunci

### Penanda menu aktif (`_Sidebar.cshtml`)
```cshtml
@{
    var controller = ViewContext.RouteData.Values["controller"]?.ToString() ?? "";
    string Active(string c) => string.Equals(controller, c, StringComparison.OrdinalIgnoreCase) ? "active" : "";
}
<a class="@Active("Dashboard")" asp-controller="Dashboard" asp-action="Index">...</a>
```

### Menu khusus admin
Bagian "Administrasi" (Pengguna & Role) hanya tampil bila `User.IsInRole("Administrator")`.

### Notifikasi global
`_Layout.cshtml` menampilkan `TempData["Success"]` / `TempData["Error"]` sebagai alert Bootstrap
yang otomatis muncul setelah operasi CRUD.

## Cara Beralih ke Metronic Berlisensi (opsional)

1. Salin `dist/assets` Metronic ke `wwwroot/assets`.
2. Pada `_Layout.cshtml` & `_AuthLayout.cshtml`, ganti referensi:
   ```html
   <!-- Ganti baris Bootstrap + erp-theme dengan bundle Metronic -->
   <link href="~/assets/plugins/global/plugins.bundle.css" rel="stylesheet" />
   <link href="~/assets/css/style.bundle.css" rel="stylesheet" />
   ...
   <script src="~/assets/plugins/global/plugins.bundle.js"></script>
   <script src="~/assets/js/scripts.bundle.js"></script>
   ```
3. Sesuaikan markup layout dengan struktur HTML Metronic (`#kt_app_sidebar`, `kt_app_header`, dsb.)
   bila ingin memakai komponen JS Metronic (drawer, menu, dll).
4. Kelas tema kustom (`erp-sidebar`, `stat-card`, `badge-light-*`) dapat dipertahankan atau
   dipetakan ke kelas Metronic (`app-sidebar`, `card`, `badge-light-success`).

> Karena tema kustom dibangun di atas Bootstrap 5 yang sama dengan Metronic 8, sebagian besar
> kelas (`card`, `table`, `btn`, grid) sudah kompatibel.

## Hasil / Verifikasi
Jalankan aplikasi dan buka halaman mana pun — sidebar, header, dan kartu tampil rapi dengan
skema warna Metronic.

## Selanjutnya
➡️ [Tahap 3 — Database, EF Core & Migrasi](03-database-efcore.md)
