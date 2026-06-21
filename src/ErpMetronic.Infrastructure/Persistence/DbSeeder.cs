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
                new Product { Sku = "PRD-0001", Name = "Contoh Produk A", CategoryId = cat.Id, UnitOfMeasureId = uom.Id, PurchasePrice = 10000, SellingPrice = 15000, AverageCost = 10000, StockQuantity = 100, ReorderLevel = 10 },
                new Product { Sku = "PRD-0002", Name = "Contoh Produk B", CategoryId = cat.Id, UnitOfMeasureId = uom.Id, PurchasePrice = 25000, SellingPrice = 32000, AverageCost = 25000, StockQuantity = 50, ReorderLevel = 5 });
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

        // 5b. Contoh Pemasok & Pelanggan (agar PO/penjualan langsung bisa dicoba)
        if (!await context.Suppliers.AnyAsync())
        {
            context.Suppliers.AddRange(
                new Supplier { Code = "SUP-001", Name = "PT Sumber Makmur", ContactPerson = "Andi", Phone = "021-555100", Email = "sales@sumbermakmur.co.id" },
                new Supplier { Code = "SUP-002", Name = "CV Mitra Sentosa", ContactPerson = "Budi", Phone = "021-555200" });
            await context.SaveChangesAsync();
        }
        if (!await context.Customers.AnyAsync())
        {
            context.Customers.AddRange(
                new Customer { Code = "CUST-001", Name = "Toko Sejahtera", Phone = "021-777100", City = "Jakarta" },
                new Customer { Code = "CUST-002", Name = "PT Ritel Nusantara", Phone = "021-777200", City = "Bandung" });
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

        // 12. Menu inventory lanjutan: penerimaan, pengeluaran, kartu stok, nilai persediaan (idempoten)
        if (!await context.MenuItems.AnyAsync(m => m.Controller == "GoodsReceipts"))
        {
            var inventoryGroup = await context.MenuItems.FirstOrDefaultAsync(m => m.Title == "Manajemen Stok" && m.ParentId == null);
            if (inventoryGroup is not null)
            {
                var maxChild = await context.MenuItems.Where(m => m.ParentId == inventoryGroup.Id).MaxAsync(m => (int?)m.SortOrder) ?? 0;
                context.MenuItems.AddRange(
                    new MenuItem { Title = "Penerimaan Barang", Icon = "bi-arrow-down-square", Controller = "GoodsReceipts", Action = "Index", ParentId = inventoryGroup.Id, SortOrder = maxChild + 1, IsSystem = true },
                    new MenuItem { Title = "Pengeluaran Barang", Icon = "bi-arrow-up-square", Controller = "DeliveryOrders", Action = "Index", ParentId = inventoryGroup.Id, SortOrder = maxChild + 2, IsSystem = true },
                    new MenuItem { Title = "Kartu Stok", Icon = "bi-card-list", Controller = "Stock", Action = "Card", ParentId = inventoryGroup.Id, SortOrder = maxChild + 3, IsSystem = true },
                    new MenuItem { Title = "Nilai Persediaan", Icon = "bi-cash-stack", Controller = "Stock", Action = "Valuation", ParentId = inventoryGroup.Id, SortOrder = maxChild + 4, IsSystem = true });
                await context.SaveChangesAsync();
            }
        }

        // 12b. Menu Pembelian → Purchase Order (idempoten)
        if (!await context.MenuItems.AnyAsync(m => m.Controller == "PurchaseOrders"))
        {
            var maxOrder = await context.MenuItems.Where(m => m.ParentId == null).MaxAsync(m => (int?)m.SortOrder) ?? 0;
            var purchasing = new MenuItem { Title = "Pembelian", Icon = "bi-cart", SortOrder = maxOrder + 1, IsSystem = true };
            context.MenuItems.Add(purchasing);
            await context.SaveChangesAsync();

            context.MenuItems.Add(new MenuItem { Title = "Purchase Order", Icon = "bi-cart-plus", Controller = "PurchaseOrders", Action = "Index", ParentId = purchasing.Id, SortOrder = 1, IsSystem = true });
            await context.SaveChangesAsync();
        }

        // 12b-2. Menu Faktur Pembelian (idempoten)
        if (!await context.MenuItems.AnyAsync(m => m.Controller == "PurchaseInvoices"))
        {
            var purchasingGroup = await context.MenuItems.FirstOrDefaultAsync(m => m.Title == "Pembelian" && m.ParentId == null);
            if (purchasingGroup is not null)
            {
                var maxChild = await context.MenuItems.Where(m => m.ParentId == purchasingGroup.Id).MaxAsync(m => (int?)m.SortOrder) ?? 0;
                context.MenuItems.Add(new MenuItem { Title = "Faktur Pembelian", Icon = "bi-receipt", Controller = "PurchaseInvoices", Action = "Index", ParentId = purchasingGroup.Id, SortOrder = maxChild + 1, IsSystem = true });
                await context.SaveChangesAsync();
            }
        }

        // 12b-3. Menu Penjualan → Sales Order & Faktur Penjualan (idempoten)
        if (!await context.MenuItems.AnyAsync(m => m.Controller == "SalesOrders"))
        {
            var maxOrder = await context.MenuItems.Where(m => m.ParentId == null).MaxAsync(m => (int?)m.SortOrder) ?? 0;
            var sales = new MenuItem { Title = "Penjualan", Icon = "bi-shop", SortOrder = maxOrder + 1, IsSystem = true };
            context.MenuItems.Add(sales);
            await context.SaveChangesAsync();

            context.MenuItems.AddRange(
                new MenuItem { Title = "Sales Order", Icon = "bi-bag-plus", Controller = "SalesOrders", Action = "Index", ParentId = sales.Id, SortOrder = 1, IsSystem = true },
                new MenuItem { Title = "Faktur Penjualan", Icon = "bi-receipt-cutoff", Controller = "SalesInvoices", Action = "Index", ParentId = sales.Id, SortOrder = 2, IsSystem = true });
            await context.SaveChangesAsync();
        }

        // 12b-4. Menu Pengadaan: Purchase Requisition & RFQ (di grup Pembelian, idempoten)
        if (!await context.MenuItems.AnyAsync(m => m.Controller == "PurchaseRequisitions"))
        {
            var purchasingGroup = await context.MenuItems.FirstOrDefaultAsync(m => m.Title == "Pembelian" && m.ParentId == null);
            if (purchasingGroup is not null)
            {
                var maxChild = await context.MenuItems.Where(m => m.ParentId == purchasingGroup.Id).MaxAsync(m => (int?)m.SortOrder) ?? 0;
                context.MenuItems.AddRange(
                    new MenuItem { Title = "Purchase Requisition", Icon = "bi-clipboard-plus", Controller = "PurchaseRequisitions", Action = "Index", ParentId = purchasingGroup.Id, SortOrder = maxChild + 1, IsSystem = true },
                    new MenuItem { Title = "Request for Quotation", Icon = "bi-chat-left-text", Controller = "RequestForQuotations", Action = "Index", ParentId = purchasingGroup.Id, SortOrder = maxChild + 2, IsSystem = true });
                await context.SaveChangesAsync();
            }
        }

        // 12b-4b. Menu Retur & Aging (idempoten)
        if (!await context.MenuItems.AnyAsync(m => m.Controller == "SalesReturns"))
        {
            var salesGroup = await context.MenuItems.FirstOrDefaultAsync(m => m.Title == "Penjualan" && m.ParentId == null);
            if (salesGroup is not null)
            {
                var mx = await context.MenuItems.Where(m => m.ParentId == salesGroup.Id).MaxAsync(m => (int?)m.SortOrder) ?? 0;
                context.MenuItems.AddRange(
                    new MenuItem { Title = "Retur Penjualan", Icon = "bi-arrow-return-left", Controller = "SalesReturns", Action = "Index", ParentId = salesGroup.Id, SortOrder = mx + 1, IsSystem = true },
                    new MenuItem { Title = "Umur Piutang", Icon = "bi-hourglass-split", Controller = "SalesInvoices", Action = "Aging", ParentId = salesGroup.Id, SortOrder = mx + 2, IsSystem = true });
            }
            var purchasingGroup = await context.MenuItems.FirstOrDefaultAsync(m => m.Title == "Pembelian" && m.ParentId == null);
            if (purchasingGroup is not null)
            {
                var mx = await context.MenuItems.Where(m => m.ParentId == purchasingGroup.Id).MaxAsync(m => (int?)m.SortOrder) ?? 0;
                context.MenuItems.AddRange(
                    new MenuItem { Title = "Retur Pembelian", Icon = "bi-arrow-return-right", Controller = "PurchaseReturns", Action = "Index", ParentId = purchasingGroup.Id, SortOrder = mx + 1, IsSystem = true },
                    new MenuItem { Title = "Umur Hutang", Icon = "bi-hourglass-split", Controller = "PurchaseInvoices", Action = "Aging", ParentId = purchasingGroup.Id, SortOrder = mx + 2, IsSystem = true });
            }
            await context.SaveChangesAsync();
        }

        // 12b-5. Bagan Akun (Chart of Accounts) bawaan (idempoten per kode)
        foreach (var (code, name, type) in Domain.Constants.AccountCodes.Defaults)
        {
            if (!await context.ChartOfAccounts.AnyAsync(a => a.Code == code))
                context.ChartOfAccounts.Add(new ChartOfAccount { Code = code, Name = name, Type = type, IsSystem = true });
        }
        await context.SaveChangesAsync();

        // 12b-6. Menu Keuangan: Bagan Akun, Jurnal, Buku Besar, Neraca Saldo (idempoten)
        if (!await context.MenuItems.AnyAsync(m => m.Controller == "ChartOfAccounts"))
        {
            var financeGroup = await context.MenuItems.FirstOrDefaultAsync(m => m.Title == "Keuangan" && m.ParentId == null);
            if (financeGroup is not null)
            {
                var maxChild = await context.MenuItems.Where(m => m.ParentId == financeGroup.Id).MaxAsync(m => (int?)m.SortOrder) ?? 0;
                context.MenuItems.AddRange(
                    new MenuItem { Title = "Bagan Akun", Icon = "bi-list-columns", Controller = "ChartOfAccounts", Action = "Index", ParentId = financeGroup.Id, SortOrder = maxChild + 1, RequiredRole = AppRoles.Administrator, IsSystem = true },
                    new MenuItem { Title = "Jurnal", Icon = "bi-journal-text", Controller = "JournalEntries", Action = "Index", ParentId = financeGroup.Id, SortOrder = maxChild + 2, RequiredRole = AppRoles.Administrator, IsSystem = true },
                    new MenuItem { Title = "Buku Besar", Icon = "bi-book", Controller = "FinanceReports", Action = "GeneralLedger", ParentId = financeGroup.Id, SortOrder = maxChild + 3, RequiredRole = AppRoles.Administrator, IsSystem = true },
                    new MenuItem { Title = "Neraca Saldo", Icon = "bi-clipboard-check", Controller = "FinanceReports", Action = "TrialBalance", ParentId = financeGroup.Id, SortOrder = maxChild + 4, RequiredRole = AppRoles.Administrator, IsSystem = true });
                await context.SaveChangesAsync();
            }
        }

        // 12b-7. Master Pajak bawaan: PPN (VAT) Masukan/Keluaran & PPh 23 (idempoten per kode)
        var defaultTaxes = new[]
        {
            ("PPN-OUT", "PPN Keluaran 11%", 11m, Domain.Enums.TaxKind.ValueAdded, Domain.Enums.TaxApplicability.Sales, Domain.Constants.AccountCodes.OutputVat),
            ("PPN-IN", "PPN Masukan 11%", 11m, Domain.Enums.TaxKind.ValueAdded, Domain.Enums.TaxApplicability.Purchase, Domain.Constants.AccountCodes.InputVat),
            ("PPH23", "PPh Pasal 23 (2%)", 2m, Domain.Enums.TaxKind.Withholding, Domain.Enums.TaxApplicability.Both, Domain.Constants.AccountCodes.WhtPayable)
        };
        foreach (var (code, name, rate, kind, applies, account) in defaultTaxes)
        {
            if (!await context.Taxes.AnyAsync(t => t.Code == code))
                context.Taxes.Add(new Tax { Code = code, Name = name, Rate = rate, Kind = kind, AppliesTo = applies, AccountCode = account, IsActive = true, IsSystem = true });
        }
        await context.SaveChangesAsync();

        // 12b-8. Menu Keuangan → Pajak (idempoten)
        if (!await context.MenuItems.AnyAsync(m => m.Controller == "Taxes"))
        {
            var financeGroup = await context.MenuItems.FirstOrDefaultAsync(m => m.Title == "Keuangan" && m.ParentId == null);
            if (financeGroup is not null)
            {
                var maxChild = await context.MenuItems.Where(m => m.ParentId == financeGroup.Id).MaxAsync(m => (int?)m.SortOrder) ?? 0;
                context.MenuItems.Add(new MenuItem { Title = "Pajak", Icon = "bi-percent", Controller = "Taxes", Action = "Index", ParentId = financeGroup.Id, SortOrder = maxChild + 1, RequiredRole = AppRoles.Administrator, IsSystem = true });
                await context.SaveChangesAsync();
            }
        }

        // 12b-9. Menu Keuangan → Laba Rugi & Neraca (idempoten)
        if (!await context.MenuItems.AnyAsync(m => m.Controller == "FinanceReports" && m.Action == "BalanceSheet"))
        {
            var financeGroup = await context.MenuItems.FirstOrDefaultAsync(m => m.Title == "Keuangan" && m.ParentId == null);
            if (financeGroup is not null)
            {
                var maxChild = await context.MenuItems.Where(m => m.ParentId == financeGroup.Id).MaxAsync(m => (int?)m.SortOrder) ?? 0;
                context.MenuItems.AddRange(
                    new MenuItem { Title = "Laba Rugi", Icon = "bi-graph-up-arrow", Controller = "FinanceReports", Action = "IncomeStatement", ParentId = financeGroup.Id, SortOrder = maxChild + 1, RequiredRole = AppRoles.Administrator, IsSystem = true },
                    new MenuItem { Title = "Neraca", Icon = "bi-bank", Controller = "FinanceReports", Action = "BalanceSheet", ParentId = financeGroup.Id, SortOrder = maxChild + 2, RequiredRole = AppRoles.Administrator, IsSystem = true });
                await context.SaveChangesAsync();
            }
        }

        // 12b-12. Menu Keuangan → Arus Kas (idempoten)
        if (!await context.MenuItems.AnyAsync(m => m.Controller == "FinanceReports" && m.Action == "CashFlow"))
        {
            var financeGroup = await context.MenuItems.FirstOrDefaultAsync(m => m.Title == "Keuangan" && m.ParentId == null);
            if (financeGroup is not null)
            {
                var maxChild = await context.MenuItems.Where(m => m.ParentId == financeGroup.Id).MaxAsync(m => (int?)m.SortOrder) ?? 0;
                context.MenuItems.Add(new MenuItem { Title = "Arus Kas", Icon = "bi-cash-stack", Controller = "FinanceReports", Action = "CashFlow", ParentId = financeGroup.Id, SortOrder = maxChild + 1, RequiredRole = AppRoles.Administrator, IsSystem = true });
                await context.SaveChangesAsync();
            }
        }

        // 12b-10. Termin pembayaran bawaan (idempoten per kode)
        var defaultTerms = new[]
        {
            ("CASH", "Tunai", 0), ("NET7", "Net 7 Hari", 7), ("NET14", "Net 14 Hari", 14),
            ("NET30", "Net 30 Hari", 30), ("NET60", "Net 60 Hari", 60)
        };
        foreach (var (code, name, days) in defaultTerms)
        {
            if (!await context.PaymentTerms.AnyAsync(t => t.Code == code))
                context.PaymentTerms.Add(new PaymentTerm { Code = code, Name = name, NetDays = days, IsActive = true, IsSystem = true });
        }
        await context.SaveChangesAsync();

        // 12b-11. Menu Master Data → Termin Pembayaran (idempoten)
        if (!await context.MenuItems.AnyAsync(m => m.Controller == "PaymentTerms"))
        {
            var masterGroup = await context.MenuItems.FirstOrDefaultAsync(m => m.Title == "Master Data" && m.ParentId == null);
            if (masterGroup is not null)
            {
                var maxChild = await context.MenuItems.Where(m => m.ParentId == masterGroup.Id).MaxAsync(m => (int?)m.SortOrder) ?? 0;
                context.MenuItems.Add(new MenuItem { Title = "Termin Pembayaran", Icon = "bi-calendar-check", Controller = "PaymentTerms", Action = "Index", ParentId = masterGroup.Id, SortOrder = maxChild + 1, RequiredRole = AppRoles.Administrator, IsSystem = true });
                await context.SaveChangesAsync();
            }
        }

        // 12b-13. Akun Kas/Bank bawaan (idempoten per kode)
        var defaultCashBank = new[]
        {
            ("KAS", "Kas", Domain.Entities.CashBankAccountKind.Cash, Domain.Constants.AccountCodes.Cash),
            ("BANK", "Bank", Domain.Entities.CashBankAccountKind.Bank, Domain.Constants.AccountCodes.Bank)
        };
        foreach (var (code, name, kind, account) in defaultCashBank)
        {
            if (!await context.CashBankAccounts.AnyAsync(a => a.Code == code))
                context.CashBankAccounts.Add(new CashBankAccount { Code = code, Name = name, Kind = kind, AccountCode = account, IsActive = true, IsSystem = true });
        }
        await context.SaveChangesAsync();

        // 12b-14. Menu Keuangan → Bank & Kas + Rekonsiliasi (idempoten)
        if (!await context.MenuItems.AnyAsync(m => m.Controller == "CashBankAccounts"))
        {
            var financeGroup = await context.MenuItems.FirstOrDefaultAsync(m => m.Title == "Keuangan" && m.ParentId == null);
            if (financeGroup is not null)
            {
                var maxChild = await context.MenuItems.Where(m => m.ParentId == financeGroup.Id).MaxAsync(m => (int?)m.SortOrder) ?? 0;
                context.MenuItems.AddRange(
                    new MenuItem { Title = "Bank & Kas", Icon = "bi-wallet2", Controller = "CashBankAccounts", Action = "Index", ParentId = financeGroup.Id, SortOrder = maxChild + 1, RequiredRole = AppRoles.Administrator, IsSystem = true },
                    new MenuItem { Title = "Rekonsiliasi Bank", Icon = "bi-check2-square", Controller = "BankReconciliation", Action = "Index", ParentId = financeGroup.Id, SortOrder = maxChild + 2, RequiredRole = AppRoles.Administrator, IsSystem = true });
                await context.SaveChangesAsync();
            }
        }

        // 12b-15. Menu Keuangan → Tutup Buku (idempoten)
        if (!await context.MenuItems.AnyAsync(m => m.Controller == "FiscalYears"))
        {
            var financeGroup = await context.MenuItems.FirstOrDefaultAsync(m => m.Title == "Keuangan" && m.ParentId == null);
            if (financeGroup is not null)
            {
                var maxChild = await context.MenuItems.Where(m => m.ParentId == financeGroup.Id).MaxAsync(m => (int?)m.SortOrder) ?? 0;
                context.MenuItems.Add(new MenuItem { Title = "Tutup Buku", Icon = "bi-lock", Controller = "FiscalYears", Action = "Index", ParentId = financeGroup.Id, SortOrder = maxChild + 1, RequiredRole = AppRoles.Administrator, IsSystem = true });
                await context.SaveChangesAsync();
            }
        }

        // 12c. Penomoran dokumen bawaan (idempoten per kode)
        foreach (var (code, name) in Domain.Constants.DocumentCodes.BuiltIns)
        {
            if (!await context.DocumentNumberSequences.AnyAsync(s => s.Code == code))
            {
                context.DocumentNumberSequences.Add(new DocumentNumberSequence
                {
                    Code = code,
                    Name = name,
                    Prefix = code,
                    Format = "{PREFIX}-{YYYY}{MM}-{SEQ}",
                    Padding = 4,
                    NextNumber = 1,
                    ResetPeriod = Domain.Enums.NumberResetPeriod.Monthly,
                    IsSystem = true
                });
            }
        }
        await context.SaveChangesAsync();

        // 12d. Menu admin: Penomoran Dokumen (idempoten)
        if (!await context.MenuItems.AnyAsync(m => m.Controller == "DocumentNumbering"))
        {
            var adminGroup = await context.MenuItems.FirstOrDefaultAsync(m => m.Title == "Administrasi" && m.ParentId == null);
            if (adminGroup is not null)
            {
                var maxChild = await context.MenuItems.Where(m => m.ParentId == adminGroup.Id).MaxAsync(m => (int?)m.SortOrder) ?? 0;
                context.MenuItems.Add(new MenuItem { Title = "Penomoran Dokumen", Icon = "bi-123", Controller = "DocumentNumbering", Action = "Index", ParentId = adminGroup.Id, SortOrder = maxChild + 1, RequiredRole = AppRoles.Administrator, IsSystem = true });
                await context.SaveChangesAsync();
            }
        }

        // 13. Backfill "saldo awal" sebagai pergerakan stok (sekali jalan) agar Kartu Stok
        //     rekonsiliasi dengan saldo ProductStock yang dulu di-set langsung tanpa jejak.
        if (!await context.StockMovements.AnyAsync(m => m.ReferenceNumber.StartsWith("OPN")))
        {
            var stocks = await context.ProductStocks.ToListAsync();
            var moves = await context.StockMovements.ToListAsync();
            var openings = new List<StockMovement>();
            foreach (var st in stocks)
            {
                var net = moves.Where(m => m.ProductId == st.ProductId).Sum(m => m.Type switch
                {
                    Domain.Enums.MovementType.StockIn => m.WarehouseId == st.WarehouseId ? m.Quantity : 0,
                    Domain.Enums.MovementType.StockOut => m.WarehouseId == st.WarehouseId ? -m.Quantity : 0,
                    Domain.Enums.MovementType.Adjustment => m.WarehouseId == st.WarehouseId ? m.Quantity : 0,
                    Domain.Enums.MovementType.Transfer => m.WarehouseId == st.WarehouseId ? -m.Quantity
                        : (m.DestinationWarehouseId == st.WarehouseId ? m.Quantity : 0),
                    _ => 0
                });

                var opening = st.Quantity - net;
                if (opening == 0) continue;
                openings.Add(new StockMovement
                {
                    ReferenceNumber = $"OPN-{st.ProductId}-{st.WarehouseId}",
                    MovementDate = new DateTime(2026, 1, 1),
                    Type = opening > 0 ? Domain.Enums.MovementType.StockIn : Domain.Enums.MovementType.StockOut,
                    ProductId = st.ProductId,
                    WarehouseId = st.WarehouseId,
                    Quantity = Math.Abs(opening),
                    Note = "Saldo awal sistem"
                });
            }
            if (openings.Count > 0)
            {
                context.StockMovements.AddRange(openings);
                await context.SaveChangesAsync();
            }
        }
    }
}
