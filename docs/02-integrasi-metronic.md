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

## Tema Warna (3 Skema yang Dapat Diganti)

Aplikasi menyediakan **3 tema** yang dapat dipilih pengguna lewat tombol palet (ikon
`bi-palette`) di header. Pilihan disimpan di `localStorage` dan diterapkan tanpa reload, serta
ada *no-flash loader* di `<head>` agar tidak berkedip saat memuat.

| Tema | Nama | Palet (gelap→terang) | Karakter |
|------|------|----------------------|----------|
| `sapphire` | **Safir** | `#111844` `#4B5694` `#7288AE` `#EAE0CF` | Navy elegan + aksen krem (default) |
| `mocha` | **Kopi** | `#4B2E2B` `#8C5A3C` `#C08552` `#FFF8F0` | Cokelat hangat |
| `emerald` | **Zamrud** | `#1F6F5F` `#2FA084` `#6FCF97` `#EEEEEE` | Hijau segar |

Implementasi (`wwwroot/css/erp-theme.css`): seluruh warna memakai **token CSS** pada `:root`
dan di-override per `[data-theme="..."]`. Atribut `data-theme` disetel pada elemen `<html>`:

```javascript
// no-flash loader di <head>
(function () {
    var t = localStorage.getItem('erp-theme') || 'sapphire';
    document.documentElement.setAttribute('data-theme', t);
})();
```

**Kontras dijaga (WCAG)**: warna tombol primer dipilih cukup gelap agar teks putih terbaca
(rasio ≥ 4.5:1), judul memakai warna paling gelap dari palet di atas kartu putih, dan teks
menu pada sidebar memakai turunan terang dari palet. Warna status (sukses/hijau,
bahaya/merah) sengaja **dipertahankan lintas tema** agar maknanya tetap konsisten.

Untuk menambah tema baru: duplikasi satu blok `[data-theme="..."]`, ganti nilai token, lalu
tambahkan satu opsi `.theme-option` di switcher header `_Layout.cshtml`.

## Sidebar Expand / Collapse

Sidebar dapat **diciutkan menjadi rail ikon** dan dilebarkan kembali:

- **Desktop**: tombol di header (`#erpCollapse`) menamb/menghapus kelas `erp-sidebar-collapsed`
  pada `<html>`. Saat ciut, lebar sidebar mengecil (`--erp-sidebar-collapsed-width: 78px`) dan
  margin konten ikut menyesuaikan otomatis karena keduanya memakai variabel
  `--erp-sidebar-width`. Label disembunyikan (`.menu-label`, `.brand-label`), heading grup
  menjadi garis pemisah, dan saat **hover** sidebar melebar sementara (*flyout*) tanpa menggeser
  konten.
- **Mobile** (<992px): tetap memakai pola *off-canvas* (tombol hamburger `#erpToggle`
  menggeser sidebar masuk/keluar).
- **Status disimpan** di `localStorage` (`erp-sidebar = collapsed|expanded`) dan diterapkan
  oleh *no-flash loader* di `<head>` agar konsisten antar halaman.

```javascript
document.getElementById('erpCollapse').addEventListener('click', function () {
    var collapsed = document.documentElement.classList.toggle('erp-sidebar-collapsed');
    localStorage.setItem('erp-sidebar', collapsed ? 'collapsed' : 'expanded');
});
```

> Label menu dibungkus `<span class="menu-label">` (di ViewComponent `SidebarMenu`) agar dapat
> disembunyikan saat ciut; atribut `title` pada tiap tautan memberi tooltip ikon.

## Hasil / Verifikasi
Jalankan aplikasi dan buka halaman mana pun — sidebar, header, dan kartu tampil rapi dengan
skema warna Metronic. Ganti tema via tombol palet di header; pilihan bertahan setelah refresh
dan halaman login pun mengikuti tema. Tombol ciut/lebar pada header mengubah sidebar menjadi
rail ikon dan kembali, dengan status yang bertahan antar halaman.

## Selanjutnya
➡️ [Tahap 3 — Database, EF Core & Migrasi](03-database-efcore.md)
