# Tahap 4 — Autentikasi & Otorisasi

## Tujuan
Mengamankan aplikasi dengan ASP.NET Core Identity: login/logout berbasis cookie dan kontrol
akses berbasis role.

## 1. Entitas Identity

`ErpMetronic.Infrastructure/Identity/`:
```csharp
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }
}
```

Konstanta role (`AppRoles.cs`): `Administrator`, `Manager`, `Staff`.

## 2. Konfigurasi Identity (DI)

`DependencyInjection.cs`:
```csharp
services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireDigit = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();
```

## 3. Cookie & Pipeline (`Program.cs`)

```csharp
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.LogoutPath = "/Account/Logout";
    o.AccessDeniedPath = "/Account/AccessDenied";
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.SlidingExpiration = true;
});
...
app.UseAuthentication();   // WAJIB sebelum UseAuthorization
app.UseAuthorization();
```
> Urutan `UseAuthentication()` lalu `UseAuthorization()` sangat penting.

## 4. AccountController

`Controllers/AccountController.cs` — bertanda `[Authorize]` di level kelas, dengan
`[AllowAnonymous]` pada aksi `Login`:

- **Login (GET/POST)**: memvalidasi kredensial via `SignInManager.PasswordSignInAsync` dengan
  `lockoutOnFailure: true`. Menolak user nonaktif (`IsActive == false`). Mendukung `ReturnUrl`
  lokal yang aman (`Url.IsLocalUrl`).
- **Logout (POST + AntiForgery)**: `SignInManager.SignOutAsync()`.
- **AccessDenied (GET)**: halaman ramah untuk akses ditolak.

## 5. Proteksi Halaman

- Semua controller fungsional ditandai `[Authorize]`.
- Controller administrasi ditandai `[Authorize(Roles = AppRoles.Administrator)]`
  (Users, Roles).
- Route default diarahkan ke `Dashboard` sehingga pengguna anonim otomatis diarahkan ke
  halaman Login.

## 6. Anti-Forgery
Semua form POST memakai `[ValidateAntiForgeryToken]` + token tersembunyi yang otomatis
di-render tag helper `<form asp-action=...>`.

## Hasil / Verifikasi
1. Akses `/Dashboard` tanpa login → diarahkan ke `/Account/Login`.
2. Login dengan `admin@erpmetronic.local` / `Admin#12345` → diarahkan ke Dashboard (HTTP 302 → `/`).
3. Pengguna non-admin membuka `/Users` → diarahkan ke `/Account/AccessDenied`.

Diuji otomatis pada repo ini: login menghasilkan **302 → `/`** dan seluruh halaman terproteksi
mengembalikan **200** setelah autentikasi.

## Selanjutnya
➡️ [Tahap 5 — Manajemen User & Role](05-manajemen-user-role.md)
