using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Models
{
    /// <summary>
    /// Hóa đơn điện tử theo TT78/2022/TT-BTC
    /// </summary>
    public class EInvoice
    {
        public int EInvoiceId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string InvoiceNumber { get; set; } // Số hóa đơn
        
        [Required]
        [MaxLength(20)]
        public string InvoiceTemplate { get; set; } = "01GTKT0/001"; // Mẫu số hóa đơn
        
        [Required]
        [MaxLength(20)]
        public string InvoiceSymbol { get; set; } = "C22TKT"; // Ký hiệu hóa đơn
        
        public DateTime IssueDate { get; set; } = DateTime.Now; // Ngày phát hành
        
        [Required]
        [MaxLength(20)]
        public string CurrencyCode { get; set; } = "VND"; // Loại tiền tệ
        
        public decimal ExchangeRate { get; set; } = 1; // Tỷ giá
        
        // Thông tin người bán
        [Required]
        [MaxLength(13)]
        public string SellerTaxCode { get; set; } // Mã số thuế người bán
        
        [Required]
        [MaxLength(500)]
        public string SellerName { get; set; } // Tên người bán
        
        [MaxLength(500)]
        public string? SellerAddress { get; set; } // Địa chỉ người bán
        
        [MaxLength(50)]
        public string? SellerPhone { get; set; } // Điện thoại người bán
        
        [MaxLength(100)]
        public string? SellerEmail { get; set; } // Email người bán
        
        [MaxLength(50)]
        public string? SellerBankAccount { get; set; } // Số tài khoản người bán
        
        [MaxLength(200)]
        public string? SellerBankName { get; set; } // Tên ngân hàng
        
        // Thông tin người mua
        [MaxLength(13)]
        public string? BuyerTaxCode { get; set; } // Mã số thuế người mua
        
        [MaxLength(500)]
        public string? BuyerName { get; set; } // Tên người mua
        
        [MaxLength(500)]
        public string? BuyerAddress { get; set; } // Địa chỉ người mua
        
        [MaxLength(50)]
        public string? BuyerPhone { get; set; } // Điện thoại người mua
        
        [MaxLength(100)]
        public string? BuyerEmail { get; set; } // Email người mua
        
        // Tổng tiền
        public decimal SubTotal { get; set; } // Tổng tiền chưa thuế
        public decimal TaxAmount { get; set; } // Tiền thuế
        public decimal TotalAmount { get; set; } // Tổng tiền thanh toán
        public decimal DiscountAmount { get; set; } = 0; // Tiền chiết khấu
        
        // Trạng thái và xử lý
        [MaxLength(20)]
        public string Status { get; set; } = "draft"; // draft, issued, cancelled, replaced
        
        [MaxLength(20)]
        public string? TransactionUuid { get; set; } // UUID giao dịch với cơ quan thuế
        
        [MaxLength(50)]
        public string? InvoiceAuthCode { get; set; } // Mã xác thực của cơ quan thuế
        
        [MaxLength(2000)]
        public string? ErrorMessage { get; set; } // Thông báo lỗi (nếu có)
        
        public DateTime? IssuedAt { get; set; } // Thời gian phát hành thực tế
        
        public DateTime? CancelledAt { get; set; } // Thời gian hủy
        
        [MaxLength(500)]
        public string? CancelReason { get; set; } // Lý do hủy
        
        // Ghi chú và thông tin bổ sung
        [MaxLength(1000)]
        public string? Notes { get; set; } // Ghi chú
        
        [MaxLength(50)]
        public string? PaymentMethod { get; set; } // Phương thức thanh toán
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        // Liên kết với đơn hàng
        public int? OrderId { get; set; }
        public Order? Order { get; set; }
        
        // Danh sách chi tiết hóa đơn
        public List<EInvoiceItem> Items { get; set; } = new List<EInvoiceItem>();
        
        // Nhân viên tạo hóa đơn
        public int? StaffId { get; set; }
        public Staff? Staff { get; set; }
    }
}