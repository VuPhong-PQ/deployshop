using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetailPointBackend.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
        public int? CustomerId { get; set; }
        
        // Thêm các trường mới cho thông tin chi tiết
        public decimal SubTotal { get; set; } = 0;
        public decimal TaxAmount { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;
        public string? PaymentMethod { get; set; } = "cash"; // cash, card, qr, ewallet
        public string? PaymentStatus { get; set; } = "pending"; // paid, pending, failed - default pending
        public string? Status { get; set; } = "pending"; // completed, pending, cancelled - default pending
        public string? CancellationReason { get; set; } // Lý do hủy đơn hàng
        public string? OrderNumber { get; set; }
        public int? StaffId { get; set; } // Foreign Key to Staff
        public string? StoreId { get; set; } // Foreign Key to Store - tạm thời dùng string
        public string? Notes { get; set; }
        
        public Customer? Customer { get; set; }
        public Staff? Staff { get; set; } // Navigation property to Staff
        // [ForeignKey("StoreId")]
        // public virtual Store? Store { get; set; } // Navigation property to Store - temporarily disabled due to type mismatch
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
