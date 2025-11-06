using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetailPointBackend.Models
{
    public class CategoryLoyaltyRule
    {
        [Key]
        public int RuleId { get; set; }
        
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; } = null!;
        
        [Column(TypeName = "decimal(3,2)")]
        public decimal PointsMultiplier { get; set; } = 1.0m;
        
        public int BonusPoints { get; set; } = 0;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime ValidFrom { get; set; } = DateTime.Now;
        public DateTime? ValidTo { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}