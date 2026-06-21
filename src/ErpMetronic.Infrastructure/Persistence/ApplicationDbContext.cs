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
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptLine> GoodsReceiptLines => Set<GoodsReceiptLine>();
    public DbSet<DeliveryOrder> DeliveryOrders => Set<DeliveryOrder>();
    public DbSet<DeliveryOrderLine> DeliveryOrderLines => Set<DeliveryOrderLine>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<DocumentNumberSequence> DocumentNumberSequences => Set<DocumentNumberSequence>();
    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
    public DbSet<PurchaseInvoiceLine> PurchaseInvoiceLines => Set<PurchaseInvoiceLine>();
    public DbSet<PurchasePayment> PurchasePayments => Set<PurchasePayment>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderItem> SalesOrderItems => Set<SalesOrderItem>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<SalesInvoiceLine> SalesInvoiceLines => Set<SalesInvoiceLine>();
    public DbSet<SalesPayment> SalesPayments => Set<SalesPayment>();
    public DbSet<PurchaseRequisition> PurchaseRequisitions => Set<PurchaseRequisition>();
    public DbSet<PurchaseRequisitionLine> PurchaseRequisitionLines => Set<PurchaseRequisitionLine>();
    public DbSet<RequestForQuotation> RequestForQuotations => Set<RequestForQuotation>();
    public DbSet<RfqLine> RfqLines => Set<RfqLine>();
    public DbSet<RfqQuote> RfqQuotes => Set<RfqQuote>();
    public DbSet<Tax> Taxes => Set<Tax>();
    public DbSet<PaymentTerm> PaymentTerms => Set<PaymentTerm>();
    public DbSet<CashBankAccount> CashBankAccounts => Set<CashBankAccount>();
    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<PriceListItem> PriceListItems => Set<PriceListItem>();
    public DbSet<SalesQuotation> SalesQuotations => Set<SalesQuotation>();
    public DbSet<SalesQuotationItem> SalesQuotationItems => Set<SalesQuotationItem>();
    public DbSet<ChartOfAccount> ChartOfAccounts => Set<ChartOfAccount>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();
    public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();
    public DbSet<SalesReturnLine> SalesReturnLines => Set<SalesReturnLine>();
    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
    public DbSet<PurchaseReturnLine> PurchaseReturnLines => Set<PurchaseReturnLine>();

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

        builder.Entity<GoodsReceipt>(e =>
        {
            e.HasIndex(x => x.ReferenceNumber);
            e.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<GoodsReceiptLine>(e =>
        {
            e.HasOne(x => x.GoodsReceipt).WithMany(g => g.Lines).HasForeignKey(x => x.GoodsReceiptId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<GoodsReceipt>()
            .HasOne(g => g.PurchaseOrder).WithMany().HasForeignKey(g => g.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PurchaseOrder>(e =>
        {
            e.HasIndex(x => x.ReferenceNumber);
            e.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Currency).WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<PurchaseOrderItem>(e =>
        {
            e.HasOne(x => x.PurchaseOrder).WithMany(p => p.Items).HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<DocumentNumberSequence>().HasIndex(x => x.Code).IsUnique();

        builder.Entity<PurchaseInvoice>(e =>
        {
            e.HasIndex(x => x.ReferenceNumber);
            e.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.PurchaseOrder).WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Currency).WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<PurchaseInvoiceLine>(e =>
        {
            e.HasOne(x => x.PurchaseInvoice).WithMany(p => p.Lines).HasForeignKey(x => x.PurchaseInvoiceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<PurchasePayment>(e =>
        {
            e.HasIndex(x => x.ReferenceNumber);
            e.HasOne(x => x.PurchaseInvoice).WithMany(p => p.Payments).HasForeignKey(x => x.PurchaseInvoiceId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DeliveryOrder>()
            .HasOne(d => d.SalesOrder).WithMany().HasForeignKey(d => d.SalesOrderId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SalesOrder>(e =>
        {
            e.HasIndex(x => x.ReferenceNumber);
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Currency).WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<SalesOrderItem>(e =>
        {
            e.HasOne(x => x.SalesOrder).WithMany(p => p.Items).HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<SalesInvoice>(e =>
        {
            e.HasIndex(x => x.ReferenceNumber);
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SalesOrder).WithMany().HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Currency).WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<SalesInvoiceLine>(e =>
        {
            e.HasOne(x => x.SalesInvoice).WithMany(p => p.Lines).HasForeignKey(x => x.SalesInvoiceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<SalesPayment>(e =>
        {
            e.HasIndex(x => x.ReferenceNumber);
            e.HasOne(x => x.SalesInvoice).WithMany(p => p.Payments).HasForeignKey(x => x.SalesInvoiceId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PurchaseRequisition>(e =>
        {
            e.HasIndex(x => x.ReferenceNumber);
        });
        builder.Entity<PurchaseRequisitionLine>(e =>
        {
            e.HasOne(x => x.PurchaseRequisition).WithMany(p => p.Lines).HasForeignKey(x => x.PurchaseRequisitionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RequestForQuotation>(e =>
        {
            e.HasIndex(x => x.ReferenceNumber);
            e.HasOne(x => x.PurchaseRequisition).WithMany().HasForeignKey(x => x.PurchaseRequisitionId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<RfqLine>(e =>
        {
            e.HasOne(x => x.RequestForQuotation).WithMany(p => p.Lines).HasForeignKey(x => x.RequestForQuotationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<RfqQuote>(e =>
        {
            e.HasOne(x => x.RequestForQuotation).WithMany(p => p.Quotes).HasForeignKey(x => x.RequestForQuotationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SalesReturn>(e =>
        {
            e.HasIndex(x => x.ReferenceNumber);
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<SalesReturnLine>(e =>
        {
            e.HasOne(x => x.SalesReturn).WithMany(p => p.Lines).HasForeignKey(x => x.SalesReturnId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<PurchaseReturn>(e =>
        {
            e.HasIndex(x => x.ReferenceNumber);
            e.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<PurchaseReturnLine>(e =>
        {
            e.HasOne(x => x.PurchaseReturn).WithMany(p => p.Lines).HasForeignKey(x => x.PurchaseReturnId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Tax>().HasIndex(x => x.Code).IsUnique();

        builder.Entity<FiscalYear>().HasIndex(x => x.Year).IsUnique();

        // Daftar Harga (price list) + item + relasi pelanggan.
        builder.Entity<PriceList>().HasIndex(x => x.Code).IsUnique();
        builder.Entity<PriceList>().HasOne(x => x.Currency).WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PriceListItem>(e =>
        {
            e.HasIndex(x => new { x.PriceListId, x.ProductId }).IsUnique();
            e.HasOne(x => x.PriceList).WithMany(p => p.Items).HasForeignKey(x => x.PriceListId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Customer>().HasOne(x => x.PriceList).WithMany().HasForeignKey(x => x.PriceListId).OnDelete(DeleteBehavior.Restrict);

        // Penawaran Penjualan (Sales Quotation)
        builder.Entity<SalesQuotation>(e =>
        {
            e.HasIndex(x => x.ReferenceNumber);
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Currency).WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.WithholdingTax).WithMany().HasForeignKey(x => x.WithholdingTaxId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<SalesQuotationItem>(e =>
        {
            e.HasOne(x => x.SalesQuotation).WithMany(p => p.Items).HasForeignKey(x => x.SalesQuotationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Tax).WithMany().HasForeignKey(x => x.TaxId).OnDelete(DeleteBehavior.Restrict);
        });

        // Akun Kas/Bank + relasi pembayaran (Restrict agar tidak terhapus saat dipakai).
        builder.Entity<CashBankAccount>().HasIndex(x => x.Code).IsUnique();
        builder.Entity<PurchasePayment>().HasOne(x => x.CashBankAccount).WithMany().HasForeignKey(x => x.CashBankAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SalesPayment>().HasOne(x => x.CashBankAccount).WithMany().HasForeignKey(x => x.CashBankAccountId).OnDelete(DeleteBehavior.Restrict);

        // Termin pembayaran + relasi ke mitra & faktur (Restrict agar tidak terhapus saat dipakai).
        builder.Entity<PaymentTerm>().HasIndex(x => x.Code).IsUnique();
        builder.Entity<Customer>().HasOne(x => x.PaymentTerm).WithMany().HasForeignKey(x => x.PaymentTermId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Supplier>().HasOne(x => x.PaymentTerm).WithMany().HasForeignKey(x => x.PaymentTermId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PurchaseInvoice>().HasOne(x => x.PaymentTerm).WithMany().HasForeignKey(x => x.PaymentTermId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SalesInvoice>().HasOne(x => x.PaymentTerm).WithMany().HasForeignKey(x => x.PaymentTermId).OnDelete(DeleteBehavior.Restrict);

        // Relasi pajak: PPN per baris & PPh (withholding) per header. Semua Restrict
        // agar pajak yang masih dipakai dokumen tidak bisa terhapus.
        builder.Entity<PurchaseOrderItem>().HasOne(x => x.Tax).WithMany().HasForeignKey(x => x.TaxId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PurchaseOrder>().HasOne(x => x.WithholdingTax).WithMany().HasForeignKey(x => x.WithholdingTaxId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SalesOrderItem>().HasOne(x => x.Tax).WithMany().HasForeignKey(x => x.TaxId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SalesOrder>().HasOne(x => x.WithholdingTax).WithMany().HasForeignKey(x => x.WithholdingTaxId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PurchaseInvoiceLine>().HasOne(x => x.Tax).WithMany().HasForeignKey(x => x.TaxId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PurchaseInvoice>().HasOne(x => x.WithholdingTax).WithMany().HasForeignKey(x => x.WithholdingTaxId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SalesInvoiceLine>().HasOne(x => x.Tax).WithMany().HasForeignKey(x => x.TaxId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SalesInvoice>().HasOne(x => x.WithholdingTax).WithMany().HasForeignKey(x => x.WithholdingTaxId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ChartOfAccount>().HasIndex(x => x.Code).IsUnique();
        builder.Entity<JournalEntry>(e => e.HasIndex(x => x.ReferenceNumber));
        builder.Entity<JournalLine>(e =>
        {
            e.HasOne(x => x.JournalEntry).WithMany(p => p.Lines).HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<DeliveryOrder>(e =>
        {
            e.HasIndex(x => x.ReferenceNumber);
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<DeliveryOrderLine>(e =>
        {
            e.HasOne(x => x.DeliveryOrder).WithMany(d => d.Lines).HasForeignKey(x => x.DeliveryOrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
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
