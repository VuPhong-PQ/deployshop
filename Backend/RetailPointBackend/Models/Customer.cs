using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetailPointBackend.Models
{
    public enum CustomerRank
    {
        Thuong = 0,    // 0 - Bronze equivalent
        Premium = 1,   // 1 - Silver equivalent  
        VIP = 2,       // 2 - Gold equivalent
        Platinum = 3   // 3 - Platinum
    }

    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }
        public string? HoTen { get; set; }
        public string? SoDienThoai { get; set; }
        public string? Email { get; set; }
        public string? DiaChi { get; set; }
        public CustomerRank HangKhachHang { get; set; }

        // Multi-store support
        public int? StoreId { get; set; }
        [ForeignKey("StoreId")]
        public virtual Store? Store { get; set; }

        // Loyalty system
        public int? TierId { get; set; } = 1; // Default to first tier
        [ForeignKey("TierId")]
        public virtual CustomerTier? CustomerTier { get; set; }
        
        public int LoyaltyPoints { get; set; } = 0;
        public DateTime? DateOfBirth { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalSpent { get; set; } = 0;
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public List<Order> Orders { get; set; } = new List<Order>();
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public List<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();
    }
}
