using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetailPointBackend.Models
{
    [Table("LoyaltySettings")]
    public class LoyaltySettings
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Kích hoạt tích điểm - Bật/tắt hệ thống tích điểm
        /// </summary>
        public bool IsPointsEnabled { get; set; } = true;

        /// <summary>
        /// Tỷ lệ tích điểm (VND/điểm) - VD: 1000 = 1000 VNĐ được 1 điểm
        /// </summary>
        public decimal PointsRate { get; set; } = 1000;

        /// <summary>
        /// Cho phép đổi điểm - Bật/tắt tính năng đổi điểm ra tiền
        /// </summary>
        public bool IsRedemptionEnabled { get; set; } = true;

        /// <summary>
        /// Giá trị điểm (VND/điểm) - VD: 1000 = 1 điểm = 1000 VNĐ
        /// </summary>
        public decimal RedemptionRate { get; set; } = 1000;

        /// <summary>
        /// Đơn hàng tối thiểu (VND) - Đơn hàng tối thiểu để được tích điểm
        /// </summary>
        public decimal MinOrderAmount { get; set; } = 50000;

        /// <summary>
        /// Tối đa đổi điểm (%) - Phần trăm tối đa hóa đơn có thể thanh toán bằng điểm
        /// </summary>
        public int MaxRedemptionPercentage { get; set; } = 50;

        /// <summary>
        /// Điểm tối đa/đơn hàng - Không giới hạn nếu là 0
        /// </summary>
        public int MaxPointsPerOrder { get; set; } = 0;

        /// <summary>
        /// Điểm hết hạn sau (ngày) - Điểm sẽ hết hạn sau số ngày này, 0 = không hết hạn
        /// </summary>
        public int PointsExpirationDays { get; set; } = 365;

        /// <summary>
        /// Thời gian tạo
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Thời gian cập nhật cuối
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Người cập nhật cuối
        /// </summary>
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// Ghi chú cấu hình
        /// </summary>
        public string? Notes { get; set; }
    }
}