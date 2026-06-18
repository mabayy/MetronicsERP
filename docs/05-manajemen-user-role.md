# Tahap 5 — Manajemen User & Role

## Tujuan
Menyediakan antarmuka administrasi untuk mengelola pengguna dan peran (role), termasuk
penetapan role ke pengguna. Modul ini hanya dapat diakses oleh `Administrator`.

## Komponen

| Berkas | Peran |
|--------|-------|
| `Controllers/UsersController.cs` | CRUD pengguna + penetapan role |
| `Controllers/RolesController.cs` | CRUD role |
| `ViewModels/UserViewModels.cs` | `UserListItemViewModel`, `CreateUserViewModel`, `EditUserViewModel`, `RoleViewModel` |
| `Views/Users/*`, `Views/Roles/*` | Tampilan Index/Create/Edit |

Keduanya ditandai `[Authorize(Roles = AppRoles.Administrator)]`.

## Manajemen Pengguna

Menggunakan `UserManager<ApplicationUser>` dan `RoleManager<ApplicationRole>`:

- **Index** — menampilkan daftar pengguna beserta role-nya (via `GetRolesAsync`).
- **Create** — membuat user (`CreateAsync`), lalu `AddToRolesAsync` untuk role terpilih.
  Validasi password mengikuti kebijakan Identity; error ditampilkan ke `ModelState`.
- **Edit** — memperbarui profil + sinkronisasi role:
  ```csharp
  var current = await _userManager.GetRolesAsync(user);
  await _userManager.RemoveFromRolesAsync(user, current.Except(model.SelectedRoles));
  await _userManager.AddToRolesAsync(user, model.SelectedRoles.Except(current));
  ```
- **Delete** — menghapus user; **akun admin default dilindungi** dari penghapusan.

Pemilihan role di form memakai checkbox `name="SelectedRoles"` (di-bind ke `List<string>`).

## Manajemen Role

- **Index** — daftar role + jumlah pengguna (`GetUsersInRoleAsync(...).Count`).
- **Create/Edit** — membuat/mengubah role beserta deskripsi; mencegah duplikasi nama.
- **Delete** — role bawaan (`Administrator`, `Manager`, `Staff`) dilindungi dari penghapusan.

## Keamanan & Validasi
- Anti-forgery token pada semua POST.
- Proteksi akun & role sistem agar tidak terhapus tidak sengaja.
- Email unik dipaksakan oleh konfigurasi Identity.

## Hasil / Verifikasi
- `/Users` dan `/Roles` mengembalikan 200 bagi Administrator.
- Membuat pengguna baru dengan role → muncul di daftar dengan badge role.
- Pengguna non-admin tidak dapat mengakses (diarahkan ke AccessDenied).

## Selanjutnya
➡️ [Tahap 6 — Dashboard](06-dashboard.md)
