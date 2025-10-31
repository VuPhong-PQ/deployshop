using System;
using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Models
{
    public class QRSettings
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string BankCode { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string BankAccountNumber { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string BankAccountHolder { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string BankName { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? BankBranch { get; set; }
        
        // Chọn provider QR: "vietqr" hoặc "vnpayqr"
        [Required]
        [MaxLength(20)]
        public string QRProvider { get; set; } = "vietqr";
        
        // API credentials cho VietQR
        [MaxLength(200)]
        public string? VietQRClientId { get; set; }
        
        [MaxLength(200)]
        public string? VietQRApiKey { get; set; }
        
        // API credentials cho VNPAY QR (nếu cần)
        [MaxLength(200)]
        public string? VNPayApiKey { get; set; }
        
        [MaxLength(200)]
        public string? VNPaySecretKey { get; set; }
        
        // Template QR: "compact", "print", "qr_only"
        [MaxLength(20)]
        public string QRTemplate { get; set; } = "compact";
        
        // Có kích hoạt QR hay không
        public bool IsEnabled { get; set; } = true;
        
        // Nội dung mô tả mặc định khi thanh toán
        [MaxLength(200)]
        public string? DefaultDescription { get; set; } = "Thanh toan hoa don";
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}