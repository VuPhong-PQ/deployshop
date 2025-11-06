using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetailPointBackend.Models
{
    public class CustomerTier
    {
        [Key]
        public int TierId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string TierName { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal MinSpent { get; set; } = 0;
        
        public int MinPoints { get; set; } = 0;
        
        [Column(TypeName = "decimal(3,2)")]
        public decimal PointsMultiplier { get; set; } = 1.0m;
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercentage { get; set; } = 0;
        
        [MaxLength(255)]
        public string? Description { get; set; }
        
        [MaxLength(7)]
        public string TierColor { get; set; } = "#808080";
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}