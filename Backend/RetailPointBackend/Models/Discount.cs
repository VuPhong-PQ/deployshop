using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetailPointBackend.Models
{
    public enum DiscountType
    {
        PercentageTotal = 1,    // Giảm % tổng bill
        FixedAmountItem = 2,    // Giảm tiền mặt hàng cụ thể
        FixedAmountTotal = 3    // Giảm tiền tổng bill
    }

    [Table("Discounts")]
    public class Discount
    {
        [Key]
        public int DiscountId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public DiscountType Type { get; set; }
        
        // Giá trị giảm (% cho PercentageTotal, số tiền cho Fixed)
        public decimal Value { get; set; }
        
        // Điều kiện áp dụng
        public decimal? MinOrderValue { get; set; } // Giá trị đơn hàng tối thiểu
        public int? MinQuantity { get; set; }       // Số lượng tối thiểu
        public int? ProductId { get; set; }         // Áp dụng cho sản phẩm cụ thể (nếu có)
        public int? CategoryId { get; set; }        // Áp dụng cho danh mục cụ thể (nếu có)
        
        // Thời gian hiệu lực
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        
        // Trạng thái
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        
        // Giới hạn sử dụng
        public int? MaxUsage { get; set; }          // Số lần sử dụng tối đa
        public int UsageCount { get; set; } = 0;    // Đã sử dụng bao nhiêu lần
        
        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public int? CreatedBy { get; set; }         // Staff ID
        
        // Navigation properties
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }
        
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }
        
        [ForeignKey("CreatedBy")]
        public Staff? CreatedByStaff { get; set; }
    }
}