# Tahap 3 — Database, EF Core & Migrasi

## Tujuan
Mendefinisikan model data, `DbContext`, relasi, migrasi, dan data awal (seed) di atas SQL Server.

## 1. Entitas Domain

Semua entitas bisnis berada di `ErpMetronic.Domain` dan mewarisi `BaseEntity`:

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
```

Entitas yang dibuat: `Category`, `UnitOfMeasure`, `Product`, `Customer`, `Supplier`, `Warehouse`.
Relasi utama: `Product` → `Category` (N:1) dan `Product` → `UnitOfMeasure` (N:1).

## 2. DbContext

`ErpMetronic.Infrastructure/Persistence/ApplicationDbContext.cs` mewarisi
`IdentityDbContext<ApplicationUser, ApplicationRole, string>` sehingga tabel Identity dan
master data berada dalam satu konteks:

```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    // ... dst

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Product>().HasIndex(x => x.Sku).IsUnique();
        builder.Entity<Product>()
            .HasOne(p => p.Category).WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);  // cegah hapus kategori yang dipakai
        // Nama tabel Identity diringkas: Users, Roles, UserRoles, ...
    }
}
```

Poin penting:
- **Indeks unik** pada kolom `Code`/`Sku` tiap master data.
- **`DeleteBehavior.Restrict`** pada relasi Produk → Kategori/Satuan agar tidak terjadi cascade
  delete yang tidak diinginkan.
- Nama tabel Identity diringkas (`Users`, `Roles`, dll.) agar skema lebih bersih.

## 3. Connection String

`src/ErpMetronic.Web/appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=ErpMetronicDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```
> Untuk LocalDB: `Server=(localdb)\\MSSQLLocalDB;Database=ErpMetronicDb;Trusted_Connection=True;...`

## 4. Registrasi DI

`ErpMetronic.Infrastructure/DependencyInjection.cs`:
```csharp
services.AddDbContext<ApplicationDbContext>(o =>
    o.UseSqlServer(connectionString,
        sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
```
`MigrationsAssembly` ditunjuk ke Infrastructure agar migrasi tersimpan di proyek tersebut.

## 5. Membuat & Menerapkan Migrasi

```bash
dotnet ef migrations add InitialCreate \
  --project src/ErpMetronic.Infrastructure \
  --startup-project src/ErpMetronic.Web \
  --output-dir Persistence/Migrations

dotnet ef database update \
  --project src/ErpMetronic.Infrastructure \
  --startup-project src/ErpMetronic.Web
```

## 6. Seeder Data Awal

`Persistence/DbSeeder.cs` dipanggil saat startup (`Program.cs`) dan akan:
1. `context.Database.MigrateAsync()` — menerapkan migrasi yang belum dijalankan.
2. Membuat role `Administrator`, `Manager`, `Staff`.
3. Membuat akun admin `admin@erpmetronic.local` / `Admin#12345`.
4. Mengisi contoh `UnitOfMeasure`, `Category`, dan `Product`.

```csharp
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}
```

## Hasil / Verifikasi
```bash
dotnet ef database update ...
# => Done. Database ErpMetronicDb dibuat dengan tabel Identity + master data.
```
Tabel dapat diperiksa via SSMS atau:
```bash
sqlcmd -S localhost -d ErpMetronicDb -Q "SELECT name FROM sys.tables ORDER BY name;"
```

## Catatan
- Migrasi berikutnya: ubah entitas → `dotnet ef migrations add <Nama>` → `database update`.
- Jangan edit file migrasi yang sudah diterapkan ke produksi; buat migrasi baru.

## Selanjutnya
➡️ [Tahap 4 — Autentikasi & Otorisasi](04-autentikasi-otorisasi.md)
