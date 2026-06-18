# Tahap 10 — Master Menu (Menu Dinamis)

## Tujuan
Mengubah menu sidebar dari hardcoded menjadi **menu dinamis berbasis database** yang dapat
**ditambah, diedit, dihapus, dan diurutkan ulang** (drag-and-drop) oleh administrator.

## Komponen

| Berkas | Peran |
|--------|-------|
| `Domain/Entities/MenuItem.cs` | Entitas menu (self-referencing untuk hierarki induk→anak) |
| `Persistence/ApplicationDbContext.cs` | `DbSet<MenuItem>` + relasi `Parent`/`Children` (`DeleteBehavior.Restrict`) |
| `Persistence/DbSeeder.cs` | Seed menu default (Dashboard, Master Data, Administrasi) |
| `Controllers/MenusController.cs` | CRUD + endpoint `Reorder` (AJAX) — khusus `Administrator` |
| `ViewComponents/SidebarMenuViewComponent.cs` | Merender sidebar dari DB + filter role |
| `Views/Shared/Components/SidebarMenu/Default.cshtml` | Template sidebar dinamis |
| `Views/Menus/*` | Index (drag-drop), Create, Edit |

## Model Data

```csharp
public class MenuItem : BaseEntity
{
    public string Title { get; set; }
    public string? Icon { get; set; }         // kelas Bootstrap Icons, mis. "bi-box"
    public string? Controller { get; set; }   // tujuan MVC; kosong = grup/header
    public string? Action { get; set; }
    public string? Url { get; set; }           // alternatif controller (eksternal/kustom)
    public int SortOrder { get; set; }         // urutan antar item sejajar
    public string? RequiredRole { get; set; }  // batasi tampil per role
    public int? ParentId { get; set; }         // hierarki 1 tingkat
    public MenuItem? Parent { get; set; }
    public ICollection<MenuItem> Children { get; set; }
    public bool IsSystem { get; set; }         // menu bawaan: tak bisa dihapus
}
```

Hierarki **satu tingkat**: item tanpa `ParentId` adalah level atas; bila memiliki anak ia tampil
sebagai **header grup** (tidak dapat diklik). Bila tidak punya anak namun punya
`Controller`/`Url`, ia menjadi **tautan langsung**.

## Render Sidebar (ViewComponent)

`_Layout.cshtml` memanggil:
```cshtml
@await Component.InvokeAsync("SidebarMenu")
```
ViewComponent memuat item aktif, **memfilter berdasarkan role** (`RequiredRole` kosong → tampil
untuk semua; selain itu cek `User.IsInRole`), lalu menyusun pohon induk→anak terurut
`SortOrder`. Grup tanpa anak yang terlihat & tanpa tujuan sendiri otomatis disembunyikan.

## Pengurutan Drag-and-Drop

- Halaman `Menus/Index` memakai **SortableJS**. Tiap daftar (level atas & tiap grup anak)
  adalah zona sortable.
- Saat item dilepas, JavaScript mengirim urutan baru ke endpoint AJAX:

```js
fetch('/Menus/Reorder', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
    body: JSON.stringify({ parentId: <id|null>, ids: [<urutan id>] })
});
```

- Endpoint controller menyetel ulang `SortOrder` (1..n) dan **memperbarui `ParentId`** sesuai
  daftar tujuan — sehingga sub-menu dapat dipindah antar grup hanya dengan menyeret.

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> Reorder([FromBody] ReorderRequest request)
{
    if (request?.Ids is not { Length: > 0 }) return Json(new { success = true });
    var items = await _db.MenuItems.Where(m => request.Ids.Contains(m.Id)).ToListAsync();
    for (var i = 0; i < request.Ids.Length; i++)
    {
        var item = items.FirstOrDefault(x => x.Id == request.Ids[i]);
        if (item is null) continue;
        item.SortOrder = i + 1;
        item.ParentId = request.ParentId;
    }
    await _db.SaveChangesAsync();
    return Json(new { success = true });
}
```

> **Anti-forgery untuk AJAX**: di `Program.cs` dikonfigurasi
> `builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");`
> agar token bisa dikirim lewat header.

Untuk memindahkan item **antara level atas dan sub-menu**, gunakan tombol **Edit** lalu ubah
field "Menu Induk".

## Aturan & Proteksi
- Menu **sistem** (`IsSystem = true`) tidak dapat dihapus (hanya dapat dinonaktifkan/diedit/diurutkan).
- Menghapus menu induk akan menghapus sub-menunya.
- Menu tidak boleh menjadi induk dirinya sendiri (divalidasi saat Edit).
- Seluruh aksi `Menus` dibatasi `[Authorize(Roles = "Administrator")]`.

## Migrasi
```bash
dotnet ef migrations add AddMenuItems \
  --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web \
  --output-dir Persistence/Migrations
dotnet ef database update --project src/ErpMetronic.Infrastructure --startup-project src/ErpMetronic.Web
```

## Hasil / Verifikasi (teruji)
- Sidebar dirender dari DB (Dashboard, Master Data, Administrasi, Master Menu, dst.).
- `Menus`, `Menus/Create`, `Menus/Edit/{id}` → **200**.
- **Reorder**: urutan `4,5,6,7,8,9` → `9,4,5,6,7,8` tersimpan & tercermin di sidebar.
- **Create** "Laporan" → muncul di sidebar lengkap dengan ikon; **Delete** → hilang.
- **Proteksi**: percobaan hapus menu sistem ("Produk") ditolak (tetap ada).

## Cara Pakai
1. Login sebagai Administrator → menu **Administrasi → Master Menu**.
2. **Tambah Menu**: isi Judul, Ikon (mis. `bi-bar-chart`), Controller/Action atau URL, pilih
   Menu Induk & batasan Role bila perlu.
3. **Urutkan**: seret item via ikon grip; perubahan tersimpan otomatis.
4. **Edit/Hapus**: lewat tombol pada tiap baris.
