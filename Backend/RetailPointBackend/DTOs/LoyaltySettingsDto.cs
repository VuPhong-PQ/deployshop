using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.DTOs
{
    public class LoyaltyConfigDto
    {
        public int LoyaltyConfigId { get; set; }
        
        [Required]
        public bool IsEnabled { get; set; }
        
        [Required]
        [Range(1, 1000000)]
        public decimal PointsPerCurrency { get; set; } = 1000;
        
        [Required]
        [Range(0, 10000000)]
        public decimal MinOrderAmountForPoints { get; set; } = 50000;
        
        public int? MaxPointsPerOrder { get; set; }
        
        [Required]
        [Range(1, 3650)]
        public int PointExpiryDays { get; set; } = 365;
        
        public bool AllowPointRedemption { get; set; } = true;
        
        [Required]
        [Range(1, 100000)]
        public decimal PointValue { get; set; } = 1000;
        
        [Range(1, 100)]
        public decimal MaxRedemptionPercentage { get; set; } = 50;
        
        // Happy Hour Settings
        public bool HappyHourEnabled { get; set; } = false;
        public TimeSpan? HappyHourStartTime { get; set; }
        public TimeSpan? HappyHourEndTime { get; set; }
        [Range(1, 10)]
        public decimal HappyHourMultiplier { get; set; } = 2.0m;
        
        // Weekend Bonus
        public bool WeekendBonusEnabled { get; set; } = false;
        [Range(1, 10)]
        public decimal WeekendMultiplier { get; set; } = 1.5m;
        
        // Birthday Bonus
        public bool BirthdayBonusEnabled { get; set; } = false;
        [Range(1, 10)]
        public decimal BirthdayMultiplier { get; set; } = 3.0m;
        [Range(1, 30)]
        public int BirthdayValidDays { get; set; } = 7;
    }

    public class CustomerTierDto
    {
        public int TierId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string TierName { get; set; } = "";
        
        [Required]
        [Range(0, 1000000000)]
        public decimal MinSpent { get; set; } = 0;
        
        [Required]
        [Range(0, 1000000)]
        public int MinPoints { get; set; } = 0;
        
        [Required]
        [Range(1, 10)]
        public decimal PointsMultiplier { get; set; } = 1.0m;
        
        [Range(0, 100)]
        public decimal DiscountPercentage { get; set; } = 0;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [StringLength(10)]
        public string TierColor { get; set; } = "#000000";
        
        public bool IsActive { get; set; } = true;
    }

    public class LoyaltySettingsDto
    {
        public LoyaltyConfigDto Config { get; set; } = new();
        public List<CustomerTierDto> Tiers { get; set; } = new();
    }

    public class CustomerLoyaltyStatusDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public decimal TotalSpent { get; set; }
        public int TotalPoints { get; set; }
        public CustomerTierInfoDto? CurrentTier { get; set; }
        public CustomerTierInfoDto? NextTier { get; set; }
        public LoyaltyProgressDto Progress { get; set; } = new();
    }

    public class CustomerTierInfoDto
    {
        public int TierId { get; set; }
        public string TierName { get; set; } = "";
        public string TierColor { get; set; } = "";
        public decimal DiscountPercentage { get; set; }
        public decimal PointsMultiplier { get; set; }
        public string? Description { get; set; }
        public decimal? MinSpent { get; set; }
        public int? MinPoints { get; set; }
    }

    public class LoyaltyProgressDto
    {
        public decimal SpentToNext { get; set; }
        public int PointsToNext { get; set; }
        public decimal ProgressPercentage { get; set; }
    }

    public class PointsCalculationDto
    {
        public int Points { get; set; }
        public int BasePoints { get; set; }
        public decimal Multiplier { get; set; }
        public List<string> BonusInfo { get; set; } = new();
        public string Formula { get; set; } = "";
        public string? Message { get; set; }
    }
}