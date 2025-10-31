using System;
using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Models
{
    public enum NotificationType
    {
        NewOrder = 1,        // Đơn hàng mới
        LowStock = 2,        // Cảnh báo tồn kho
        PaymentSuccess = 3,  // Thanh toán thành công
        OutOfStock = 4,      // Hết hàng
        SystemAlert = 5      // Cảnh báo hệ thống
    }

    public enum NotificationStatus
    {
        Unread = 0,  // Chưa đọc
        Read = 1     // Đã đọc
    }

    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }
        
        [Required]
        public NotificationType Type { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Message { get; set; }
        
        public NotificationStatus Status { get; set; } = NotificationStatus.Unread;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? ReadAt { get; set; }
        
        // Reference IDs for related entities
        public int? OrderId { get; set; }
        public int? ProductId { get; set; }
        public int? CustomerId { get; set; }
        
        // Metadata as JSON string for additional info
        public string? Metadata { get; set; }
        
        // Navigation properties
        public Order? Order { get; set; }
        // Note: Product would need to be added as navigation property if needed
    }
}