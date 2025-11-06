using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetailPointBackend.Models
{
    public enum LoyaltyPromotionType
    {
        BONUS_POINTS = 0,    // Thưởng điểm cố định
        MULTIPLIER = 1,      // Nhân điểm
        MILESTONE = 2        // Mốc thành tích
    }

    public class LoyaltyPromotion
    {
        [Key]
        public int PromotionId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        public LoyaltyPromotionType PromotionType { get; set; }
        
        // Điều kiện
        [Column(TypeName = "decimal(18,2)")]
        public decimal MinOrderAmount { get; set; } = 0;
        
        public int MinQuantity { get; set; } = 1;
        
        public int? TargetCustomerTier { get; set; }
        [ForeignKey("TargetCustomerTier")]
        public virtual CustomerTier? CustomerTier { get; set; }
        
        // Phần thưởng
        public int BonusPoints { get; set; } = 0;
        
        [Column(TypeName = "decimal(3,2)")]
        public decimal PointsMultiplier { get; set; } = 1.0m;
        
        // Thời gian
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        // Giới hạn
        public int? MaxUsagePerCustomer { get; set; }
        public int? MaxTotalUsage { get; set; }
        public int CurrentUsage { get; set; } = 0;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        [ForeignKey("CreatedBy")]
        public virtual Staff? CreatedByStaff { get; set; }
    }
}