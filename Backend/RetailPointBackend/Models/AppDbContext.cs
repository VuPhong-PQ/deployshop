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
    public DbSet<Category> Categories { get; set; }
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
    
    // Loyalty system tables
    public DbSet<LoyaltyConfig> LoyaltyConfigs { get; set; }
    public DbSet<CustomerTier> CustomerTiers { get; set; }
    public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; }
    public DbSet<CategoryLoyaltyRule> CategoryLoyaltyRules { get; set; }
    public DbSet<ProductLoyaltyRule> ProductLoyaltyRules { get; set; }
    public DbSet<LoyaltyPromotion> LoyaltyPromotions { get; set; }
    public DbSet<LoyaltySettings> LoyaltySettings { get; set; }

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

        // Configure Customer decimal properties
        modelBuilder.Entity<Customer>()
            .Property(c => c.TotalSpent)
            .HasPrecision(18, 2);

        // Configure table names để match với database hiện tại
        modelBuilder.Entity<Category>()
            .ToTable("Category");

        // Configure Loyalty System decimal properties
        modelBuilder.Entity<LoyaltyConfig>()
            .Property(lc => lc.PointsPerCurrency)
            .HasPrecision(10, 2);
        
        modelBuilder.Entity<LoyaltyConfig>()
            .Property(lc => lc.MinOrderAmountForPoints)
            .HasPrecision(18, 2);
        
        modelBuilder.Entity<LoyaltyConfig>()
            .Property(lc => lc.PointValue)
            .HasPrecision(10, 2);
        
        modelBuilder.Entity<LoyaltyConfig>()
            .Property(lc => lc.MaxRedemptionPercentage)
            .HasPrecision(5, 2);
        
        modelBuilder.Entity<LoyaltyConfig>()
            .Property(lc => lc.HappyHourMultiplier)
            .HasPrecision(3, 2);
        
        modelBuilder.Entity<LoyaltyConfig>()
            .Property(lc => lc.WeekendMultiplier)
            .HasPrecision(3, 2);
        
        modelBuilder.Entity<LoyaltyConfig>()
            .Property(lc => lc.BirthdayMultiplier)
            .HasPrecision(3, 2);

        modelBuilder.Entity<CustomerTier>()
            .Property(ct => ct.MinSpent)
            .HasPrecision(18, 2);
        
        modelBuilder.Entity<CustomerTier>()
            .Property(ct => ct.PointsMultiplier)
            .HasPrecision(3, 2);
        
        modelBuilder.Entity<CustomerTier>()
            .Property(ct => ct.DiscountPercentage)
            .HasPrecision(5, 2);

        modelBuilder.Entity<CategoryLoyaltyRule>()
            .Property(clr => clr.PointsMultiplier)
            .HasPrecision(3, 2);

        modelBuilder.Entity<ProductLoyaltyRule>()
            .Property(plr => plr.PointsMultiplier)
            .HasPrecision(3, 2);

        modelBuilder.Entity<LoyaltyPromotion>()
            .Property(lp => lp.MinOrderAmount)
            .HasPrecision(18, 2);
        
        modelBuilder.Entity<LoyaltyPromotion>()
            .Property(lp => lp.PointsMultiplier)
            .HasPrecision(3, 2);

        // Configure LoyaltySettings decimal properties
        modelBuilder.Entity<LoyaltySettings>()
            .Property(ls => ls.PointsRate)
            .HasPrecision(18, 2);
        
        modelBuilder.Entity<LoyaltySettings>()
            .Property(ls => ls.RedemptionRate)
            .HasPrecision(18, 2);
        
        modelBuilder.Entity<LoyaltySettings>()
            .Property(ls => ls.MinOrderAmount)
            .HasPrecision(18, 2);

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

        // Seed default LoyaltyConfig
        modelBuilder.Entity<LoyaltyConfig>().HasData(
            new LoyaltyConfig
            {
                LoyaltyConfigId = 1,
                IsEnabled = true,
                PointsPerCurrency = 1000.0m,
                MinOrderAmountForPoints = 50000,
                PointExpiryDays = 365,
                AllowPointRedemption = true,
                PointValue = 1000.0m,
                MaxRedemptionPercentage = 50.0m,
                HappyHourEnabled = false,
                HappyHourStartTime = new TimeSpan(17, 0, 0),
                HappyHourEndTime = new TimeSpan(19, 0, 0),
                HappyHourMultiplier = 2.0m,
                WeekendBonusEnabled = false,
                WeekendMultiplier = 1.5m,
                BirthdayBonusEnabled = false,
                BirthdayMultiplier = 3.0m,
                BirthdayValidDays = 7,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed default CustomerTiers
        modelBuilder.Entity<CustomerTier>().HasData(
            new CustomerTier
            {
                TierId = 1,
                TierName = "Đồng",
                MinSpent = 0,
                MinPoints = 0,
                PointsMultiplier = 1.0m,
                DiscountPercentage = 0,
                Description = "Khách hàng mới",
                TierColor = "#CD7F32",
                IsActive = true,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new CustomerTier
            {
                TierId = 2,
                TierName = "Bạc",
                MinSpent = 5000000,
                MinPoints = 500,
                PointsMultiplier = 1.2m,
                DiscountPercentage = 2,
                Description = "Khách hàng thân thiết",
                TierColor = "#C0C0C0",
                IsActive = true,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new CustomerTier
            {
                TierId = 3,
                TierName = "Vàng",
                MinSpent = 20000000,
                MinPoints = 2000,
                PointsMultiplier = 1.5m,
                DiscountPercentage = 5,
                Description = "Khách hàng VIP",
                TierColor = "#FFD700",
                IsActive = true,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new CustomerTier
            {
                TierId = 4,
                TierName = "Kim cương",
                MinSpent = 50000000,
                MinPoints = 5000,
                PointsMultiplier = 2.0m,
                DiscountPercentage = 10,
                Description = "Khách hàng VVIP",
                TierColor = "#B9F2FF",
                IsActive = true,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed default LoyaltySettings
        modelBuilder.Entity<LoyaltySettings>().HasData(
            new LoyaltySettings
            {
                Id = 1,
                IsPointsEnabled = true,
                PointsRate = 1000,
                IsRedemptionEnabled = true,
                RedemptionRate = 1000,
                MinOrderAmount = 50000,
                MaxRedemptionPercentage = 50,
                MaxPointsPerOrder = 0,
                PointsExpirationDays = 365,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Notes = "Cài đặt tích điểm mặc định"
            }
        );
    }
    }

}
