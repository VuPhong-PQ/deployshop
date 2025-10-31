using System;
using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Models
{
    /// <summary>
    /// Cấu hình hóa đơn điện tử
    /// </summary>
    public class EInvoiceConfig
    {
        public int EInvoiceConfigId { get; set; }
        
        // Cấu hình chung
        public bool IsEnabled { get; set; } = false; // Bật/tắt hóa đơn điện tử
        
        [Required]
        [MaxLength(100)]
        public string Provider { get; set; } = "VNPT"; // Nhà cung cấp (VNPT, Viettel, FPT, etc.)
        
        [MaxLength(500)]
        public string? ApiUrl { get; set; } // URL API của nhà cung cấp
        
        [MaxLength(100)]
        public string? Username { get; set; } // Tài khoản
        
        [MaxLength(100)]
        public string? Password { get; set; } // Mật khẩu
        
        [MaxLength(200)]
        public string? Token { get; set; } // Token xác thực
        
        [MaxLength(100)]
        public string? CompanyCode { get; set; } // Mã công ty trên hệ thống nhà cung cấp
        
        // Thông tin mẫu hóa đơn
        [MaxLength(20)]
        public string DefaultTemplate { get; set; } = "01GTKT0/001"; // Mẫu số hóa đơn mặc định
        
        [MaxLength(20)]
        public string DefaultSymbol { get; set; } = "C22TKT"; // Ký hiệu hóa đơn mặc định
        
        // Cấu hình tự động
        public bool AutoIssue { get; set; } = false; // Tự động phát hành sau khi tạo
        public bool AutoSendEmail { get; set; } = false; // Tự động gửi email cho khách hàng
        public bool AutoSendSMS { get; set; } = false; // Tự động gửi SMS cho khách hàng
        
        // Thông tin công ty (người bán)
        [MaxLength(13)]
        public string? CompanyTaxCode { get; set; } // Mã số thuế công ty
        
        [MaxLength(500)]
        public string? CompanyName { get; set; } // Tên công ty
        
        [MaxLength(500)]
        public string? CompanyAddress { get; set; } // Địa chỉ công ty
        
        [MaxLength(50)]
        public string? CompanyPhone { get; set; } // Điện thoại công ty
        
        [MaxLength(100)]
        public string? CompanyEmail { get; set; } // Email công ty
        
        [MaxLength(50)]
        public string? CompanyBankAccount { get; set; } // Số tài khoản công ty
        
        [MaxLength(200)]
        public string? CompanyBankName { get; set; } // Tên ngân hàng
        
        // Cấu hình thuế
        [MaxLength(10)]
        public string DefaultTaxRate { get; set; } = "10%"; // Thuế suất mặc định
        
        // Cấu hình template email/SMS
        [MaxLength(1000)]
        public string? EmailTemplate { get; set; } // Template email gửi hóa đơn
        
        [MaxLength(500)]
        public string? SMSTemplate { get; set; } // Template SMS gửi hóa đơn
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        public int? CreatedByStaffId { get; set; }
        public Staff? CreatedByStaff { get; set; }
    }
}