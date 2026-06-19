using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ErpMetronic.Infrastructure.Persistence;

/// <summary>
/// DbContext utama aplikasi. Menggabungkan tabel Identity (user/role) dengan
/// tabel master data ERP.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<UnitOfMeasure> UnitOfMeasures => Set<UnitOfMeasure>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<ProductStock> ProductStocks => Set<ProductStock>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Division> Divisions => Set<Division>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<MenuItemDivision> MenuItemDivisions => Set<MenuItemDivision>();
    public DbSet<MenuItemPosition> MenuItemPositions => Set<MenuItemPosition>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Category>().HasIndex(x => x.Code).IsUnique();
        builder.Entity<UnitOfMeasure>().HasIndex(x => x.Code).IsUnique();
        builder.Entity<Product>().HasIndex(x => x.Sku).IsUnique();
        builder.Entity<Customer>().HasIndex(x => x.Code).IsUnique();
        builder.Entity<Supplier>().HasIndex(x => x.Code).IsUnique();
        builder.Entity<Warehouse>().HasIndex(x => x.Code).IsUnique();

        builder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Product>()
            .HasOne(p => p.UnitOfMeasure)
            .WithMany(u => u.Products)
            .HasForeignKey(p => p.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Product>()
            .HasOne(p => p.Currency)
            .WithMany()
            .HasForeignKey(p => p.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Currency>().HasIndex(c => c.Code).IsUnique();
        // Hanya satu mata uang dasar (indeks unik terfilter di SQL Server).
        builder.Entity<Currency>()
            .HasIndex(c => c.IsBaseCurrency)
            .IsUnique()
            .HasFilter("[IsBaseCurrency] = 1");

        builder.Entity<ExchangeRate>(e =>
        {
            e.HasIndex(x => new { x.CurrencyId, x.EffectiveDate }).IsUnique();
            e.HasOne(x => x.Currency).WithMany(c => c.ExchangeRates)
                .HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MenuItem>()
            .HasOne(m => m.Parent)
            .WithMany(m => m.Children)
            .HasForeignKey(m => m.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductStock>(e =>
        {
            e.HasIndex(x => new { x.ProductId, x.WarehouseId }).IsUnique();
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StockMovement>(e =>
        {
            e.HasIndex(x => x.ReferenceNumber);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.DestinationWarehouse).WithMany().HasForeignKey(x => x.DestinationWarehouseId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Division>().HasIndex(x => x.Code).IsUnique();
        builder.Entity<Position>().HasIndex(x => x.Code).IsUnique();

        // Pengguna → Divisi & Posisi (boleh kosong)
        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Division).WithMany().HasForeignKey(u => u.DivisionId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Position).WithMany().HasForeignKey(u => u.PositionId).OnDelete(DeleteBehavior.SetNull);

        // Hak akses menu per divisi/posisi (join). Hapus menu → grant ikut terhapus;
        // divisi/posisi dibatasi (Restrict) agar tidak terhapus saat masih dipakai grant.
        builder.Entity<MenuItemDivision>(e =>
        {
            e.HasKey(x => new { x.MenuItemId, x.DivisionId });
            e.HasOne(x => x.MenuItem).WithMany(m => m.AllowedDivisions).HasForeignKey(x => x.MenuItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Division).WithMany().HasForeignKey(x => x.DivisionId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<MenuItemPosition>(e =>
        {
            e.HasKey(x => new { x.MenuItemId, x.PositionId });
            e.HasOne(x => x.MenuItem).WithMany(m => m.AllowedPositions).HasForeignKey(x => x.MenuItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Position).WithMany().HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Restrict);
        });

        // Ringkaskan nama tabel Identity agar lebih bersih.
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("RoleClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("UserTokens");
    }
}
