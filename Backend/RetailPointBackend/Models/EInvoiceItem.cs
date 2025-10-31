using System;
using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Models
{
    /// <summary>
    /// Chi tiết hóa đơn điện tử theo TT78/2022/TT-BTC
    /// </summary>
    public class EInvoiceItem
    {
        public int EInvoiceItemId { get; set; }
        
        public int EInvoiceId { get; set; }
        public EInvoice EInvoice { get; set; }
        
        public int LineNumber { get; set; } // Số thứ tự dòng
        
        [Required]
        [MaxLength(20)]
        public string ItemCode { get; set; } // Mã hàng hóa, dịch vụ
        
        [Required]
        [MaxLength(500)]
        public string ItemName { get; set; } // Tên hàng hóa, dịch vụ
        
        [MaxLength(50)]
        public string? Unit { get; set; } // Đơn vị tính
        
        public decimal Quantity { get; set; } // Số lượng
        
        public decimal UnitPrice { get; set; } // Đơn giá
        
        public decimal LineTotal { get; set; } // Thành tiền (chưa thuế)
        
        // Thông tin thuế
        [MaxLength(10)]
        public string TaxRate { get; set; } = "10%"; // Thuế suất (0%, 5%, 10%, KCT - không chịu thuế, KKKNT - không kê khai không nộp thuế)
        
        public decimal TaxAmount { get; set; } // Tiền thuế
        
        public decimal TotalAmount { get; set; } // Tổng tiền (có thuế)
        
        // Chiết khấu
        public decimal DiscountRate { get; set; } = 0; // Tỷ lệ chiết khấu (%)
        public decimal DiscountAmount { get; set; } = 0; // Tiền chiết khấu
        
        // Liên kết với sản phẩm
        public int? ProductId { get; set; }
        public Product? Product { get; set; }
        
        // Liên kết với OrderItem (nếu có)
        public int? OrderItemId { get; set; }
        public OrderItem? OrderItem { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}