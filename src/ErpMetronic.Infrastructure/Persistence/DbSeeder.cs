using ErpMetronic.Domain.Entities;
using ErpMetronic.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpMetronic.Infrastructure.Persistence;

/// <summary>
/// Mengisi data awal: akun administrator, master data, menu, divisi & posisi.
/// Hak administrator ditentukan oleh Posisi/Jabatan (bukan Identity Role).
/// </summary>
public static class DbSeeder
{
    public const string DefaultAdminEmail = "admin@erpmetronic.local";
    public const string DefaultAdminPassword = "Admin#12345";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        await context.Database.MigrateAsync();

        // 1. Akun administrator (hak admin diberikan lewat Posisi pada langkah berikutnya)
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
                new MenuItem { Title = "Master Menu", Icon = "bi-list-nested", Controller = "Menus", Action = "Index", ParentId = adminMenu.Id, SortOrder = 3, RequiredRole = AppRoles.Administrator, IsSystem = true });
            await context.SaveChangesAsync();
        }

        // 5. Gudang default (jika belum ada)
        if (!await context.Warehouses.AnyAsync())
        {
            context.Warehouses.AddRange(
                new Warehouse { Code = "WH-01", Name = "Gudang Utama", Location = "Kantor Pusat" },
                new Warehouse { Code = "WH-02", Name = "Gudang Cabang", Location = "Cabang" });
            await context.SaveChangesAsync();
        }

        // 6. Alokasi saldo stok awal: tempatkan StockQuantity tiap produk di gudang pertama
        if (!await context.ProductStocks.AnyAsync())
        {
            var firstWarehouse = await context.Warehouses.OrderBy(w => w.Id).FirstOrDefaultAsync();
            if (firstWarehouse is not null)
            {
                var products = await context.Products.ToListAsync();
                foreach (var p in products)
                    context.ProductStocks.Add(new ProductStock { ProductId = p.Id, WarehouseId = firstWarehouse.Id, Quantity = p.StockQuantity });
                await context.SaveChangesAsync();
            }
        }

        // 7. Menu Manajemen Stok (idempoten—ditambahkan bila belum ada)
        if (!await context.MenuItems.AnyAsync(m => m.Controller == "Stock"))
        {
            var maxOrder = await context.MenuItems.Where(m => m.ParentId == null).MaxAsync(m => (int?)m.SortOrder) ?? 0;
            var inventory = new MenuItem { Title = "Manajemen Stok", Icon = "bi-boxes", SortOrder = maxOrder + 1, IsSystem = true };
            context.MenuItems.Add(inventory);
            await context.SaveChangesAsync();

            context.MenuItems.AddRange(
                new MenuItem { Title = "Stok Masuk", Icon = "bi-box-arrow-in-down", Controller = "Stock", Action = "In", ParentId = inventory.Id, SortOrder = 1, IsSystem = true },
                new MenuItem { Title = "Stok Keluar", Icon = "bi-box-arrow-up", Controller = "Stock", Action = "Out", ParentId = inventory.Id, SortOrder = 2, IsSystem = true },
                new MenuItem { Title = "Transfer Stok", Icon = "bi-arrow-left-right", Controller = "Stock", Action = "Transfer", ParentId = inventory.Id, SortOrder = 3, IsSystem = true },
                new MenuItem { Title = "Penyesuaian", Icon = "bi-sliders", Controller = "Stock", Action = "Adjust", ParentId = inventory.Id, SortOrder = 4, IsSystem = true },
                new MenuItem { Title = "Riwayat Stok", Icon = "bi-clock-history", Controller = "Stock", Action = "Movements", ParentId = inventory.Id, SortOrder = 5, IsSystem = true },
                new MenuItem { Title = "Saldo Stok", Icon = "bi-clipboard-data", Controller = "Stock", Action = "Balances", ParentId = inventory.Id, SortOrder = 6, IsSystem = true });
            await context.SaveChangesAsync();
        }

        // 8. Divisi & Posisi default (jika belum ada)
        if (!await context.Divisions.AnyAsync())
        {
            context.Divisions.AddRange(
                new Division { Code = "MGT", Name = "Manajemen", Description = "Direksi & manajemen" },
                new Division { Code = "SAL", Name = "Penjualan", Description = "Sales & marketing" },
                new Division { Code = "WHS", Name = "Gudang", Description = "Operasional gudang & stok" },
                new Division { Code = "FIN", Name = "Keuangan", Description = "Finance & akuntansi" });
            await context.SaveChangesAsync();
        }

        if (!await context.Positions.AnyAsync())
        {
            context.Positions.AddRange(
                new Position { Code = "ADM", Name = "Administrator", Description = "Akses penuh sistem", IsAdministrator = true },
                new Position { Code = "MGR", Name = "Manajer" },
                new Position { Code = "SPV", Name = "Supervisor" },
                new Position { Code = "STF", Name = "Staff" });
            await context.SaveChangesAsync();
        }

        // Pastikan ada posisi administrator (untuk DB yang sudah terisi sebelumnya)
        if (!await context.Positions.AnyAsync(p => p.IsAdministrator))
        {
            context.Positions.Add(new Position { Code = "ADM", Name = "Administrator", Description = "Akses penuh sistem", IsAdministrator = true });
            await context.SaveChangesAsync();
        }

        // Tetapkan posisi administrator pada akun admin default bila belum punya posisi
        if (admin is not null && admin.PositionId is null)
        {
            var adminPosition = await context.Positions.FirstAsync(p => p.IsAdministrator);
            admin.PositionId = adminPosition.Id;
            await userManager.UpdateAsync(admin);
        }

        // Hapus menu lama "Peran/Role" — manajemen role sudah ditiadakan
        var roleMenu = await context.MenuItems.FirstOrDefaultAsync(m => m.Controller == "Roles");
        if (roleMenu is not null)
        {
            context.MenuItems.Remove(roleMenu);
            await context.SaveChangesAsync();
        }

        // 9. Menu admin: Divisi & Posisi (idempoten)
        if (!await context.MenuItems.AnyAsync(m => m.Controller == "Divisions"))
        {
            var adminGroup = await context.MenuItems.FirstOrDefaultAsync(m => m.Title == "Administrasi" && m.ParentId == null);
            if (adminGroup is not null)
            {
                var maxChild = await context.MenuItems.Where(m => m.ParentId == adminGroup.Id).MaxAsync(m => (int?)m.SortOrder) ?? 0;
                context.MenuItems.AddRange(
                    new MenuItem { Title = "Divisi", Icon = "bi-diagram-3", Controller = "Divisions", Action = "Index", ParentId = adminGroup.Id, SortOrder = maxChild + 1, RequiredRole = AppRoles.Administrator, IsSystem = true },
                    new MenuItem { Title = "Posisi", Icon = "bi-person-badge", Controller = "Positions", Action = "Index", ParentId = adminGroup.Id, SortOrder = maxChild + 2, RequiredRole = AppRoles.Administrator, IsSystem = true });
                await context.SaveChangesAsync();
            }
        }

        // 10. Mata uang (multi-currency) + kurs awal
        if (!await context.Currencies.AnyAsync())
        {
            var idr = new Currency { Code = "IDR", Name = "Rupiah", Symbol = "Rp", DecimalPlaces = 0, IsBaseCurrency = true };
            var usd = new Currency { Code = "USD", Name = "US Dollar", Symbol = "$", DecimalPlaces = 2 };
            var eur = new Currency { Code = "EUR", Name = "Euro", Symbol = "€", DecimalPlaces = 2 };
            context.Currencies.AddRange(idr, usd, eur);
            await context.SaveChangesAsync();

            var effective = new DateTime(2026, 1, 1);
            context.ExchangeRates.AddRange(
                new ExchangeRate { CurrencyId = usd.Id, Rate = 16000m, EffectiveDate = effective },
                new ExchangeRate { CurrencyId = eur.Id, Rate = 17500m, EffectiveDate = effective });
            await context.SaveChangesAsync();
        }

        // Tetapkan mata uang dasar pada produk yang belum punya mata uang
        var baseCurrency = await context.Currencies.FirstOrDefaultAsync(c => c.IsBaseCurrency);
        if (baseCurrency is not null)
        {
            var productsNoCurrency = await context.Products.Where(p => p.CurrencyId == null).ToListAsync();
            if (productsNoCurrency.Count > 0)
            {
                foreach (var p in productsNoCurrency) p.CurrencyId = baseCurrency.Id;
                await context.SaveChangesAsync();
            }
        }

        // 11. Menu Keuangan (idempoten)
        if (!await context.MenuItems.AnyAsync(m => m.Controller == "Currencies"))
        {
            var maxOrder = await context.MenuItems.Where(m => m.ParentId == null).MaxAsync(m => (int?)m.SortOrder) ?? 0;
            var finance = new MenuItem { Title = "Keuangan", Icon = "bi-cash-coin", SortOrder = maxOrder + 1, RequiredRole = AppRoles.Administrator, IsSystem = true };
            context.MenuItems.Add(finance);
            await context.SaveChangesAsync();

            context.MenuItems.AddRange(
                new MenuItem { Title = "Mata Uang", Icon = "bi-currency-exchange", Controller = "Currencies", Action = "Index", ParentId = finance.Id, SortOrder = 1, RequiredRole = AppRoles.Administrator, IsSystem = true },
                new MenuItem { Title = "Kurs / Exchange Rate", Icon = "bi-graph-up-arrow", Controller = "ExchangeRates", Action = "Index", ParentId = finance.Id, SortOrder = 2, RequiredRole = AppRoles.Administrator, IsSystem = true });
            await context.SaveChangesAsync();
        }
    }
}
