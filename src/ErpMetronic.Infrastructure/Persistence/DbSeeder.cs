using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpMetronic.Infrastructure.Persistence;

/// <summary>
/// Mengisi data awal: role default, akun administrator, dan beberapa contoh
/// master data agar aplikasi langsung bisa dicoba.
/// </summary>
public static class DbSeeder
{
    public const string DefaultAdminEmail = "admin@erpmetronic.local";
    public const string DefaultAdminPassword = "Admin#12345";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        await context.Database.MigrateAsync();

        // 1. Roles
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new ApplicationRole(role) { Description = $"Role {role}" });
        }

        // 2. Admin user
        var admin = await userManager.FindByEmailAsync(DefaultAdminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = DefaultAdminEmail,
                Email = DefaultAdminEmail,
                FullName = "System Administrator",
                EmailConfirmed = true,
                IsActive = true
            };
            await userManager.CreateAsync(admin, DefaultAdminPassword);
            await userManager.AddToRoleAsync(admin, AppRoles.Administrator);
        }

        // 3. Contoh master data
        if (!await context.UnitOfMeasures.AnyAsync())
        {
            context.UnitOfMeasures.AddRange(
                new UnitOfMeasure { Code = "PCS", Name = "Pieces" },
                new UnitOfMeasure { Code = "BOX", Name = "Box" },
                new UnitOfMeasure { Code = "KG", Name = "Kilogram" });
        }

        if (!await context.Categories.AnyAsync())
        {
            context.Categories.AddRange(
                new Category { Code = "ELC", Name = "Elektronik", Description = "Produk elektronik" },
                new Category { Code = "FOO", Name = "Makanan & Minuman", Description = "F&B" },
                new Category { Code = "STA", Name = "Alat Tulis", Description = "Stationery" });
        }

        await context.SaveChangesAsync();

        if (!await context.Products.AnyAsync())
        {
            var cat = await context.Categories.FirstAsync();
            var uom = await context.UnitOfMeasures.FirstAsync();
            context.Products.AddRange(
                new Product { Sku = "PRD-0001", Name = "Contoh Produk A", CategoryId = cat.Id, UnitOfMeasureId = uom.Id, PurchasePrice = 10000, SellingPrice = 15000, StockQuantity = 100, ReorderLevel = 10 },
                new Product { Sku = "PRD-0002", Name = "Contoh Produk B", CategoryId = cat.Id, UnitOfMeasureId = uom.Id, PurchasePrice = 25000, SellingPrice = 32000, StockQuantity = 50, ReorderLevel = 5 });
            await context.SaveChangesAsync();
        }

        // 4. Menu navigasi default (master menu)
        if (!await context.MenuItems.AnyAsync())
        {
            var dashboard = new MenuItem { Title = "Dashboard", Icon = "bi-grid-1x2", Controller = "Dashboard", Action = "Index", SortOrder = 1, IsSystem = true };
            var masterData = new MenuItem { Title = "Master Data", Icon = "bi-database", SortOrder = 2, IsSystem = true };
            var adminMenu = new MenuItem { Title = "Administrasi", Icon = "bi-gear", SortOrder = 3, RequiredRole = AppRoles.Administrator, IsSystem = true };
            context.MenuItems.AddRange(dashboard, masterData, adminMenu);
            await context.SaveChangesAsync();

            context.MenuItems.AddRange(
                new MenuItem { Title = "Produk", Icon = "bi-box", Controller = "Products", Action = "Index", ParentId = masterData.Id, SortOrder = 1, IsSystem = true },
                new MenuItem { Title = "Kategori", Icon = "bi-tags", Controller = "Categories", Action = "Index", ParentId = masterData.Id, SortOrder = 2, IsSystem = true },
                new MenuItem { Title = "Satuan", Icon = "bi-rulers", Controller = "UnitOfMeasures", Action = "Index", ParentId = masterData.Id, SortOrder = 3, IsSystem = true },
                new MenuItem { Title = "Pelanggan", Icon = "bi-people", Controller = "Customers", Action = "Index", ParentId = masterData.Id, SortOrder = 4, IsSystem = true },
                new MenuItem { Title = "Pemasok", Icon = "bi-truck", Controller = "Suppliers", Action = "Index", ParentId = masterData.Id, SortOrder = 5, IsSystem = true },
                new MenuItem { Title = "Gudang", Icon = "bi-building", Controller = "Warehouses", Action = "Index", ParentId = masterData.Id, SortOrder = 6, IsSystem = true },
                new MenuItem { Title = "Pengguna", Icon = "bi-person-gear", Controller = "Users", Action = "Index", ParentId = adminMenu.Id, SortOrder = 1, RequiredRole = AppRoles.Administrator, IsSystem = true },
                new MenuItem { Title = "Peran/Role", Icon = "bi-shield-lock", Controller = "Roles", Action = "Index", ParentId = adminMenu.Id, SortOrder = 2, RequiredRole = AppRoles.Administrator, IsSystem = true },
                new MenuItem { Title = "Master Menu", Icon = "bi-list-nested", Controller = "Menus", Action = "Index", ParentId = adminMenu.Id, SortOrder = 3, RequiredRole = AppRoles.Administrator, IsSystem = true });
            await context.SaveChangesAsync();
        }
    }
}
