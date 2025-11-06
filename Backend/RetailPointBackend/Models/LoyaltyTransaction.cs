using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetailPointBackend.Models
{
    public enum LoyaltyTransactionType
    {
        EARN = 0,      // Tích điểm
        REDEEM = 1,    // Đổi điểm
        EXPIRE = 2,    // Hết hạn
        ADJUST = 3     // Điều chỉnh thủ công
    }

    public class LoyaltyTransaction
    {
        [Key]
        public int TransactionId { get; set; }
        
        public int CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; } = null!;
        
        public int? OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
        
        public LoyaltyTransactionType TransactionType { get; set; }
        
        public int Points { get; set; } // Có thể âm hoặc dương
        
        public int PointsBalance { get; set; } // Số dư sau giao dịch
        
        [MaxLength(255)]
        public string? Reason { get; set; }
        
        public DateTime? ExpiryDate { get; set; }
        
        public DateTime ProcessedAt { get; set; } = DateTime.Now;
        
        public int? ProcessedBy { get; set; }
        [ForeignKey("ProcessedBy")]
        public virtual Staff? ProcessedByStaff { get; set; }
    }
}