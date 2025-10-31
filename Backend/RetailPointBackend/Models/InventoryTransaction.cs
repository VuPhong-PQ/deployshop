using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Models
{
    public class InventoryTransaction
    {
        [Key]
        public int TransactionId { get; set; }
        
        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;
        
        public int StaffId { get; set; }
        public virtual Staff Staff { get; set; } = null!;
        
        [Required]
        public TransactionType Type { get; set; } // IN (nhập) hoặc OUT (xuất)
        
        [Required]
        public int Quantity { get; set; } // Số lượng (dương cho nhập, âm cho xuất)
        
        public decimal UnitPrice { get; set; } // Giá đơn vị tại thời điểm giao dịch
        
        public decimal TotalValue { get; set; } // Tổng giá trị = Quantity * UnitPrice
        
        [Required]
        [StringLength(100)]
        public string Reason { get; set; } = string.Empty; // Lý do: "Bán hàng", "Nhập hàng mới", "Trả hàng", "Kiểm kê", "Hỏng hóc"
        
        public string? Notes { get; set; } // Ghi chú chi tiết
        
        public int? OrderId { get; set; } // Liên kết với đơn hàng (nếu xuất do bán hàng)
        public virtual Order? Order { get; set; }
        
        public int? SupplierId { get; set; } // Nhà cung cấp (nếu nhập hàng)
        public string? SupplierName { get; set; } // Tên nhà cung cấp
        
        public string? ReferenceNumber { get; set; } // Số chứng từ, hóa đơn tham chiếu
        
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public int StockBefore { get; set; } // Tồn kho trước giao dịch
        public int StockAfter { get; set; } // Tồn kho sau giao dịch
    }
    
    public enum TransactionType
    {
        IN = 1,  // Nhập kho
        OUT = 2  // Xuất kho
    }
    
    // DTO cho tạo giao dịch nhập kho
    public class CreateInboundTransactionDto
    {
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }
        
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
        public decimal UnitPrice { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Reason { get; set; } = string.Empty;
        
        public string? Notes { get; set; }
        
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        
        public string? ReferenceNumber { get; set; }
        
        public DateTime? TransactionDate { get; set; }
    }
    
    // DTO cho tạo giao dịch xuất kho
    public class CreateOutboundTransactionDto
    {
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Reason { get; set; } = string.Empty;
        
        public string? Notes { get; set; }
        
        public int? OrderId { get; set; }
        
        public string? ReferenceNumber { get; set; }
        
        public DateTime? TransactionDate { get; set; }
    }
    
    // DTO cho response
    public class InventoryTransactionResponseDto
    {
        public int TransactionId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public int StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Changed from TransactionType to string
        public string TypeName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public int? OrderId { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? ReferenceNumber { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int StockBefore { get; set; }
        public int StockAfter { get; set; }
    }
    
    // DTO cho tổng hợp báo cáo
    public class InventorySummaryDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int TotalInbound { get; set; }
        public int TotalOutbound { get; set; }
        public decimal TotalInboundValue { get; set; }
        public decimal TotalOutboundValue { get; set; }
        public DateTime? LastTransaction { get; set; }
    }
}