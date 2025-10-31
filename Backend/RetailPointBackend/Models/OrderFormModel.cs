using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Models
{
    public class OrderFormModel
    {
    [Required]
    public string? CustomerName { get; set; }
    [Required]
    public string? ItemsJson { get; set; } // Chuỗi JSON chứa mảng sản phẩm
    }
}