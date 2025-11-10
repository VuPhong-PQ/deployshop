using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Models
{
    public class DiscountType
    {
        [Key]
        public int DiscountId { get; set; }

        [Required]
        [StringLength(100)]
        public string DiscountName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        [Required]
        public DiscountCalculationType CalculationType { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal DiscountValue { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MinOrderAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MaxDiscountAmount { get; set; }

        public DateTime? ValidFrom { get; set; }

        public DateTime? ValidTo { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        public bool RequiresApproval { get; set; } = false;

        [Required]
        public bool IsAutomaticDiscount { get; set; } = false;

        public int? CustomerTierRequired { get; set; }

        [StringLength(50)]
        public string? PromoCode { get; set; }

        [Range(0, int.MaxValue)]
        public int? UsageLimit { get; set; }

        [Range(0, int.MaxValue)]
        public int UsageCount { get; set; } = 0;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public int CreatedByStaffId { get; set; }

        // Navigation properties
        public virtual Staff? CreatedByStaff { get; set; }
        public virtual CustomerTier? RequiredTier { get; set; }
        public virtual ICollection<DiscountApplicableProduct>? ApplicableProducts { get; set; }
        public virtual ICollection<OrderDiscount>? OrderDiscounts { get; set; }
    }

    public enum DiscountCalculationType
    {
        Percentage = 1,      // Giảm theo phần trăm
        FixedAmount = 2,     // Giảm số tiền cố định
        BuyXGetY = 3,        // Mua X tặng Y
        FreeShipping = 4     // Miễn phí vận chuyển
    }

    public class DiscountApplicableProduct
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DiscountId { get; set; }

        [Required]
        public int ProductId { get; set; }

        // Navigation properties
        public virtual DiscountType? Discount { get; set; }
        public virtual Product? Product { get; set; }
    }

    public class OrderDiscount
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int DiscountId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal DiscountAmount { get; set; }

        [StringLength(255)]
        public string? AppliedReason { get; set; }

        [Required]
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int AppliedByStaffId { get; set; }

        // Navigation properties
        public virtual Order? Order { get; set; }
        public virtual DiscountType? Discount { get; set; }
        public virtual Staff? AppliedByStaff { get; set; }
    }
}