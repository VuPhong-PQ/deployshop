using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RetailPointBackend.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }
        public string? Name { get; set; }
        public string? Barcode { get; set; }
        public int? CategoryId { get; set; }
        [JsonPropertyName("productGroupId")]
        public int? ProductGroupId { get; set; } // Nhóm sản phẩm
        public decimal Price { get; set; }
        public decimal? CostPrice { get; set; }
        public int StockQuantity { get; set; }
        public int MinStockLevel { get; set; }
        public string? Unit { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public bool IsFeatured { get; set; } = false; // Sản phẩm hay bán
        
        // Multi-store support
        public int? StoreId { get; set; }
        [ForeignKey("StoreId")]
        public virtual Store? Store { get; set; }

        // Computed properties for stock status
        [NotMapped]
        public bool IsLowStock => StockQuantity <= MinStockLevel;

        [NotMapped]
        public bool IsOutOfStock => StockQuantity <= 0;

        [NotMapped]
        public string StockStatus => StockQuantity <= 0 ? "Hết hàng" : 
                                   StockQuantity <= MinStockLevel ? "Tồn kho thấp" : "Đủ hàng";

        [NotMapped]
        public int StockDeficit => Math.Max(0, MinStockLevel - StockQuantity);
    }
}