using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetailPointBackend.Models
{
    using System.ComponentModel.DataAnnotations.Schema;
    // ...existing code...
    [Table("OrderItems")]
    public class OrderItem
    {
        [Key]
    public int OrderItemId { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal TotalPrice { get; set; }
    
    // Thông tin giảm giá cho từng mặt hàng
    public decimal DiscountAmount { get; set; } = 0;
    public decimal FinalPrice { get; set; } = 0; // Giá sau khi giảm

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [ForeignKey("OrderId")]
    public Order? Order { get; set; }
    
    [ForeignKey("ProductId")]
    public Product? Product { get; set; }
    }
}
