using Microsoft.EntityFrameworkCore;

namespace RetailPointBackend.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        // Constructor không tham số cho migration design-time
        public AppDbContext() { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Lấy connection string mặc định cho migration
                optionsBuilder.UseSqlServer("Server=TEST-PC\\KTEAM;Database=RetailPoint;User Id=sa;Password=sa@123;MultipleActiveResultSets=True;TrustServerCertificate=True;");
            }
        }

    public DbSet<Product> Products { get; set; }
    public DbSet<ProductGroup> ProductGroups { get; set; }
    public DbSet<PaymentSettings> PaymentSettings { get; set; }
    public DbSet<QRSettings> QRSettings { get; set; }
    public DbSet<StoreInfo> StoreInfos { get; set; }
    public DbSet<TaxConfig> TaxConfigs { get; set; }
    public DbSet<PaymentMethodConfig> PaymentMethodConfigs { get; set; }
    public DbSet<PrintConfig> PrintConfigs { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Staff> Staffs { get; set; }
    public DbSet<Store> Stores { get; set; }
    public DbSet<StaffStore> StaffStores { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
    public DbSet<BackupHistory> BackupHistories { get; set; }
    public DbSet<BackupSettings> BackupSettings { get; set; }
    
    // E-Invoice tables
    public DbSet<EInvoice> EInvoices { get; set; }
    public DbSet<EInvoiceItem> EInvoiceItems { get; set; }
    public DbSet<EInvoiceConfig> EInvoiceConfigs { get; set; }
    
    // Discount tables
    public DbSet<Discount> Discounts { get; set; }
    public DbSet<OrderDiscount> OrderDiscounts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure decimal properties precision
        modelBuilder.Entity<Discount>()
            .Property(d => d.MinOrderValue)
            .HasPrecision(18, 2);
        
        modelBuilder.Entity<Discount>()
            .Property(d => d.Value)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .Property(o => o.DiscountAmount)
            .HasPrecision(18, 2);
        
        modelBuilder.Entity<Order>()
            .Property(o => o.SubTotal)
            .HasPrecision(18, 2);
        
        modelBuilder.Entity<Order>()
            .Property(o => o.TaxAmount)
            .HasPrecision(18, 2);
        
        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.Price)
            .HasPrecision(18, 2);
        
        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.DiscountAmount)
            .HasPrecision(18, 2);
        
        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.FinalPrice)
            .HasPrecision(18, 2);
        
        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.TotalPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);
        
        modelBuilder.Entity<Product>()
            .Property(p => p.CostPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<TaxConfig>()
            .Property(tc => tc.VATRate)
            .HasPrecision(5, 2);
        
        modelBuilder.Entity<TaxConfig>()
            .Property(tc => tc.EnvTaxRate)
            .HasPrecision(5, 2);

        // Seed default data
        SeedDefaultData(modelBuilder);
    }

    private void SeedDefaultData(ModelBuilder modelBuilder)
    {
        // Seed default TaxConfig
        modelBuilder.Entity<TaxConfig>().HasData(
            new TaxConfig
            {
                Id = 1,
                EnableVAT = false,
                VATIncludedInPrice = true,
                VATRate = 10.0m,
                VATLabel = "VAT",
                EnableEnvTax = false,
                EnvTaxRate = 2.0m
            }
        );

        // Seed default PrintConfig
        modelBuilder.Entity<PrintConfig>().HasData(
            new PrintConfig
            {
                Id = 1,
                PrinterName = "Default Printer",
                PaperSize = "80mm",
                PrintCopies = 1,
                AutoPrintBill = true,
                AutoPrintOnOrder = false,
                PrintBarcode = true,
                PrintLogo = false,
                BillHeader = "RETAIL POINT STORE",
                BillFooter = "Cảm ơn quý khách!"
            }
        );
    }
    }

}
