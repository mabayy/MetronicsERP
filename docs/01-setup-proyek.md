# Tahap 1 — Setup Proyek & Struktur Solusi

## Tujuan
Membuat solusi .NET 8 berlapis (layered) dengan pemisahan tanggung jawab yang jelas.

## Arsitektur Target

```
ErpMetronic.sln
└── src/
    ├── ErpMetronic.Domain          # Entitas bisnis murni, tanpa dependensi framework
    ├── ErpMetronic.Infrastructure  # EF Core, Identity, akses data, seeder
    └── ErpMetronic.Web             # ASP.NET Core MVC (Controllers, Views, wwwroot)
```

Aturan ketergantungan (dependency rule): **Web → Infrastructure → Domain**.
Domain tidak boleh bergantung pada proyek lain.

## Langkah

### 1. Buat solusi & proyek
```bash
dotnet new sln -n ErpMetronic

dotnet new classlib -n ErpMetronic.Domain         -o src/ErpMetronic.Domain         -f net8.0
dotnet new classlib -n ErpMetronic.Infrastructure -o src/ErpMetronic.Infrastructure -f net8.0
dotnet new mvc      -n ErpMetronic.Web            -o src/ErpMetronic.Web            -f net8.0
```

### 2. Tambahkan ke solusi
```bash
dotnet sln add src/ErpMetronic.Domain/ErpMetronic.Domain.csproj
dotnet sln add src/ErpMetronic.Infrastructure/ErpMetronic.Infrastructure.csproj
dotnet sln add src/ErpMetronic.Web/ErpMetronic.Web.csproj
```

### 3. Hubungkan referensi antar-proyek
```bash
dotnet add src/ErpMetronic.Infrastructure reference src/ErpMetronic.Domain
dotnet add src/ErpMetronic.Web            reference src/ErpMetronic.Infrastructure src/ErpMetronic.Domain
```

### 4. Tambahkan paket NuGet
```bash
# Infrastructure (EF Core + Identity)
dotnet add src/ErpMetronic.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer       --version 8.0.11
dotnet add src/ErpMetronic.Infrastructure package Microsoft.EntityFrameworkCore.Tools           --version 8.0.11
dotnet add src/ErpMetronic.Infrastructure package Microsoft.EntityFrameworkCore.Design          --version 8.0.11
dotnet add src/ErpMetronic.Infrastructure package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.0.11

# Web (untuk perintah migrasi dari startup project)
dotnet add src/ErpMetronic.Web package Microsoft.EntityFrameworkCore.Design --version 8.0.11
```

### 5. Penting: FrameworkReference di Infrastructure
Karena `AddIdentity()` berada di shared framework ASP.NET Core, proyek class library
**Infrastructure** harus mereferensikan framework tersebut. Tambahkan pada
`ErpMetronic.Infrastructure.csproj`:

```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

Tanpa baris ini, build gagal dengan error `CS1061: 'IServiceCollection' does not contain a definition for 'AddIdentity'`.

## Hasil / Verifikasi
```bash
dotnet build ErpMetronic.sln
# => Build succeeded. 0 Error(s)
```

## Catatan
- Hapus file `Class1.cs` bawaan template `classlib`.
- Struktur folder dalam tiap proyek diperkenalkan bertahap (`Common/`, `Entities/`,
  `Persistence/`, `Identity/`, `ViewModels/`, dst.) pada tahap-tahap berikutnya.

## Selanjutnya
➡️ [Tahap 2 — Integrasi Tema Metronic 8](02-integrasi-metronic.md)
