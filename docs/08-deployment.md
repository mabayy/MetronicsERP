# Tahap 8 — Deployment & Produksi

## Tujuan
Panduan menyiapkan aplikasi untuk lingkungan produksi.

## 1. Konfigurasi per-Lingkungan

Gunakan `appsettings.Production.json` (jangan commit rahasia ke repo):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=PROD-SQL;Database=ErpMetronicDb;User Id=erp_app;Password=__SECRET__;TrustServerCertificate=True"
  }
}
```
Lebih aman: simpan connection string di **Environment Variables** atau **User Secrets** /
**Azure Key Vault**:
```bash
setx ConnectionStrings__DefaultConnection "Server=...;Database=...;..."
```

## 2. Build Rilis

```bash
dotnet publish src/ErpMetronic.Web -c Release -o ./publish
```

## 3. Migrasi di Produksi

Pilih salah satu strategi:
- **Otomatis saat startup** (sudah aktif via `DbSeeder` → `MigrateAsync`). Sederhana, cocok
  untuk tim kecil.
- **Manual / script** (disarankan untuk produksi terkontrol):
  ```bash
  dotnet ef migrations script --idempotent \
    --project src/ErpMetronic.Infrastructure \
    --startup-project src/ErpMetronic.Web -o migrate.sql
  ```
  Jalankan `migrate.sql` melalui DBA. Untuk produksi, pertimbangkan menonaktifkan seeding
  otomatis atau membatasi hanya pembuatan role/admin.

## 4. Hosting

### a. IIS (Windows)
1. Pasang **.NET 8 Hosting Bundle**.
2. Buat situs/aplikasi IIS yang menunjuk ke folder `publish`.
3. App Pool: **No Managed Code** (ASP.NET Core berjalan out-of-process/in-process via module).
4. Pastikan akun App Pool punya akses ke SQL Server.

### b. Docker (lintas platform)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/ErpMetronic.Web -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ErpMetronic.Web.dll"]
```

## 5. Checklist Keamanan Produksi
- [ ] HTTPS dipaksakan (`UseHttpsRedirection` + sertifikat valid) & `UseHsts` aktif (non-Dev).
- [ ] Ganti password admin default segera setelah deploy.
- [ ] Connection string & secret tidak ada di source control.
- [ ] Logging diarahkan ke sink produksi (file/Seq/Application Insights).
- [ ] Backup database terjadwal.
- [ ] Batasi akun SQL ke hak minimum (bukan `sa`).

## 6. Observability (disarankan)
- Tambahkan health check: `builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>();`
  lalu `app.MapHealthChecks("/health");`
- Integrasikan Serilog untuk structured logging.

## Selanjutnya
➡️ [Tahap 9 — Roadmap Pengembangan Lanjutan](09-roadmap.md)
