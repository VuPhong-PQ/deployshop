using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetailPointBackend.Models
{
    public class LoyaltyConfig
    {
        [Key]
        public int LoyaltyConfigId { get; set; }
        
        public bool IsEnabled { get; set; } = true;
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal PointsPerCurrency { get; set; } = 1000.0m; // 1000 VND = 1 điểm
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal MinOrderAmountForPoints { get; set; } = 0;
        
        public int? MaxPointsPerOrder { get; set; }
        
        public int PointExpiryDays { get; set; } = 365;
        
        public bool AllowPointRedemption { get; set; } = true;
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal PointValue { get; set; } = 100.0m; // 100 điểm = 1000 VND
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal MaxRedemptionPercentage { get; set; } = 50.0m;
        
        // Tích điểm theo thời gian
        public bool HappyHourEnabled { get; set; } = false;
        public TimeSpan HappyHourStartTime { get; set; } = new TimeSpan(17, 0, 0);
        public TimeSpan HappyHourEndTime { get; set; } = new TimeSpan(19, 0, 0);
        
        [Column(TypeName = "decimal(3,2)")]
        public decimal HappyHourMultiplier { get; set; } = 2.0m;
        
        public bool WeekendBonusEnabled { get; set; } = false;
        
        [Column(TypeName = "decimal(3,2)")]
        public decimal WeekendMultiplier { get; set; } = 1.5m;
        
        public bool BirthdayBonusEnabled { get; set; } = false;
        
        [Column(TypeName = "decimal(3,2)")]
        public decimal BirthdayMultiplier { get; set; } = 3.0m;
        
        public int BirthdayValidDays { get; set; } = 7;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public int? CreatedBy { get; set; }
    }
}