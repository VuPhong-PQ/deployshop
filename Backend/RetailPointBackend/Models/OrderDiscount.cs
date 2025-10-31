using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetailPointBackend.Models
{
    [Table("OrderDiscounts")]
    public class OrderDiscount
    {
        [Key]
        public int OrderDiscountId { get; set; }
        
        public int OrderId { get; set; }
        public int DiscountId { get; set; }
        
        // Thông tin chi tiết giảm giá được áp dụng
        public string DiscountName { get; set; } = string.Empty;
        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }      // Giá trị giảm (% hoặc tiền)
        public decimal DiscountAmount { get; set; }     // Số tiền thực tế được giảm
        
        // Áp dụng cho mặt hàng cụ thể (nếu có)
        public int? OrderItemId { get; set; }
        
        public DateTime AppliedAt { get; set; } = DateTime.Now;
        public int? AppliedBy { get; set; }             // Staff ID
        
        // Navigation properties
        [ForeignKey("OrderId")]
        public Order? Order { get; set; }
        
        [ForeignKey("DiscountId")]
        public Discount? Discount { get; set; }
        
        [ForeignKey("OrderItemId")]
        public OrderItem? OrderItem { get; set; }
        
        [ForeignKey("AppliedBy")]
        public Staff? AppliedByStaff { get; set; }
    }
}